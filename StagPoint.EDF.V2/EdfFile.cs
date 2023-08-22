using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace StagPoint.EDF.Net
{
	public class EdfFile
	{
		public EdfFileHeader Header { get; set; } = new EdfFileHeader();

		public List<EdfSignalBase> Signals { get; set; } = new List<EdfSignalBase>();

		public void ReadFrom( string filename )
		{
			using( var file = File.OpenRead( filename ) )
			{
				ReadFrom( file );
			}
		}

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
				while( buffer[ position ] == '\x14' )
				{
					++position;

					if( buffer[ position ] != 0 )
					{
						descripton = parseString( buffer, ref position );

						Debug.WriteLine( $"Annotation: @{onset}, {duration}, {descripton}" );
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
	}
}
