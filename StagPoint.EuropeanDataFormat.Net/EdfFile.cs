// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

// ReSharper disable ConvertToUsingDeclaration
// ReSharper disable UsePatternMatching

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
		public EdfFileHeader Header { get; private set; } = new EdfFileHeader();

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

		/// <summary>
		/// Appends one compatible EDF file to the end of another. 
		/// </summary>
		/// <param name="fileToAppend">The file to append to the end of this file</param>
		public void Append( EdfFile fileToAppend )
		{
			if( !this.Header.IsCompatibleWith( fileToAppend.Header ) )
			{
				throw new Exception( "File headers are not compatible" );
			}

			var endTime = CalculateEndTime();
			if( fileToAppend.Header.StartTime < endTime )
			{
				throw new Exception( "Appended files must start at or after the end of the file being appended to" );
			}

			// Automatically set the EDF+D file type when necessary
			if( fileToAppend.Header.StartTime > endTime.AddMilliseconds( 1 ) )
			{
				Header.FileType = EdfFileType.EDF_Plus_Discontinuous;
			}

			var timeOffset      = fileToAppend.Header.StartTime.Value.Subtract( Header.StartTime ).TotalSeconds;
			var dataRecordIndex = Header.NumberOfDataRecords.Value;
			
			MarkFragment( dataRecordIndex, timeOffset );

			// Append annotations
			if( fileToAppend.AnnotationSignals.Count > 0 )
			{
				EdfAnnotationSignal thisAnnotations = null;
				
				// Always append annotations to the first AnnotationSignal (whether new or existing) on the target file
				if( this.AnnotationSignals.Count == 0 )
				{
					thisAnnotations = new EdfAnnotationSignal();
					AnnotationSignals.Add( thisAnnotations );
					
					thisAnnotations.NumberOfSamplesPerRecord.Value = fileToAppend.AnnotationSignals[ 0 ].NumberOfSamplesPerRecord;
				}
				else
				{
					thisAnnotations = AnnotationSignals[ 0 ];
				}

				thisAnnotations.Annotations.RemoveAll( x => x.IsTimeKeepingAnnotation );

				thisAnnotations.Annotations.Add( new EdfAnnotation()
				{
					Onset      = timeOffset,
					Annotation = $"APPEND: {timeOffset}",
					Duration   = fileToAppend.CalculateEndTime().Subtract( fileToAppend.Header.StartTime ).TotalSeconds
				} );
				
				foreach( var appendAnnotations in fileToAppend.AnnotationSignals )
				{
					appendAnnotations.AppendAnnotations( thisAnnotations.Annotations, false );
				}
			}

			// Append all signal samples
			for( int i = 0; i < Signals.Count; i++ )
			{
				Signals[ i ].Samples.AddRange( fileToAppend.Signals[ i ].Samples );
			}
			
			// TODO: Updating the number of data records in this way might be incorrect for "Annotations Only" files?
			// Update the number of Data Records by adding the number of Data Records the appended file
			// has, on the assumption that it will take that many additional Data Records to store the 
			// additional data. 
			Header.NumberOfDataRecords.Value += fileToAppend.Header.NumberOfDataRecords;
			updateFragmentEndIndices();
		}

		/// <summary>
		/// Returns a copy of this EdfFile instance
		/// </summary>
		public EdfFile Clone()
		{
			var newFile = new EdfFile();
			Header.CopyTo( newFile.Header );
			
			newFile.Header.AllocateSignals( newFile.Signals, newFile.AnnotationSignals );

			for( int i = 0; i < Signals.Count; i++ )
			{
				Signals[ i ].CopyTo( newFile.Signals[ i ] );
			}

			for( int i = 0; i < AnnotationSignals.Count; i++ )
			{
				AnnotationSignals[ i ].CopyTo( newFile.AnnotationSignals[ i ] );
			}

			newFile.Fragments.AddRange( this.Fragments );

			return newFile;
		}

		/// <summary>
		/// Returns the DateTime indicating when the recording ends 
		/// </summary>
		public DateTime CalculateEndTime()
		{
			// For a fragmented file, we need to add the last fragment's StartTime and Duration to the file's StartTime 
			if( Fragments.Count > 0 )
			{
				var lastFragment = Fragments.Last();
				return Header.StartTime.Value.AddSeconds( lastFragment.StartTime + lastFragment.Duration );
			}

			// For a contiguous file, the end time is a straightforward calculation based on the number and duration of Data Records.
			return Header.StartTime.Value.AddSeconds( Header.DurationOfDataRecord * Header.NumberOfDataRecords );
		}

		/// <summary>
		/// Sets the Start Time of a Data Record. 
		/// </summary>
		/// <param name="dataRecordIndex">The index of the Data Record to set the Start Time of</param>
		/// <param name="startTime">The time, in seconds, from the start of this file when the new fragment starts</param>
		public EdfDataFragment MarkFragment( int dataRecordIndex, double startTime )
		{
			// If there are any fragments, there must be a fragment matching the file's Start Time 
			if( Fragments.Count == 0 && startTime > 0 )
			{
				Fragments.Add( new EdfDataFragment( 0, 0 ) );				
			}

			// If an existing Fragment exists for the desired Data Record, update it. Otherwise, add a new Fragment.
			var newFragment = Fragments.FirstOrDefault( x => x.StartRecordIndex == dataRecordIndex );
			if( newFragment != null )
			{
				newFragment.StartTime = startTime;
			}
			else
			{
				newFragment = new EdfDataFragment( dataRecordIndex, Header.DurationOfDataRecord ) { StartTime = startTime };
				Fragments.Add( newFragment );
				
				// Sort the fragment into its proper place. Lazy coding, but not performance critical or anything. 
				Fragments.Sort();
			}

			// Recalculate the Duration and EndRecordIndex of each fragment
			updateFragmentEndIndices();
			
			return newFragment;
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
				
				// Keeping track of the "expected start time" allows detection of discontinuous files 
				double expectedRecordStartTime = 0;
			
				// Gather all of the signals (in declared order) into a single index-able array
				var allSignals = new EdfSignalBase[ Header.Labels.Count ];
				for( int i = 0; i < allSignals.Length; i++ )
				{
					allSignals[ i ] = GetSignalByName( Header.Labels[ i ].Value );
				}

				// Read in all Data Records stored. 
				for( int i = 0; i < Header.NumberOfDataRecords; i++ )
				{
					readDataRecord( reader, allSignals, i, fileType, ref expectedRecordStartTime );
				}
				
				// Make sure that all EdfFileFragments are updated 
				updateFragmentEndIndices();
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
			var fileType      = FileType;
			var isEdfPlusFile = fileType != EdfFileType.EDF;
			
			// From the specification: Even if no annotations are to be kept, an EDF+ file must contain at least
			// one 'EDF Annotations' signal in order to specify the starttime of each datarecord (see section 2.2.4).
			//    https://www.edfplus.info/specs/edfplus.html#annotationssignal
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

			assertFragmentsContiguousOrFileTypeIsCorrect();
			
			// Ensure that EdfStandardSignal.FrequencyInHz is updated.
			updateSignalFrequency();
			
			// Remove all Timekeeping Notifications from all Annotation Streams. Timekeeping notifications
			// get rewritten anew each time a file is saved, so don't retain stale ones.
			foreach( var signal in AnnotationSignals )
			{
				signal.Annotations.RemoveAll( x => x.IsTimeKeepingAnnotation );
			}
			
			using( var writer = new BinaryWriter( file, Encoding.ASCII, true ) )
			{
				// We don't know the number of DataRecords written yet. This value will be overwritten below. 
				// It's easy enough to calculate for files that contain only ordinary signals, but not for 
				// files which contain only annotations.
				Header.NumberOfDataRecords.Value = 0;
				
				// Update the header fields that store Signal information 
				Header.UpdateSignalFields( Signals, AnnotationSignals );
				
				// Write the header information 
				Header.WriteTo( writer );
                
				// Gather all of the signals (in declared order) into a single index-able array
				var allSignals = new EdfSignalBase[ Header.Labels.Count ];
				for( int i = 0; i < allSignals.Length; i++ )
				{
					allSignals[ i ] = GetSignalByName( Header.Labels[ i ].Value );
				}

				// Keep track of a counter per signal which counts how many samples from that signal have been written so far
				var counters = new int[ allSignals.Length ];

				var dataRecordStartTime = 0.0;

				bool continueWritingSignals     = true;
				bool continueWritingAnnotations = true;
				bool isFirstAnnotationSignal    = true;

				while( continueWritingSignals || continueWritingAnnotations )
				{
					// Assume that we are done unless proven otherwise (allows us to use boolean "OR" operation below)
					continueWritingSignals     = false;
					continueWritingAnnotations = false;

					// Use a flag to keep track of the first Annotation Signal written on each pass 
					// Only the first AnnotationSignal stores timekeeping annotations
					isFirstAnnotationSignal = true;

					for( int i = 0; i < allSignals.Length; i++ )
					{
						var baseSignal     = allSignals[ i ];
						var standardSignal = baseSignal as EdfStandardSignal;

						if( standardSignal != null )
						{
							counters[ i ]          += writeStandardSignal( writer, standardSignal, counters[ i ] );
							continueWritingSignals |= counters[ i ] < standardSignal.Samples.Count;
						}
						else
						{
							var annotationSignal = (EdfAnnotationSignal)baseSignal;

							if( !isEdfPlusFile )
							{
								throw new Exception( "The file must be marked as an EDF+ file to use annotations" );
							}

							var fragmentMarker = Fragments.FirstOrDefault( x => x.StartRecordIndex == Header.NumberOfDataRecords.Value );
							if( fragmentMarker != null )
							{
								dataRecordStartTime = fragmentMarker.StartTime;
							}

							counters[ i ] = writeAnnotationSignal(
								writer,
								annotationSignal,
								counters[ i ],
								isFirstAnnotationSignal,  
								dataRecordStartTime
							);

							continueWritingAnnotations |= counters[ i ] < annotationSignal.Annotations.Count;
							isFirstAnnotationSignal    =  false;
						}
					}
					
					// If there are ordinary signals present in the file (this is not an "Annotations Only" file), and
					// all signal data has been written to the file but there is still annotation data remaining to be
					// written, raise an exception. 
					// Otherwise, what would happen is that additional Data Records would need to be be written that
					// have no signal data to store, and that situation is not adequately covered by the Specification. 
					if( Signals.Count > 0 && !continueWritingSignals && continueWritingAnnotations )
					{
						throw new Exception( "Not enough space has been allocated for Annotations to be stored in the same number of Data Records as ordinary Signals" );
					}
					
					// Keep track of the expected start time of the next Data Record 
					// This allows us to know when the next data record is not contiguous.
					dataRecordStartTime              += Header.DurationOfDataRecord;
					Header.NumberOfDataRecords.Value += 1;
				}

				// Patch up the NumberOfDataRecords by seeking to the position of the field and overwriting the value. 
				// We could easily make this a constant, but it isn't performance-critical and this is very easy to 
				// read and understand.
				{
					var savedPosition = file.Position;

					file.Position = Header.Version.FieldLength +
					                Header.PatientIdentification.FieldLength +
					                Header.RecordingIdentification.FieldLength +
					                Header.StartTime.FieldLength +
					                Header.HeaderRecordSize.FieldLength +
					                Header.Reserved.FieldLength;

					Header.NumberOfDataRecords.WriteTo( writer );

					file.Position = savedPosition;
				}
			}
		}
		
		/// <summary>
		/// Finds and returns the named Signal. 
		/// </summary>
		public EdfSignalBase GetSignalByName( string name, bool ignoreCase = false )
		{
			StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

			// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
			foreach( var signal in Signals )
			{
				if( string.Compare( signal.Label.Value, name, comparison ) == 0 )
				{
					return signal;
				}
			}

			// NOTE: Since all Annotation Signals *must* have the same name, this function can only ever return
			// the first one. It is highly likely that there will only ever be a single Annotation Signal, but 
			// that is not a requirement.
			if( string.Compare( name, StandardTexts.SignalType.EdfAnnotations, comparison ) == 0 )
			{
				return AnnotationSignals.Count > 0 ? AnnotationSignals[ 0 ] : null;
			}
			
			return null;
		}
		
		#endregion

		#region Private functions

		private void assertFragmentsContiguousOrFileTypeIsCorrect()
		{
			if( FileType == EdfFileType.EDF_Plus_Discontinuous )
			{
				return;
			}
			
			for( int i = 1; i < Fragments.Count; i++ )
			{
				var recordedStartTime = Fragments[ i ].StartTime;
				var expectedStartTime = Fragments[ i - 1 ].StartTime + Fragments[ i - 1 ].Duration;
				
				if( Fragments[ i ].StartTime > Fragments[ i - 1 ].StartTime + Fragments[ i - 1 ].Duration + double.Epsilon )
				{
					throw new Exception( $"Data Records are not contiguous ({recordedStartTime - expectedStartTime}s gap encountered at record {i}), but File Type is not marked as EDF+D" );
				}
			}
		}
		
		private void updateFragmentEndIndices()
		{
			// Recalculate the EndRecordIndex and Duration of each fragment
			for( int i = 0; i < Fragments.Count; i++ )
			{
				if( i < Fragments.Count - 1 )
				{
					Fragments[ i ].EndRecordIndex = Fragments[ i + 1 ].StartRecordIndex - 1;
				}
				else
				{
					Fragments[ i ].EndRecordIndex = Header.NumberOfDataRecords - 1;
				}
			}
		}

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
				// TODO: Timekeeping annotations must specify the event that started a DataRecord in "Annotations Only" files
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
				
				// From the specification: Never use any digit grouping symbol in numbers.
				// Never use a comma "," for a for a decimal separator.
				// When a decimal separator is required, use a dot (".").
				//    https://www.edfplus.info/specs/edfplus.html#additionalspecs:~:text=Never%20use%20any%20digit%20grouping%20symbol
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
				var annotationSize = annotation.GetSize();

				// If there isn't enough space allocated for a large Annotation, then it cannot ever get written.
				// This is not something that can be worked around automatically, since Annotations may not be
				// split across Data Records.
				if( annotationSize > bytesAllocated )
				{
					throw new Exception( $"Annotation too large: The amount of storage allocated for {signal.Label} ({bytesAllocated} bytes) is not large enough to hold an annotation that is {annotationSize} bytes." );
				}

				// An Annotation must not be split across DataRecord boundaries. If there isn't enough space left, then
				// we are done writing Annotations for the current Data Record.
				if( bytesWritten + annotationSize > bytesAllocated )
				{
					break;
				}

				// Track all annotations written, so the calling code knows when to stop
				++position;
				
				// Write Onset
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

					// Write duration as an ASCII string. 
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

						// Linking signals to specific recording channels can be optionally specified in
						// EDF+ for which each corresponding annotation must be followed by the two-character
						// string '@@', followed by the corresponding standard EDF+ label.
						// Examples are: 'Limb movement@@EMG RAT' or 'EEG arousal@@Fpz-Cz' .
						//    https://www.edfplus.info/specs/edftexts.html#linkannotations
						if( !string.IsNullOrEmpty( annotation.LinkedChannel ) )
						{
							writer.Write( (byte)'@' );
							writer.Write( (byte)'@' );
							writer.Write(  Encoding.ASCII.GetBytes( annotation.LinkedChannel ) );
						}
						
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
			}

			// Pad out the rest of the allocated space with null characters 
			while( bytesWritten < bytesAllocated )
			{
				writer.Write( (byte)0x00 );
				bytesWritten += 1;
			}

			return position;
		}
		
		private void readDataRecord( BinaryReader reader, EdfSignalBase[] signals, int dataRecordIndex, EdfFileType fileType, ref double expectedStartTime )
		{
			// The first annotation of the first 'EDF Annotations' signal in each data record is empty, but its timestamp specifies
			// how many seconds after the file start time that data record starts.
			var    isFirstAnnotationSignal = true;
			double recordedStartTime       = expectedStartTime;

			for( int i = 0; i < signals.Length; i++ )
			{
				// Because the Standard Signals and Annotation Signals may be in any order, but I've chosen to store them
				// in separate Lists for user convenience, we need to iterate them in order of declaration and cast to the
				// appropriate type. 
				// One example of an Annotation Signal coming first is with ResMed AirSense 10 CPAP machine data files, 
				// where the two signals in one of the files are the Annotations and the CRC values, in that order. 
				var search = signals[ i ];
				
				// ReSharper disable once MergeCastWithTypeCheck
				if( search is EdfStandardSignal )
				{
					readStandardSignal( reader, (EdfStandardSignal)search );
					continue;
				}

				var signal = (EdfAnnotationSignal)search;
				
				//
				// If the file contains discontinuous data, we need to read the timekeeping annotation of each
				// data record and use that value instead.
				// See "Time Keeping of Data Records" in the EDF+ specification
				//     https://www.edfplus.info/specs/edfplus.html#timekeeping
				//
				
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
						Fragments.Add( new EdfDataFragment( dataRecordIndex, Header.DurationOfDataRecord )
						{
							StartTime = expectedStartTime,
						} );
					}
				}
				else
				{
					if( fileType == EdfFileType.EDF_Plus_Discontinuous )
					{
						// Start a new fragment 
						Fragments.Add( new EdfDataFragment( dataRecordIndex, Header.DurationOfDataRecord )
						{
							StartTime = recordedStartTime,
						} );
					}
					else
					{
						// Disregard if there are no Standard Signals or if DurationOfDataRecord == 0
						// This is because Annotation-only files will often have a Duration of zero, and 
						// since there are no signals, timing is irrelevant. 
						if( Header.DurationOfDataRecord > 0 && Signals.Count > 0 )
						{
							throw new Exception( $"Data Records are not contiguous ({recordedStartTime - expectedStartTime}s gap encountered at record {dataRecordIndex}), but File Type is not marked as EDF+D" );
						}
					}
				}
					
				isFirstAnnotationSignal = false;
			}

			// Keep track of the (potentially changed) expected start time. 
			expectedStartTime = recordedStartTime + Header.DurationOfDataRecord;
		}

		private void readAnnotationSignal( BinaryReader reader, EdfAnnotationSignal signal, bool expectTimekeepingAnnotation, ref double startTime )
		{
			// Read NumberOfSamplesPerRecord 2-byte 'samples' into a local memory buffer, which we will parse
			var length   = signal.NumberOfSamplesPerRecord * 2;
			var buffer   = reader.ReadBytes( length );
			int position = 0;

			if( expectTimekeepingAnnotation )
			{
				// If the file contains discontinuous data, we need to read the timekeeping annotation of each
				// data record and use that value instead.
				// See "Time Keeping of Data Records" in the EDF+ specification - https://www.edfplus.info/specs/edfplus.html#timekeeping
				
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

					string description = parseString( ref position );
					
					var channelIndex = description.IndexOf( "@@", StringComparison.Ordinal );
					if( channelIndex != -1 )
					{
						annotation.LinkedChannel = description.Substring( channelIndex + 2 );
						
						description = description.Substring( 0, channelIndex );
					}
					
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
	public class EdfDataFragment : IComparable<EdfDataFragment>
	{
		#region Public properties 
			
		/// <summary>
		/// The number of seconds after the file start time when the DataFragment begins 
		/// </summary>
		public double StartTime { get; set; } = 0;

		/// <summary>
		/// The number of seconds included in this DataFragment 
		/// </summary>
		public double Duration { get => (EndRecordIndex - StartRecordIndex + 1) * DataRecordLength; }

		/// <summary>
		/// The index of the first Data Record included in this EdfDataFragment
		/// </summary>
		internal int StartRecordIndex { get; private set; } = 0;

		/// <summary>
		/// The index of the last Data Record included in this EdfDataFragment
		/// </summary>
		internal int EndRecordIndex { get; set; } = 0;

		/// <summary>
		/// The duration, in seconds, of the source file's Data Record
		/// </summary>
		internal double DataRecordLength { get; private set; } = 0;
			
		#endregion
		
		#region Constructor

		internal EdfDataFragment( int startRecordIndex, double dataRecordLength )
		{
			StartRecordIndex = startRecordIndex;
			EndRecordIndex   = startRecordIndex;
			DataRecordLength = dataRecordLength;
		}
		
		#endregion 

		#region Base class overrides

		/// <inheritdoc />
		public int CompareTo( EdfDataFragment other )
		{
			return this.StartRecordIndex.CompareTo( other.StartRecordIndex );
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"Start: {StartTime:F2}, Records: {StartRecordIndex} - {EndRecordIndex}, Duration: {Duration:F2}";
		}
			
		#endregion 
	}
}
