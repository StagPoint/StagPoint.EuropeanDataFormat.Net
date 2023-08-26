// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
// ReSharper disable ConvertToUsingDeclaration

namespace StagPoint.EDF.Net
{
	/// <summary>
	/// Represents a European Data Format file, and is used to read and write EDF files.
	/// </summary>
	public class EdfFile
	{
		#region Public properties 
		
		/// <summary>
		/// Returns the EdfFileHeader instance containing all of the information stored in the EDF Header of this file.
		/// </summary>
		public EdfFileHeader Header { get; } = new EdfFileHeader();

		/// <summary>
		/// The list of all Standard Signals (containing numerical signal data) stored in this file.
		/// </summary>
		public List<EdfStandardSignal> Signals { get; } = new List<EdfStandardSignal>();

		/// <summary>
		/// The list of all Annotation Signals stored in this file.
		/// </summary>
		public List<EdfAnnotationSignal> AnnotationSignals { get; } = new List<EdfAnnotationSignal>();
		
		/// <summary>
		/// For EDF+D files (EDF+ Discontinuous), this list will contain the start time and size in seconds of
		/// each discontinuous section of signal samples.
		/// <see cref="EdfDataFragment"/>
		/// </summary>
		public List<EdfDataFragment> Fragments { get; } = new List<EdfDataFragment>();

		/// <summary>
		/// Returns the type of file format used (EDF, EDF+C, or EDF+C)
		/// </summary>
		public EdfFileType FileType
		{
			get => Header.FileType;
			set { Header.FileType = value; }
		}
		
		#endregion
		
		#region Static functions

		/// <summary>
		/// Returns a new EdfFile instance loaded from the given filename
		/// </summary>
		/// <param name="filename">The fully-qualified path to the EDF file to open</param>
		public static EdfFile Open( string filename )
		{
			var file = new EdfFile();
			file.ReadFrom( filename );

			return file;
		}
		
		#endregion 

		#region Public functions

		public void MarkFragment( double onset, double duration )
		{
			if( duration % Header.DurationOfDataRecord > double.Epsilon )
			{
				throw new ArgumentException( $"Duration must be a multiple of the Data Record size. {duration} is not a multiple of {Header.DurationOfDataRecord}", nameof( duration ) );
			}

			int dataRecordIndex = 0;

			if( Fragments.Count > 0 )
			{
				var last = Fragments.Last();
				
				if( onset <= last.Onset + last.Duration )
				{
					throw new ArgumentException( $"Onset time {onset} overlaps an existing fragment", nameof( onset ) );
				}

				dataRecordIndex = last.DataRecordIndex + (int)Math.Ceiling( last.Duration / Header.DurationOfDataRecord );
			}

			var newFragment = new EdfDataFragment( dataRecordIndex, Header.DurationOfDataRecord )
			{
				Onset = onset,
				Duration = duration
			};
			
			Fragments.Add( newFragment );
		}

		/// <summary>
		/// Reads from the file indicated
		/// </summary>
		/// <param name="filename">The full path to the file to be read</param>
		public void ReadFrom( string filename )
		{
			using( var file = File.OpenRead( filename ) )
			{
				ReadFrom( file );
			}
		}

		/// <summary>
		/// Reads the EDF File information from the provided stream (most often a File Stream)
		/// </summary>
		/// <param name="file">The stream which contains the EDF file information to be read</param>
		public void ReadFrom( Stream file )
		{
			using( var reader = new BinaryReader( file, Encoding.ASCII ) )
			{
				Fragments.Clear();
				
				Header.ReadFrom( reader );
				Header.AllocateSignals( this.Signals, this.AnnotationSignals );

				// Ensure that EdfStandardSignal.FrequencyInHz is updated.
				updateSignalFrequency();

				// Only want to perform this work once
				var fileType = FileType;
			
				// Read in all Data Records stored. 
				for( int i = 0; i < Header.NumberOfDataRecords; i++ )
				{
					readDataRecord( reader, i, fileType );
				}
			}
		}

		/// <summary>
		/// Saves the EDF file to the given filename
		/// </summary>
		/// <param name="filename">The fully-qualified path to the file you wish to save</param>
		public void WriteTo( string filename )
		{
			using( var file = File.Create( filename ) )
			{
				WriteTo( file );
			}
		}

