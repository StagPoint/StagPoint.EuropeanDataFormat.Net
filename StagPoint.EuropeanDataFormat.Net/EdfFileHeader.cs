// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StagPoint.EDF.Net
{
	/// <summary>
	/// The header record identifies the patient and recording and specifies the technical characteristics of the recorded signals.
	/// </summary>
	public class EdfFileHeader
	{
		#region Stored Header fields 

		/// <summary>
		/// The EDF version number. Should always be 0.
		/// </summary>
		public EdfAsciiInteger Version { get; } = new EdfAsciiInteger( 8 );

		/// <summary>
		/// The local Patient Identification code. For EDF+ files, this will contain the following subfields
		/// in order: Patient Code, Sex, Birthdate, Patient Name. 
		/// See <a href="https://www.edfplus.info/specs/edfplus.html#additionalspecs">Additional specifications in EDF+</a>
		/// </summary>
		public EdfAsciiString PatientIdentification { get; private set; } = new EdfAsciiString( 80 );

		/// <summary>
		/// The Local Recording Identification field. For EDF+ files, this will contain the following subfields
		/// in order: The text 'Startdate', the recording start date, the hospital administration code of the
		/// investigation (i.e. EEG number or PSG number), a code specifying the responsible investigator or technician,
		/// and a code specifying the used equipment.
		/// See <a href="https://www.edfplus.info/specs/edfplus.html#additionalspecs">Additional specifications in EDF+</a>
		/// </summary>
		public EdfAsciiString RecordingIdentification { get; private set; } = new EdfAsciiString( 80 );

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
		
		#region Public properties not stored in the file

		/// <summary>
		/// Gets/Sets which EDF file format is used to store the file 
		/// </summary>
		public EdfFileType FileType
		{
			get
			{
				// ReSharper disable once ConvertIfStatementToSwitchStatement
				if( string.IsNullOrEmpty( Reserved.Value ) )
					return EdfFileType.EDF;
				else if( Reserved.Value == StandardTexts.FileType.EDF_Plus_Continuous )
					return EdfFileType.EDF_Plus;
				else if( Reserved.Value == StandardTexts.FileType.EDF_Plus_Discontinuous )
					return EdfFileType.EDF_Plus_Discontinuous;
				else
					throw new Exception( $"Unrecognized file type '{Reserved.Value}'" );
			}
			set
			{
				// ReSharper disable once ConvertSwitchStatementToSwitchExpression
				switch( value )
				{
					case EdfFileType.EDF:
						Reserved.Value = StandardTexts.FileType.EDF;
						break;
					case EdfFileType.EDF_Plus:
						Reserved.Value = StandardTexts.FileType.EDF_Plus_Continuous;
						break;
					case EdfFileType.EDF_Plus_Discontinuous:
						Reserved.Value = StandardTexts.FileType.EDF_Plus_Discontinuous;
						break;
					default:
						throw new ArgumentOutOfRangeException( nameof(value), $"Argument is not a valid {nameof(EdfFileType)} value" );
				}
			}
		}
		
		#endregion 

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the EdfFileHeader class
		/// </summary>
		public EdfFileHeader() { }

		/// <summary>
		/// Reads an EDF file Header from the indicated file
		/// </summary>
		/// <param name="filename">The fully-qualified path to the EDF file</param>
		public EdfFileHeader( string filename )
		{
			using( var file = File.OpenRead( filename ) )
			{
				using( var reader = new BinaryReader( file, Encoding.ASCII, true ) )
				{
					ReadFrom( reader );
				}
			}
		}

		/// <summary>
		/// Reads an EDF file header from the provided Stream.
		/// </summary>
		/// <param name="source">A Stream (usually a FileStream) object containing the EdfHeader data to be loaded.</param>
		public EdfFileHeader( Stream source )
		{
			using( var reader = new BinaryReader( source, Encoding.ASCII, true ) )
			{
				ReadFrom( reader );
			}
		}

		#endregion

		#region Public Read/Write functions

		public bool IsCompatibleWith( EdfFileHeader other )
		{
			if( Math.Abs( DurationOfDataRecord.Value - other.DurationOfDataRecord ) > 1e-4 )
				return false;
			
			if( NumberOfSignals.Value != other.NumberOfSignals.Value )
				return false;

			if( !areListsCompatible( Labels,               other.Labels ) ) return false;
			if( !areListsCompatible( TransducerType,       other.TransducerType ) ) return false;
			if( !areListsCompatible( PhysicalDimension,    other.PhysicalDimension ) ) return false;
			if( !areListsCompatible( PhysicalMinimum,      other.PhysicalMinimum ) ) return false;
			if( !areListsCompatible( PhysicalMaximum,      other.PhysicalMaximum ) ) return false;
			if( !areListsCompatible( DigitalMinimum,       other.DigitalMinimum ) ) return false;
			if( !areListsCompatible( DigitalMaximum,       other.DigitalMaximum ) ) return false;
			if( !areListsCompatible( Prefiltering,         other.Prefiltering ) ) return false;
			if( !areListsCompatible( SamplesPerDataRecord, other.SamplesPerDataRecord ) ) return false;
			if( !areListsCompatible( SignalReserved,       other.SignalReserved ) ) return false;
			
			bool areListsCompatible<T>( List<T> lhs, List<T> rhs ) where T : EdfAsciiField
			{
				if( lhs.Count != rhs.Count ) return false;

				for( int i = 0; i < lhs.Count; i++ )
				{
					if( string.Compare( lhs.ToString(), rhs.ToString(), StringComparison.Ordinal ) != 0 )
					{
						return false;
					}
				}

				return true;
			}

			return true;
		}

		/// <summary>
		/// Writes all EDF File Header information to the provided Stream
		/// </summary>
		public void WriteTo( BinaryWriter buffer )
		{
			HeaderRecordSize.Value = calculateHeaderRecordSize();

			// Write the fixed-size portion of the header
			Version.WriteTo( buffer );
			PatientIdentification.WriteTo( buffer );
			RecordingIdentification.WriteTo( buffer );
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

		/// <summary>
		/// Reads all EDF File Header information from the provided stream
		/// </summary>
		public void ReadFrom( BinaryReader buffer )
		{
			// Read the fixed-size portion of the header
			Version.ReadFrom( buffer );
			PatientIdentification.ReadFrom( buffer );
			RecordingIdentification.ReadFrom( buffer );
			StartTime.ReadFrom( buffer );
			HeaderRecordSize.ReadFrom( buffer );
			Reserved.ReadFrom( buffer );
			NumberOfDataRecords.ReadFrom( buffer );
			DurationOfDataRecord.ReadFrom( buffer );
			NumberOfSignals.ReadFrom( buffer );

			// Automatically replace the PatientInfo field with an EdfPatientInfo instance when appropriate. 
			if( EdfPatientInfo.IsMatch( PatientIdentification.Value ) )
			{
				PatientIdentification = EdfPatientInfo.Parse( PatientIdentification.Value );
			}
			
			// Automatically replace the RecordingInfo field with an EdfRecordingInfo instance when appropriate.
			if( EdfRecordingInfo.IsMatch( RecordingIdentification.Value ) )
			{
				RecordingIdentification = EdfRecordingInfo.Parse( RecordingIdentification.Value );
			}

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
		
		internal void UpdateSignalFields( List<EdfStandardSignal> signals, List<EdfAnnotationSignal> annotations )
		{
			NumberOfSignals.Value = signals.Count + annotations.Count;

			Labels               = signals.Select( x => x.Label ).Concat( annotations.Select( x => x.Label ) ).ToList();
			TransducerType       = signals.Select( x => x.TransducerType ).Concat( annotations.Select( x => x.TransducerType ) ).ToList();
			PhysicalDimension    = signals.Select( x => x.PhysicalDimension ).Concat( annotations.Select( x => x.PhysicalDimension ) ).ToList();
			PhysicalMinimum      = signals.Select( x => x.PhysicalMinimum ).Concat( annotations.Select( x => x.PhysicalMinimum ) ).ToList();
			PhysicalMaximum      = signals.Select( x => x.PhysicalMaximum ).Concat( annotations.Select( x => x.PhysicalMaximum ) ).ToList();
			DigitalMinimum       = signals.Select( x => x.DigitalMinimum ).Concat( annotations.Select( x => x.DigitalMinimum ) ).ToList();
			DigitalMaximum       = signals.Select( x => x.DigitalMaximum ).Concat( annotations.Select( x => x.DigitalMaximum ) ).ToList();
			Prefiltering         = signals.Select( x => x.Prefiltering ).Concat( annotations.Select( x => x.Prefiltering ) ).ToList();
			SamplesPerDataRecord = signals.Select( x => x.NumberOfSamplesPerRecord ).Concat( annotations.Select( x => x.NumberOfSamplesPerRecord ) ).ToList();
			SignalReserved       = signals.Select( x => x.Reserved ).Concat( annotations.Select( x => x.Reserved ) ).ToList();
		}

		internal void AllocateSignals( List<EdfStandardSignal> signals, List<EdfAnnotationSignal> annotations )
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
					NumberOfDataRecords      = NumberOfDataRecords,
					Reserved                 = SignalReserved[ i ]
				};

				if( header.Label.Value.Equals( StandardTexts.SignalType.EdfAnnotations ) )
				{
					annotations.Add( new EdfAnnotationSignal( header ) );
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

	/// <summary>
	/// Represents the EDF file type
	/// </summary>
	public enum EdfFileType
	{
		/// <summary>
		/// Standard (legacy) EDF file. No annotations or EDF+ extensions are supported. 
		/// </summary>
		EDF,
		/// <summary>
		/// EDF+ file. Annotations and EDF+ extensions are supported. Data Records are stored contiguously.
		/// </summary>
		EDF_Plus,
		/// <summary>
		/// EDF+ file. Annotations and EDF+ extensions are supported. Data Records may not be stored contiguously.
		/// </summary>
		EDF_Plus_Discontinuous
	}
}
