using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace StagPoint.EDF.Net
{
	public class EdfFileHeader
	{
		#region Public properties

		/// <summary>
		/// The EDF version number. Should always be 0.
		/// </summary>
		public EdfAsciiInteger Version { get; } = new EdfAsciiInteger( 8 );

		/// <summary>
		/// The local Patient Identification code. For EDF+ files, this will contain the following subfields
		/// in order: Patient Code, Sex, Birthdate, Patient Name. 
		/// See <a href="https://www.edfplus.info/specs/edfplus.html#additionalspecs">Additional specifications in EDF+</a>
		/// </summary>
		public EdfAsciiString PatientInfo { get; } = new EdfAsciiString( 80 );

		/// <summary>
		/// The Local Recording Identification field. For EDF+ files, this will contain the following subfields
		/// in order: The text 'Startdate', the recording start date, the hospital administration code of the
		/// investigation (i.e. EEG number or PSG number), a code specifying the responsible investigator or technician,
		/// and a code specifying the used equipment.
		/// See <a href="https://www.edfplus.info/specs/edfplus.html#additionalspecs">Additional specifications in EDF+</a>
		/// </summary>
		public EdfAsciiString RecordingInfo { get; } = new EdfAsciiString( 80 );

		/// <summary>
		/// The Start Date and Start Time of the recording.
		/// </summary>
		public EdfAsciiDateTime StartTime { get; } = new EdfAsciiDateTime();

		/// <summary>
		/// The size of the header record, in bytes. 
		/// </summary>
		public EdfAsciiInteger HeaderRecordSize { get; } = new EdfAsciiInteger( 8 );

		/// <summary>
		/// Reserved for future use, with the following exception: For EDF+ files, this field will
		/// contain one of the following values: "EDF+C" meaning that the file conforms to the
		/// EDF+ specification and signal data is uninterrupted with all data stored contiguously,
		/// or "EDF+D" meaning that the file conforms to the EDF+ specification but signal data may be stored discontinuously. 
		/// See <a href="https://www.edfplus.info/specs/edfplus.html#header">The EDF+ header</a>
		/// </summary>
		public EdfAsciiString Reserved { get; } = new EdfAsciiString( 44 );
		
		/// <summary>
		/// Indicates the number of Data Records stored in the file
		/// </summary>
		public EdfAsciiInteger  NumberOfDataRecords  { get; } = new EdfAsciiInteger( 8 );

		/// <summary>
		/// The number of seconds represented by each Data Record. If the file contains only Annotations
		/// this value may be set to zero.
		/// See <a href="https://www.edfplus.info/specs/edfplus.html#datarecords">The EDF+ data records</a>
		/// </summary>
		public EdfAsciiFloat DurationOfDataRecord { get; } = new EdfAsciiFloat( 8 );

		/// <summary>
		/// The number of signals, both Standard signals and Annotations signals, stored in the file.
		/// </summary>
		public EdfAsciiInteger NumberOfSignals { get; } = new EdfAsciiInteger( 4 );

		/// <summary>
		/// Contains the Label field of each of the file's Signals, in order. 
		/// </summary>
		public List<EdfAsciiString> Labels { get; private set; } = new List<EdfAsciiString>();
		
		/// <summary>
		/// Contains the Transducer field of each of the file's Signals, in order.
		/// </summary>
		public List<EdfAsciiString> TransducerType { get; private set; } = new List<EdfAsciiString>();

		/// <summary>
		/// Contains the PhysicalDimension field of each of the file's Signals, in order.
		/// </summary>
		public List<EdfAsciiString> PhysicalDimension { get; private set; } = new List<EdfAsciiString>();

		/// <summary>
		/// Contains the PhysicalMinimum field of each of the file's Signals, in order.
		/// </summary>
		public List<EdfAsciiFloat> PhysicalMinimum { get; private set; } = new List<EdfAsciiFloat>();


		/// <summary>
		/// Contains the PhysicalMaximum field of each of the file's Signals, in order.
		/// </summary>
		public List<EdfAsciiFloat> PhysicalMaximum { get; private set; } = new List<EdfAsciiFloat>();

		/// <summary>
		/// Contains the DigitalMinimum field of each of the file's Signals, in order.
		/// </summary>
		public List<EdfAsciiInteger> DigitalMinimum { get; private set; } = new List<EdfAsciiInteger>();

		/// <summary>
		/// Contains the DigitalMaximum field of each of the file's Signals, in order.
		/// </summary>
		public List<EdfAsciiInteger> DigitalMaximum { get; private set; } = new List<EdfAsciiInteger>();

		/// <summary>
		/// Contains the Prefiltering field of each of the file's Signals, in order.
		/// </summary>
		public List<EdfAsciiString> Prefiltering { get; private set; } = new List<EdfAsciiString>();

		/// <summary>
		/// Contains the number of samples stored by each Signal per Data Record. 
		/// </summary>
		public List<EdfAsciiInteger> SamplesPerDataRecord { get; private set; } = new List<EdfAsciiInteger>();

		/// <summary>
		/// Contains the Reserved field of each of the file's Signals, in order.
		/// </summary>
		public List<EdfAsciiString> SignalReserved { get; private set; } = new List<EdfAsciiString>();

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
			readListFromBuffer( buffer, Labels,               NumberOfSignals, () => new EdfAsciiString( 16 ) );
			readListFromBuffer( buffer, TransducerType,       NumberOfSignals, () => new EdfAsciiString( 80 ) );
			readListFromBuffer( buffer, PhysicalDimension,    NumberOfSignals, () => new EdfAsciiString( 8 ) );
			readListFromBuffer( buffer, PhysicalMinimum,      NumberOfSignals, () => new EdfAsciiFloat( 8 ) );
			readListFromBuffer( buffer, PhysicalMaximum,      NumberOfSignals, () => new EdfAsciiFloat( 8 ) );
			readListFromBuffer( buffer, DigitalMinimum,       NumberOfSignals, () => new EdfAsciiInteger( 8 ) );
			readListFromBuffer( buffer, DigitalMaximum,       NumberOfSignals, () => new EdfAsciiInteger( 8 ) );
			readListFromBuffer( buffer, Prefiltering,         NumberOfSignals, () => new EdfAsciiString( 80 ) );
			readListFromBuffer( buffer, SamplesPerDataRecord, NumberOfSignals, () => new EdfAsciiInteger( 8 ) );
			readListFromBuffer( buffer, SignalReserved,       NumberOfSignals, () => new EdfAsciiString( 32 ) );
		}
		
		#endregion
		
		#region Internal functions used by the EdfFile class 
		
		internal void UpdateSignalFields( List<EdfSignalBase> signals )
		{
			NumberOfSignals.Value = signals.Count;

			Labels               = signals.Select( x => x.Label ).ToList();
			TransducerType       = signals.Select( x => x.TransducerType ).ToList();
			PhysicalDimension    = signals.Select( x => x.PhysicalDimension ).ToList();
			PhysicalMinimum      = signals.Select( x => x.PhysicalMinimum ).ToList();
			PhysicalMaximum      = signals.Select( x => x.PhysicalMaximum ).ToList();
			DigitalMinimum       = signals.Select( x => x.DigitalMinimum ).ToList();
			DigitalMaximum       = signals.Select( x => x.DigitalMaximum ).ToList();
			Prefiltering         = signals.Select( x => x.Prefiltering ).ToList();
			SamplesPerDataRecord = signals.Select( x => x.NumberOfSamplesPerRecord ).ToList();
			SignalReserved       = signals.Select( x => x.Reserved ).ToList();
		}

		internal void AllocateSignals( List<EdfSignalBase> signals )
		{
			signals.Clear();

			for( int i = 0; i < NumberOfSignals; i++ )
			{
				var header = new EdfSignalHeader
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

				if( header.Label.Value.Equals( StandardTexts.SignalType.EdfAnnotations ) )
				{
					signals.Add( new EdfAnnotationSignal( header ) );
				}
				else
				{
					signals.Add( new EdfStandardSignal( header ) );
				}
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