		/// <summary>
		/// Saves the EDF file to the given Stream
		/// </summary>
		/// <param name="file">The stream (typically a FileStream, but may be any object derived from Stream)
		/// to which you want to save the file information.</param>
		public void WriteTo( Stream file )
		{
			// EDF+ files must have an Annotations Signal, for timekeeping if nothing else 
			var fileType      = FileType;
			var isEdfPlusFile = fileType != EdfFileType.EDF;
			if( isEdfPlusFile )
			{
				if( AnnotationSignals.Count == 0 )
				{
					var defaultAnnotationSignal = new EdfAnnotationSignal();
					
					// Ensure that enough space is allocated for storing a Timekeeping Annotation
					defaultAnnotationSignal.NumberOfSamplesPerRecord.Value = 8;

					AnnotationSignals.Add( defaultAnnotationSignal );
				}
			}
			
			// Ensure that EdfStandardSignal.FrequencyInHz is updated.
			updateSignalFrequency();
			
			using( var writer = new BinaryWriter( file, Encoding.ASCII ) )
			{
				// We don't know the number of DataRecords written yet. This value will be overwritten below.  
				Header.NumberOfDataRecords.Value = 0;
				
				// Update the header fields that store Signal information 
				Header.UpdateSignalFields( Signals, AnnotationSignals );
				
				// Write the header information 
				Header.WriteTo( writer );
                
				// Keep track of a counter per signal which counts how many samples from that signal have been written so far
				var counters           = new int[ Signals.Count ];
				var annotationCounters = new int[ AnnotationSignals.Count ];

				var dataRecordStartTime = 0.0;

				bool continueWriting = true;
				while( continueWriting )
				{
					continueWriting = false;
					
					for( int i = 0; i < Signals.Count; i++ )
					{
						var standardSignal = Signals[ i ];
						
						counters[ i ]   += writeStandardSignal( writer, standardSignal, counters[ i ] );
						continueWriting |= counters[ i ] < standardSignal.Samples.Count;
					}

					for( int i = 0; i < AnnotationSignals.Count; i++ )
					{
						if( !isEdfPlusFile )
						{
							throw new Exception( "The file must be marked as an EDF+ file to use annotations" );
						}

						var fragmentMarker = Fragments.FirstOrDefault( x => x.DataRecordIndex == Header.NumberOfDataRecords.Value );
						if( fragmentMarker != null )
						{
							dataRecordStartTime = fragmentMarker.Onset;
						}

						var annotationSignal = AnnotationSignals[ i ];
						
						annotationCounters[ i ] += writeAnnotationSignal(
							writer,
							annotationSignal,
							annotationCounters[ i ],
							i == 0, // Only the first AnnotationSignal stores timekeeping annotations 
							dataRecordStartTime
						);

						continueWriting |= annotationCounters[ i ] < annotationSignal.NumberOfSamplesPerRecord;
					}

					// Keep track of the expected start time of the next Data Record 
					// This allows us to know when the next data record is not contiguous.
					dataRecordStartTime              += Header.DurationOfDataRecord;
					Header.NumberOfDataRecords.Value += 1;
				}
				
				// Patch up the NumberOfDataRecords by seeking to the position of the field and overwriting the value. 
				// We could easily make this a constant, but it isn't performance-critical and this is very easy to 
				// read and understand.
				file.Position = Header.Version.FieldLength +
				                Header.PatientIdentification.FieldLength +
				                Header.RecordingIdentification.FieldLength +
				                Header.StartTime.FieldLength +
				                Header.HeaderRecordSize.FieldLength +
				                Header.Reserved.FieldLength;

				Header.NumberOfDataRecords.WriteTo( writer );
			}
		}
		
		#endregion

		#region Private functions

		private void updateSignalFrequency()
		{
			foreach( var signal in Signals )
			{
				signal.FrequencyInHz = signal.NumberOfSamplesPerRecord / Header.DurationOfDataRecord;
			}
		}

		private int writeStandardSignal( BinaryWriter writer, EdfStandardSignal signal, int position )
		{
			int samplesWritten = 0;

			// Write as many samples as possible, up to NumberOfSamplesPerRecord 
			while( position < signal.Samples.Count && samplesWritten < signal.NumberOfSamplesPerRecord )
			{
				var    sample      = signal.Samples[ position ];
				double inverseT    = MathUtil.InverseLerp( signal.PhysicalMinimum, signal.PhysicalMaximum, sample );
				short  outputValue = (short)MathUtil.Lerp( signal.DigitalMinimum, signal.DigitalMaximum, inverseT );
				
				writer.Write( outputValue );

				++position;
				++samplesWritten;
			}
			
			// TODO: What is the correct thing to to when there are fewer samples available than allocated space? The spec seems strangely silent on this.
			// When there are fewer samples than the space allocated, fill out the rest of the allocated space to keep things aligned
			while( samplesWritten < signal.NumberOfSamplesPerRecord )
			{
				// Writing the DigitalMinimum value is probably the safest thing to do
				writer.Write( (short)signal.DigitalMinimum.Value );

				++samplesWritten;
			}

			return samplesWritten;
		}

