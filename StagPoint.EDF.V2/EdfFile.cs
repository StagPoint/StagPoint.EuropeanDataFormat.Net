using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
				this.Header.ReadFrom( reader );
				allocateSignals();

				for( int i = 0; i < Header.NumberOfDataRecords; i++ )
				{
					readDataRecord( reader, i );
				}
			}
		}

		public void WriteTo( string filename )
		{
			using( var file = File.Create( filename ) )
			{
				WriteTo( file );
			}
		}

		public void WriteTo( Stream file )
		{
			using( var writer = new BinaryWriter( file, Encoding.ASCII ) )
			{
				// We don't know the number of DataRecords written yet. This value will be overwritten below.  
				Header.NumberOfDataRecords.Value = 0;
				
				// Write the header information 
				this.Header.WriteTo( writer );

				// Keep track of a counter per signal which counts how many samples from that signal have been written so far
				var counters = new int[ Signals.Count ];

				bool continueWriting = true;
				while( continueWriting )
				{
					Header.NumberOfDataRecords.Value += 1;

					continueWriting = false;
					
					for( int i = 0; i < Signals.Count; i++ )
					{
						int signalSamplesWritten = 0;

						if( Signals[ i ] is EdfStandardSignal standardSignal )
						{
							counters[ i ]   += writeSignal( writer, standardSignal, counters[ i ] );
							continueWriting |= counters[ i ] < standardSignal.Samples.Count;
						}
						else if( Signals[ i ] is EdfAnnotationSignal annotationSignal )
						{
							counters[ i ]   += writeSignal( writer, annotationSignal, counters[ i ] );
							continueWriting |= counters[ i ] < annotationSignal.Annotations.Count;
						}
					}
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

		private int writeSignal( BinaryWriter writer, EdfStandardSignal signal, int startIndex )
		{
			int samplesWritten = 0;
			int position       = startIndex;

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
			
			// Need to pad out the rest of any unused space 
			while( samplesWritten < signal.NumberOfSamplesPerRecord )
			{
				// Write a junk value that's easy to spot visually in a hex viewer, for debugging purposes 
				writer.Write( (short)0x0ABC );

				++samplesWritten;
			}

			return samplesWritten;
		}

		private int writeSignal( BinaryWriter writer, EdfAnnotationSignal signal, int startIndex )
		{
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

			foreach( var signal in Signals )
			{
				if( signal is EdfStandardSignal standardSignal )
				{
					readSignal( reader, standardSignal );
				}
				else if( signal is EdfAnnotationSignal annotationSignal )
				{
					readSignal( reader, annotationSignal );
				}
			}
		}

		private void readSignal( BinaryReader reader, EdfAnnotationSignal signal )
		{
			// TODO: This code relies too heavily on the file format being correct, and is potentially fragile as a result.  

			// Read NumberOfSamplesPerRecord 2-byte 'samples' into a local memory buffer, which we will parse
			var length   = signal.NumberOfSamplesPerRecord * 2;
			var buffer   = reader.ReadBytes( length );
			int position = 0;

			while( position < length )
			{
				double onset      = parseFloat( buffer, true, ref position );
				double duration   = 0;
				string descripton = string.Empty;

				// If an annotation contains a duration, it will be preceded by 0x15
				if( buffer[ position ] == '\x15' )
				{
					++position;
					duration = parseFloat( buffer, false, ref position );
				}
				
				// If an annotation contains a description, it will be preceded by 0x15
				// Note that there may be multiple consecutive descriptions for the same Onset and Duration, 
				// and each should be added as a separate Annotation.
				// See the "Time-stamped Annotations Lists (TALs) in an 'EDF Annotations' Signal" section of the EDF+ spec
				//		https://www.edfplus.info/specs/edfplus.html#tal
				while( buffer[ position ] == '\x14' )
				{
					++position;

					if( buffer[ position ] != 0 )
					{
						descripton = parseString( buffer, ref position );

						var annotation = new EdfAnnotation
						{
							Onset      = onset,
							Duration   = duration,
							Annotation = descripton
						};

						signal.Annotations.Add( annotation );
					}
				}
				
				// Consume the trailing 0x00 character. There may be multiple null characters present
				// if there are no more annotations in this Data Record, because the reserved space 
				// must be padded.
				while( position < length && buffer[ position ] == 0x00 )
				{
					++position;
				}
			}

			string parseString( byte[] bytes, ref int pos )
			{
				int startPos = pos;

				while( bytes[ pos ] >= 32 || (bytes[ pos ] >= 9 && bytes[ pos ] <= 13) )
				{
					pos++;
				}

				return Encoding.UTF8.GetString( bytes, startPos, pos - startPos );
			}
			
			double parseFloat( byte[] bytes, bool requireSign, ref int pos )
			{
				int startPos = pos;
				
				// The Onset field must start with a + or - character
				if( requireSign && bytes[ pos ] != '+' && bytes[ pos ] != '-' )
				{
					throw new FormatException( $"Expected a '+' or '-' character at pos {pos}" );
				}

				// The field may only contain the '+', '-', '.', and '0-9' characters.
				while( buffer[pos] >= 43 && buffer[pos] <= 57 )
				{
					pos++;
				}

				var stringRepresentation = Encoding.ASCII.GetString( bytes, startPos, pos - startPos );
				var result = double.Parse( stringRepresentation );

				return result;
			}
		}
		
		private void readSignal( BinaryReader reader, EdfStandardSignal signal )
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

		private void allocateSignals()
		{
			Signals.Clear();

			foreach( var header in Header.SignalHeaders )
			{
				if( header.Label.Value.Equals( StandardTexts.SignalType.EdfAnnotations ) )
				{
					Signals.Add( new EdfAnnotationSignal( header ) );
				}
				else
				{
					Signals.Add( new EdfStandardSignal( header ) );
				}
			}
		}
		
		#endregion 
	}
}
