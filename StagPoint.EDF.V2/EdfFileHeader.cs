using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace StagPoint.EDF.Net
{
	public class EdfFileHeader
	{
		#region Public properties 
		
		public EdfAsciiInteger  Version              { get; private set; } = new EdfAsciiInteger( 8 );
		public EdfAsciiString   PatientInfo          { get; set; }         = new EdfAsciiString( 80 );
		public EdfAsciiString   RecordingInfo        { get; set; }         = new EdfAsciiString( 80 );
		public EdfAsciiDateTime StartTime            { get; set; }         = new EdfAsciiDateTime();
		public EdfAsciiInteger  HeaderRecordSize     { get; private set; } = new EdfAsciiInteger( 8 );
		public EdfAsciiString   Reserved             { get; set; }         = new EdfAsciiString( 44 );
		public EdfAsciiInteger  NumberOfDataRecords  { get; set; }         = new EdfAsciiInteger( 8 );
		public EdfAsciiFloat    DurationOfDataRecord { get; set; }         = new EdfAsciiFloat( 8 );
		public EdfAsciiInteger  NumberOfSignals      { get; set; }         = new EdfAsciiInteger( 4 );

		public List<EdfAsciiString>  Labels               { get; set; } = new List<EdfAsciiString>();
		public List<EdfAsciiString>  TransducerType       { get; set; } = new List<EdfAsciiString>();
		public List<EdfAsciiString>  PhysicalDimension    { get; set; } = new List<EdfAsciiString>();
		public List<EdfAsciiInteger> PhysicalMinimum      { get; set; } = new List<EdfAsciiInteger>();
		public List<EdfAsciiInteger> PhysicalMaximum      { get; set; } = new List<EdfAsciiInteger>();
		public List<EdfAsciiInteger> DigitalMinimum       { get; set; } = new List<EdfAsciiInteger>();
		public List<EdfAsciiInteger> DigitalMaximum       { get; set; } = new List<EdfAsciiInteger>();
		public List<EdfAsciiString>  Prefiltering         { get; set; } = new List<EdfAsciiString>();
		public List<EdfAsciiInteger> SamplesPerDataRecord { get; set; } = new List<EdfAsciiInteger>();
		public List<EdfAsciiString>  SignalReserved       { get; set; } = new List<EdfAsciiString>();
		
		#endregion 
		
		#region Public Read/Write functions 

		public void WriteToBuffer( BinaryWriter buffer )
		{
			HeaderRecordSize.Value = calculateHeaderRecordSize();

			// Write the fixed-size portion of the header
			Version.WriteToBuffer( buffer );
			PatientInfo.WriteToBuffer( buffer );
			RecordingInfo.WriteToBuffer( buffer );
			StartTime.WriteToBuffer( buffer );
			HeaderRecordSize.WriteToBuffer( buffer );
			Reserved.WriteToBuffer( buffer );
			NumberOfDataRecords.WriteToBuffer( buffer );
			DurationOfDataRecord.WriteToBuffer( buffer );
			NumberOfSignals.WriteToBuffer( buffer );

            // Write the signal information 
            writeListToBuffer( buffer, Labels );
            writeListToBuffer( buffer, TransducerType );
            writeListToBuffer( buffer, PhysicalDimension );
            writeListToBuffer( buffer, PhysicalMinimum );
            writeListToBuffer( buffer, PhysicalMaximum );
            writeListToBuffer( buffer, DigitalMinimum );
            writeListToBuffer( buffer, DigitalMaximum );
            writeListToBuffer( buffer, Prefiltering );
            writeListToBuffer( buffer, SamplesPerDataRecord );
            writeListToBuffer( buffer, SignalReserved );
		}

		public void ReadFromBuffer( BinaryReader buffer )
		{
			// Read the fixed-size portion of the header
			Version.ReadFromBuffer( buffer );
			PatientInfo.ReadFromBuffer( buffer );
			RecordingInfo.ReadFromBuffer( buffer );
			StartTime.ReadFromBuffer( buffer );
			HeaderRecordSize.ReadFromBuffer( buffer );
			Reserved.ReadFromBuffer( buffer );
			NumberOfDataRecords.ReadFromBuffer( buffer );
			DurationOfDataRecord.ReadFromBuffer( buffer );
			NumberOfSignals.ReadFromBuffer( buffer );

			// Read the signal information
			readListFromBuffer( buffer, Labels,               NumberOfSignals, ()=> new EdfAsciiString( 16 ) );
			readListFromBuffer( buffer, TransducerType,       NumberOfSignals, ()=> new EdfAsciiString( 80 )  );
			readListFromBuffer( buffer, PhysicalDimension,    NumberOfSignals, ()=> new EdfAsciiString( 8 )  );
			readListFromBuffer( buffer, PhysicalMinimum,      NumberOfSignals, ()=> new EdfAsciiInteger( 8 )  );
			readListFromBuffer( buffer, PhysicalMaximum,      NumberOfSignals, ()=> new EdfAsciiInteger( 8 )  );
			readListFromBuffer( buffer, DigitalMinimum,       NumberOfSignals, ()=> new EdfAsciiInteger( 8 ) );
			readListFromBuffer( buffer, DigitalMaximum,       NumberOfSignals, ()=> new EdfAsciiInteger( 8 ) );
			readListFromBuffer( buffer, Prefiltering,         NumberOfSignals, ()=> new EdfAsciiString( 80 )  );
			readListFromBuffer( buffer, SamplesPerDataRecord, NumberOfSignals, ()=> new EdfAsciiInteger( 8 )  );
			readListFromBuffer( buffer, SignalReserved,       NumberOfSignals, () => new EdfAsciiString( 32 ) );
		}
		#endregion

		#region Private functions

		private void readListFromBuffer<T>( BinaryReader buffer, List<T> list, int count, Func<T> createItem ) where T : IEdfAsciiField
		{
			list.Clear();
			
			for( int i = 0; i < count; i++ )
			{
				var item = createItem();
				item.ReadFromBuffer( buffer );
				
				list.Add( item );
			}
		}

		private static void writeListToBuffer( BinaryWriter buffer, IEnumerable<IEdfAsciiField> values )
		{
			foreach( var value in values )
			{
				value.WriteToBuffer( buffer );
			}
		}
		
		private int calculateHeaderRecordSize()
		{
			return 256 +                  // The fixed-size part of a header is 256 bytes
			       NumberOfSignals * 16 + // Labels
			       NumberOfSignals * 80 + // Transducer type
			       NumberOfSignals * 8 +  // Physical Dimension
			       NumberOfSignals * 8 +  // Physical Minimum
			       NumberOfSignals * 8 +  // Physical Maximum
			       NumberOfSignals * 8 +  // Digital Minimum
			       NumberOfSignals * 8 +  // Digital Maximum
			       NumberOfSignals * 80 + // Prefiltering
			       NumberOfSignals * 8 +  // Number of samples per Data Record
			       NumberOfSignals * 32;  // Reserved
		}

		#endregion
	}
}