		private int writeAnnotationSignal( BinaryWriter writer, EdfAnnotationSignal signal, int position, bool storeTimekeeping, double dataRecordStartTime )
		{
			var bufferStartPosition = writer.BaseStream.Position;
			int bytesWritten        = 0;
			int bytesAllocated      = signal.NumberOfSamplesPerRecord * 2;

			if( storeTimekeeping )
			{
				// TODO: Timekeeping annotations must specify the event that started a DataRecord in files with no signals
				//
				//	  https://www.edfplus.info/specs/edfplus.html#timekeeping
				//
				//	  "If the data records contain 'ordinary signals', then the starttime of each data record must be the starttime
				//	  of its signals. If there are no 'ordinary signals', then a non-empty annotation immediately following the
				//	  time-keeping annotation (in the same TAL) must specify what event defines the starttime of this data record."
				//
				//	  Worth noting that none of the EDF-compatible software I have seems to enforce/expect/support this?
				
				long startPos = writer.BaseStream.Position;
				
				if( dataRecordStartTime >= 0 )
				{
					writer.Write( (byte)'+' );
				}
				
				var startTimeAsString = dataRecordStartTime.ToString( CultureInfo.InvariantCulture );
				writer.Write( Encoding.ASCII.GetBytes( startTimeAsString ) );
				
				// Note that the specification calls the Timekeeping Annotation a TAL, implying that one or more text
				// annotations can be included, but at least two of the pieces of software that I have which read EDF
				// files refuse to read such files and consider them invalid. 
					
				writer.Write( (byte)0x14 );
				writer.Write( (byte)0x14 );
				writer.Write( (byte)0x00 );
				
				bytesWritten = (int)(writer.BaseStream.Position - startPos);
			}
				
			while( position < signal.Annotations.Count )
			{
				var annotation = signal.Annotations[ position ];
				
				// Ignore any Timekeeping Annotations in the Signal, as these were read from file previously and 
				// may no longer match the file. 
				if( annotation.IsTimeKeepingAnnotation )
				{
					++position;
					continue;
				}

				var annotationSize = annotation.GetSize();

				// If there isn't enough space allocated for a large Annotation, then it cannot ever get written
				if( annotationSize > bytesAllocated )
				{
					throw new Exception( $"Annotation too large: The amount of storage allocated for {signal.Label} ({bytesAllocated} bytes) is not large enough to hold an annotation that is {annotationSize} bytes." );
				}

				// An Annotation must not be split across DataRecord boundaries 
				if( bytesWritten + annotationSize > bytesAllocated )
				{
					break;
				}
				
				// Write Onset. 
				{
					// Onset must be preceded by a '-' or '+' character
					if( annotation.Onset >= 0 )
					{
						writer.Write( (byte)'+' );
					}

					var onsetAsString = annotation.Onset.ToString( CultureInfo.InvariantCulture );
					writer.Write( Encoding.ASCII.GetBytes( onsetAsString ) );
				}
				
				// Write Duration, if present
				if( annotation.Duration.HasValue )
				{
					// Write delimiter
					writer.Write( (byte)0x15 );

					var durationAsString = annotation.Duration.Value.ToString( CultureInfo.InvariantCulture );
					writer.Write( Encoding.ASCII.GetBytes( durationAsString ) );
				}
				
				// Write delimiter
				writer.Write( (byte)0x14 );

				// Write all annotations
				if( annotation.AnnotationList.Count > 0 )
				{
					foreach( var description in annotation.AnnotationList )
					{
						writer.Write( Encoding.UTF8.GetBytes( description ) );
						writer.Write( (byte)0x14 );
					}
				}
				else
				{
					// Write "Empty Annotation" delimiter
					writer.Write( (byte)0x14 );
				}

				// Write terminator 
				writer.Write( (byte)0x00 );
				
				// Keep track of the number of bytes written
				bytesWritten = (int)(writer.BaseStream.Position - bufferStartPosition);

				++position;
			}

			// Pad out the rest of the allocated space with null characters 
			while( bytesWritten < bytesAllocated )
			{
				writer.Write( (byte)0x00 );
				bytesWritten += 1;
			}

			return signal.NumberOfSamplesPerRecord;
		}
		
