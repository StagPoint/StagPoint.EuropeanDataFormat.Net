using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

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
		/// Returns the list of all Signals (both Standard signals and Annotation signals) stored in this file.
		/// </summary>
		public List<EdfSignalBase> Signals { get; } = new List<EdfSignalBase>();

		/// <summary>
		/// Returns TRUE if this file is an EDF+ file (makes use of EDF+ features)
		/// </summary>
		public bool IsEdfPlusFile
		{
			get
			{
				return Header.Reserved.Value.Equals( StandardTexts.FileType.EDF_Plus_Continuous ) ||
				       Header.Reserved.Value.Equals( StandardTexts.FileType.EDF_Plus_Discontinuous );
			}
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
				Header.ReadFrom( reader );
				Header.AllocateSignals( this.Signals );

				// Ensure that EdfStandardSignal.FrequencyInHz is updated.
				updateSignalFrequency();
			
				for( int i = 0; i < Header.NumberOfDataRecords; i++ )
				{
					readDataRecord( reader, i );
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
			var isEdfPlusFile = IsEdfPlusFile;
			if( isEdfPlusFile )
			{
				if( !Signals.Any( x => x is EdfAnnotationSignal ) )
				{
					var defaultAnnotationSignal = new EdfAnnotationSignal();
					defaultAnnotationSignal.NumberOfSamplesPerRecord.Value = 8;

					Signals.Add( defaultAnnotationSignal );
				}
			}
			
			// Ensure that EdfStandardSignal.FrequencyInHz is updated.
			updateSignalFrequency();
			
			using( var writer = new BinaryWriter( file, Encoding.ASCII ) )
			{
				// We don't know the number of DataRecords written yet. This value will be overwritten below.  
				Header.NumberOfDataRecords.Value = 0;
				
				// Update the header fields that store Signal information 
				Header.UpdateSignalFields( Signals );
				
				// Write the header information 
				Header.WriteTo( writer );
                
				// Keep track of a counter per signal which counts how many samples from that signal have been written so far
				var counters = new int[ Signals.Count ];

				var dataRecordStartTime = 0.0;

				bool continueWriting = true;
				while( continueWriting )
				{
					Header.NumberOfDataRecords.Value += 1;

					continueWriting = false;
					var writeTimekeepingAnnotation = isEdfPlusFile;
					
					for( int i = 0; i < Signals.Count; i++ )
					{
						if( Signals[ i ] is EdfStandardSignal standardSignal )
						{
							counters[ i ]   += writeStandardSignal( writer, standardSignal, counters[ i ] );
							continueWriting |= counters[ i ] < standardSignal.Samples.Count;
						}
						else if( Signals[ i ] is EdfAnnotationSignal annotationSignal )
						{
							if( !isEdfPlusFile )
							{
								throw new Exception( "The file must be marked as an EDF+ file to use annotations" );
							}
							
							counters[ i ] += writeAnnotationSignal( writer, annotationSignal, counters[ i ], writeTimekeepingAnnotation, dataRecordStartTime );
							
							continueWriting            |= counters[ i ] < annotationSignal.Annotations.Count;
							writeTimekeepingAnnotation =  false;
						}
					}

					dataRecordStartTime += Header.DurationOfDataRecord;
				}
				
				// Patch up the NumberOfDataRecords by seeking to the position of the field and overwriting the value. 
				// We could easily make this a constant, but it isn't performance-critical and this is very easy to 
				// read and understand.
				file.Position = Header.Version.FieldLength +
				                Header.PatientInfo.FieldLength +
				                Header.RecordingInfo.FieldLength +
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
			foreach( var loop in Signals )
			{
				if( loop is EdfStandardSignal signal )
				{
					signal.FrequencyInHz = signal.NumberOfSamplesPerRecord / Header.DurationOfDataRecord;
				}
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

				// An Annotation must not be split across DataRecord boundaries 
				if( bytesWritten + annotationSize > signal.NumberOfSamplesPerRecord * 2 )
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
			while( bytesWritten < signal.NumberOfSamplesPerRecord * 2 )
			{
				writer.Write( (byte)0x00 );
				bytesWritten += 1;
			}

			return signal.NumberOfSamplesPerRecord;
		}
		
		private void readDataRecord( BinaryReader reader, int index )
		{
			// By default a Data Record's start time is sequential, with each one starting immediately after
			// the previous one ended. The only exception is when reading EDF+D (EDF+ Discontinuous) files.
			var recordStartTime = Header.StartTime.Value.AddSeconds( Header.DurationOfDataRecord * index );
			
			// If the file contains discontinuous data, we need to read the timekeeping annotation of each
			// data record and use that value instead.
			// See "Time Keeping of Data Records" in the EDF+ specification - https://www.edfplus.info/specs/edfplus.html#timekeeping
			if( Header.Reserved.Value.Equals( StandardTexts.FileType.EDF_Plus_Discontinuous ) )
			{
				// TODO: Finish EDF+D implementation 
				throw new NotImplementedException();
			}
			else if( Header.Reserved.Value.Equals( StandardTexts.FileType.EDF_Plus_Continuous ) )
			{
				// NOTE: An EDF+C file may also contain timekeeping annotations in each data record, but 
				// those should always match the record start time calculated above.
				// TODO: Should we assert that EDF+C timekeeping annotations always match calculated Data Record start time?
			}

			var isFirstAnnotationSignal = true;

			foreach( var signal in Signals )
			{
				if( signal is EdfStandardSignal standardSignal )
				{
					readStandardSignal( reader, standardSignal );
				}
				else if( signal is EdfAnnotationSignal annotationSignal )
				{
					readAnnotationSignal( reader, annotationSignal, isFirstAnnotationSignal );
					isFirstAnnotationSignal = false;
				}
			}
		}
		
		private void readAnnotationSignal( BinaryReader reader, EdfAnnotationSignal signal, bool expectTimekeepingAnnotation )
		{
			// Read NumberOfSamplesPerRecord 2-byte 'samples' into a local memory buffer, which we will parse
			var length   = signal.NumberOfSamplesPerRecord * 2;
			var buffer   = reader.ReadBytes( length );
			int position = 0;

			if( expectTimekeepingAnnotation )
			{
				signal.Annotations.Add( readTimekeepingAnnotation( ref position ) );
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

				double onset = parseFloat( true, ref position );

				if( buffer[ position++ ] != 0x14 || buffer[ position++ ] != 0x14 || buffer[ position++ ] != 0x00 )
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
}
