using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace StagPoint.EDF.Net
{
	public class EdfFileHeader
	{
		#region Public properties
        
		public EdfAsciiInteger  Version              { get; } = new EdfAsciiInteger( 8 );
		public EdfAsciiString   PatientInfo          { get; } = new EdfAsciiString( 80 );
		public EdfAsciiString   RecordingInfo        { get; } = new EdfAsciiString( 80 );
		public EdfAsciiDateTime StartTime            { get; } = new EdfAsciiDateTime();
		public EdfAsciiInteger  HeaderRecordSize     { get; } = new EdfAsciiInteger( 8 );
		public EdfAsciiString   Reserved             { get; } = new EdfAsciiString( 44 );
		public EdfAsciiInteger  NumberOfDataRecords  { get; } = new EdfAsciiInteger( 8 );
		public EdfAsciiFloat    DurationOfDataRecord { get; } = new EdfAsciiFloat( 8 );
		public EdfAsciiInteger  NumberOfSignals      { get; } = new EdfAsciiInteger( 4 );

		public List<EdfAsciiString>  Labels               { get; } = new List<EdfAsciiString>();
		public List<EdfAsciiString>  TransducerType       { get; } = new List<EdfAsciiString>();
		public List<EdfAsciiString>  PhysicalDimension    { get; } = new List<EdfAsciiString>();
		public List<EdfAsciiFloat>   PhysicalMinimum      { get; } = new List<EdfAsciiFloat>();
		public List<EdfAsciiFloat>   PhysicalMaximum      { get; } = new List<EdfAsciiFloat>();
		public List<EdfAsciiInteger> DigitalMinimum       { get; } = new List<EdfAsciiInteger>();
		public List<EdfAsciiInteger> DigitalMaximum       { get; } = new List<EdfAsciiInteger>();
		public List<EdfAsciiString>  Prefiltering         { get; } = new List<EdfAsciiString>();
		public List<EdfAsciiInteger> SamplesPerDataRecord { get; } = new List<EdfAsciiInteger>();
		public List<EdfAsciiString>  SignalReserved       { get; } = new List<EdfAsciiString>();

		internal List<EdfSignalHeader> SignalHeaders { get; } = new List<EdfSignalHeader>();
		
		#endregion 
		
		#region Constructors

		public EdfFileHeader() { }

		public EdfFileHeader( Stream source )
		{
			using( var reader = new BinaryReader( source, Encoding.Default, true ) )
			{
				ReadFrom( reader );
			}
		}

		#endregion 
		
		#region Public Read/Write functions 

		public void WriteTo( BinaryWriter buffer )
		{
			HeaderRecordSize.Value = calculateHeaderRecordSize();

			// Write the fixed-size portion of the header
			Version.WriteTo( buffer );
			PatientInfo.WriteTo( buffer );
			RecordingInfo.WriteTo( buffer );
			StartTime.WriteTo( buffer );
			HeaderRecordSize.WriteTo( buffer );
			Reserved.WriteTo( buffer );
			NumberOfDataRecords.WriteTo( buffer );
			DurationOfDataRecord.WriteTo( buffer );
			NumberOfSignals.WriteTo( buffer );

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

		public void ReadFrom( BinaryReader buffer )
		{
			// Read the fixed-size portion of the header
			Version.ReadFrom( buffer );
			PatientInfo.ReadFrom( buffer );
			RecordingInfo.ReadFrom( buffer );
			StartTime.ReadFrom( buffer );
			HeaderRecordSize.ReadFrom( buffer );
			Reserved.ReadFrom( buffer );
			NumberOfDataRecords.ReadFrom( buffer );
			DurationOfDataRecord.ReadFrom( buffer );
			NumberOfSignals.ReadFrom( buffer );

			// Read the signal information
			readListFromBuffer( buffer, Labels,               NumberOfSignals, ()=> new EdfAsciiString( 16 ) );
			readListFromBuffer( buffer, TransducerType,       NumberOfSignals, ()=> new EdfAsciiString( 80 )  );
			readListFromBuffer( buffer, PhysicalDimension,    NumberOfSignals, ()=> new EdfAsciiString( 8 )  );
			readListFromBuffer( buffer, PhysicalMinimum,      NumberOfSignals, ()=> new EdfAsciiFloat( 8 )  );
			readListFromBuffer( buffer, PhysicalMaximum,      NumberOfSignals, ()=> new EdfAsciiFloat( 8 )  );
			readListFromBuffer( buffer, DigitalMinimum,       NumberOfSignals, ()=> new EdfAsciiInteger( 8 ) );
			readListFromBuffer( buffer, DigitalMaximum,       NumberOfSignals, ()=> new EdfAsciiInteger( 8 ) );
			readListFromBuffer( buffer, Prefiltering,         NumberOfSignals, ()=> new EdfAsciiString( 80 )  );
			readListFromBuffer( buffer, SamplesPerDataRecord, NumberOfSignals, ()=> new EdfAsciiInteger( 8 )  );
			readListFromBuffer( buffer, SignalReserved,       NumberOfSignals, () => new EdfAsciiString( 32 ) );

			SignalHeaders.Clear();

			for( int i = 0; i < NumberOfSignals; i++ )
			{
				var signal = new EdfSignalHeader
				{
					Label                    = Labels[ i ],
					TransducerType           = TransducerType[ i ],
					PhysicalDimension        = PhysicalDimension[ i ],
					PhysicalMinimum          = PhysicalMinimum[ i ],
					PhysicalMaximum          = PhysicalMaximum[ i ],
					DigitalMinimum           = DigitalMinimum[ i ],
					DigitalMaximum           = DigitalMaximum[ i ],
					Prefiltering             = Prefiltering[ i ],
					NumberOfSamplesPerRecord = SamplesPerDataRecord[ i ],
					Reserved                 = SignalReserved[ i ]
				};
				
				SignalHeaders.Add( signal );
			}
		}
		#endregion

		#region Private functions

		private void readListFromBuffer<T>( BinaryReader buffer, List<T> list, int count, Func<T> createItem ) where T : EdfAsciiField
		{
			list.Clear();
			list.Capacity = count;
			
			for( int i = 0; i < count; i++ )
			{
				var item = createItem();
				item.ReadFrom( buffer );
				
				list.Add( item );
			}
		}

		private static void writeListToBuffer( BinaryWriter buffer, IEnumerable<EdfAsciiField> values )
		{
			foreach( var value in values )
			{
				value.WriteTo( buffer );
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