		private void readDataRecord( BinaryReader reader, int index, EdfFileType fileType )
		{
			// By default a Data Record's start time is sequential, with each one starting immediately after
			// the previous one ended. The only exception is when reading EDF+D (EDF+ Discontinuous) files.
			var expectedStartTime = Header.DurationOfDataRecord * index;
			
			// The first annotation of the first 'EDF Annotations' signal in each data record is empty, but its timestamp specifies
			// how many seconds after the file start time that data record starts.
			var    isFirstAnnotationSignal = true;
			double recordedStartTime       = 0;

			// // If the file contains discontinuous data, we need to read the timekeeping annotation of each
			// // data record and use that value instead.
			// // See "Time Keeping of Data Records" in the EDF+ specification - https://www.edfplus.info/specs/edfplus.html#timekeeping
			// if( fileType == EdfFileType.EDF_Plus_Discontinuous )
			// {
			// 	// TODO: Finish EDF+D implementation 
			// 	throw new NotImplementedException();
			// }
			// else if( fileType == EdfFileType.EDF_Plus )
			// {
			// 	// NOTE: An EDF+C file may also contain timekeeping annotations in each data record, but 
			// 	// those should always match the record start time calculated above.
			// 	// TODO: Should we assert that EDF+C timekeeping annotations always match calculated Data Record start time?
			// }

			foreach( var signal in Signals )
			{
				readStandardSignal( reader, signal );
			}
			
			foreach( var signal in AnnotationSignals )
			{
				readAnnotationSignal( reader, signal, isFirstAnnotationSignal, ref recordedStartTime );

				// Check timekeeping annotation?
				if( !isFirstAnnotationSignal || Signals.Count == 0 )
				{
					continue;
				}
				
				if( recordedStartTime < expectedStartTime )
				{
					throw new Exception( $"Data Records are out of order: {recordedStartTime} was encountered before {expectedStartTime}" );
				}
					
				// If the timekeeping matches what is expected then extend the time of the last timekeeping annotation 
				if( recordedStartTime - expectedStartTime < 0.001 )
				{
					if( Fragments.Count == 0 )
					{
						// Add a new fragment when necessary
						Fragments.Add( new EdfDataFragment( index, Header.DurationOfDataRecord )
						{
							Onset    = expectedStartTime,
							Duration = Header.DurationOfDataRecord
						} );
					}
					else
					{
						// Otherwise just extend the last fragment's duration 
						Fragments.Last().Duration += Header.DurationOfDataRecord;
					}
				}
				else
				{
					if( fileType == EdfFileType.EDF_Plus_Discontinuous )
					{
						// Start a new fragment 
						Fragments.Add( new EdfDataFragment( index, Header.DurationOfDataRecord )
						{
							Onset    = recordedStartTime,
							Duration = Header.DurationOfDataRecord
						} );
					}
					else
					{
						// Disregard if there are no Standard Signals or if DurationOfDataRecord == 0
						// This is because Annotation-only files will often have a Duration of zero, and 
						// since there are no signals, timing is irrelevant. 
						if( Header.DurationOfDataRecord > 0 && Signals.Count > 0 )
						{
							throw new Exception( $"Data Records are not contiguous ({recordedStartTime - expectedStartTime}s gap encountered at record {index}), but File Type is not EDF+D" );
						}
					}
				}
					
				isFirstAnnotationSignal = false;
			}
		}
		
		private void readAnnotationSignal( BinaryReader reader, EdfAnnotationSignal signal, bool expectTimekeepingAnnotation, ref double startTime )
		{
			// Read NumberOfSamplesPerRecord 2-byte 'samples' into a local memory buffer, which we will parse
			var length   = signal.NumberOfSamplesPerRecord * 2;
			var buffer   = reader.ReadBytes( length );
			int position = 0;

			if( expectTimekeepingAnnotation )
			{
				var timeKeeping = readTimekeepingAnnotation( ref position );
				startTime = timeKeeping.Onset;
				
				signal.Annotations.Add( timeKeeping );
			}

			while( position < length && buffer[ position ] > 0x00 )
			{
				double onset      = parseFloat( true, ref position );
				double duration   = 0;

				// If an annotation contains a duration, it will be preceded by 0x15
				if( buffer[ position ] == 0x15 )
				{
					++position;
					duration = parseFloat( false, ref position );
				}

				var annotation = new EdfAnnotation
				{
					Onset      = onset,
					Duration   = duration,
				};

				signal.Annotations.Add( annotation );

				// If an annotation contains a description, it will be preceded by 0x14
				// Note that there may be multiple consecutive descriptions for the same Onset and Duration, 
				// and each should be treated as a separate Annotation.
				//
				// See the "Time-stamped Annotations Lists (TALs) in an 'EDF Annotations' Signal" section of the EDF+ spec
				//		https://www.edfplus.info/specs/edfplus.html#tal
				//
				// This library will read in a TAL as multiple discrete EdfAnnotation instances, and does not currently
				// provide a way for the user to create a TAL.
				while( buffer[ position ] == 0x14 )
				{
					if( buffer[ ++position ] == 0x00 )
					{
						break;
					}

					var description = parseString( ref position );
					annotation.AnnotationList.Add( description );
				}

				// Consume the trailing 0x00 character. There may be multiple null characters present
				// if there are no more annotations in this Data Record, because the reserved space 
				// must be padded.
				while( position < length && buffer[ position ] == 0x00 )
				{
					++position;
				}
			}

			EdfAnnotation readTimekeepingAnnotation( ref int pos )
			{
				var startPosition = pos;

				double onset = parseFloat( true, ref pos );

				if( buffer[ pos++ ] != 0x14 || buffer[ pos++ ] != 0x14 || buffer[ pos++ ] != 0x00 )
				{
					throw new FormatException( $"Missing or invalid timekeeping annotation at position {startPosition}" );
				}

				return new EdfAnnotation
				{
					Onset                   = onset,
					IsTimeKeepingAnnotation = true
				};
			}

			string parseString( ref int pos )
			{
				int startPos = pos;

				while( buffer[ pos ] != 0x14 )
				{
					pos++;
				}

				return Encoding.UTF8.GetString( buffer, startPos, pos - startPos );
			}
			
			double parseFloat( bool requireSign, ref int pos )
			{
				int startPos = pos;
				
				// The Onset field must start with a + or - character
				if( requireSign && buffer[ pos ] != '+' && buffer[ pos ] != '-' )
				{
					throw new FormatException( $"Expected a '+' or '-' character at pos {pos}" );
				}

				// The field may only contain the '+', '-', '.', and '0-9' characters.
				while( buffer[ pos ] >= 43 && buffer[ pos ] <= 57 )
				{
					pos++;
				}

				var stringRepresentation = Encoding.ASCII.GetString( buffer, startPos, pos - startPos );
				var result = double.Parse( stringRepresentation, CultureInfo.InvariantCulture );

				return result;
			}
		}
		
		private void readStandardSignal( BinaryReader reader, EdfStandardSignal signal )
		{
			for( int i = 0; i < signal.NumberOfSamplesPerRecord; i++ )
			{
				var sample = reader.ReadInt16();
				
				// TODO: Implement support for logarithmic transformation (https://www.edfplus.info/specs/edffloat.html)
				
				var t     = MathUtil.InverseLerp( signal.DigitalMinimum, signal.DigitalMaximum, sample );
				var value = MathUtil.Lerp( signal.PhysicalMinimum, signal.PhysicalMaximum, t );
				
				signal.Samples.Add( value );
			}
		}
		
		#endregion 
	}
	
	/// <summary>
	/// Represents the start time (in seconds from the file start time) and Duration (in seconds)
	/// of a section of Signal samples stored in the file. 
	/// </summary>
	public class EdfDataFragment
	{
		#region Public properties 
			
		/// <summary>
		/// The number of seconds after the file start time when the DataFragment begins 
		/// </summary>
		public double Onset { get; set; } = 0;
			
		/// <summary>
		/// The number of seconds included in this DataFragment 
		/// </summary>
		public double Duration { get; set; } = 0;

		/// <summary>
		/// The index of the Data Record that this EdfDataFragment starts on
		/// </summary>
		internal int DataRecordIndex { get; private set; } = 0;

		/// <summary>
		/// The duration, in seconds, of the source file's Data Record
		/// </summary>
		internal double DataRecordDuration { get; private set; } = 0;
			
		#endregion
		
		#region Constructor

		internal EdfDataFragment( int dataRecordIndex, double dataRecordDuration )
		{
			DataRecordIndex  = dataRecordIndex;
			DataRecordDuration = dataRecordDuration;
		}
		
		#endregion 

		#region Base class overrides

		/// <summary>
		/// Returns a string representation of this object
		/// </summary>
		public override string ToString()
		{
			return $"Start: {Onset:F2}, Duration: {Duration:F2}";
		}
			
		#endregion 
	}
}
