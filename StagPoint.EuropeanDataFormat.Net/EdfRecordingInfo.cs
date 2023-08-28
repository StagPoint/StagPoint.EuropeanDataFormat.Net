// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace StagPoint.EDF.Net
{
	/// <summary>
	/// Stores Recording Identification information according to the
	/// <a href="https://www.edfplus.info/specs/edfplus.html#additionalspecs">EDF+ specification</a>
	/// </summary>
	public class EdfRecordingInfo : EdfAsciiString
	{
		#region Public properties

		/// <summary>
		/// The date on which the recording was startedS
		/// </summary>
		public DateTime StartDate { get; set; }

		/// <summary>
		/// The hospital administration code of the investigation, i.e. EEG number or PSG number
		/// </summary>
		public string Code { get; set; }
		
		/// <summary>
		/// A code specifying the responsible investigator or technician
		/// </summary>
		public string Technician { get; set; }

		/// <summary>
		/// A code specifying the equipment used for the recording
		/// </summary>
		public string Equipment { get; set; }

		/// <summary>
		/// Any additional data not included in the primary subfields 
		/// </summary>
		public List<string> AdditionalFields { get; } = new List<string>();

		/// <summary>
		/// Returns all Recording Identification data as a single formatted string 
		/// </summary>
		/// <exception cref="FieldAccessException"></exception>
		public override string Value
		{
			get => ToString();
			set
			{
				if( !TryParse( value, this ) )
				{
					throw new FormatException();
				}
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the EdfRecordingInfo class
		/// </summary>
		public EdfRecordingInfo( int fieldLength ) : base( fieldLength )
		{
			if( fieldLength != 80 )
			{
				throw new Exception( "Invalid field length for the Recording Identification field" );
			}
		}

		/// <summary>
		/// Initializes a new instance of the EdfRecordingInfo class
		/// </summary>
		public EdfRecordingInfo( int fieldLength, string value ) : base( fieldLength, value )
		{
			if( fieldLength != 80 )
			{
				throw new Exception( "Invalid field length for the Recording Identification field" );
			}
		}

		#endregion

		#region Static functions

		/// <summary>
		/// Returns true if a string value matches the specification for an <a href="https://www.edfplus.info/specs/edfplus.html#additionalspecs">EDF+ Recording Identification field</a> 
		/// </summary>
		/// <param name="value">The value to be checked.</param>
		/// <returns>Returns true if the string matches the pattern required to conform to the EDF+ Recording Identification Field specification.</returns>
		internal static bool IsMatch( string value )
		{
			const string MATCH_PATTERN = @"Startdate \d{2}-[A-Za-z0-9_]{3}-\d{4}\x20[\x21-\x7E]+\x20[\x21-\x7E]+\x20[\x21-\x7E]+($|[\x20-\x7E]+)";

			return Regex.IsMatch( value, MATCH_PATTERN );
		}

		/// <summary>
		/// Attempts to parse subfield information from a text buffer. Returns an EdfRecordingInfo if successful.
		/// </summary>
		internal static EdfRecordingInfo Parse( string buffer, bool throwOnFormatInvalid = true )
		{
			EdfRecordingInfo result = new EdfRecordingInfo( 80 );
			
			if( TryParse( buffer, result ) )
			{
				return result;
			}

			if( throwOnFormatInvalid )
			{
				throw new FormatException( $"The value '{buffer}' does not appear to be a valid format for {nameof( EdfRecordingInfo )}" );
			}

			return null;
		}

		/// <summary>
		/// Attempts to parse subfield information from a text buffer. Returns an EdfRecordingInfo if successful.
		/// </summary>
		private static bool TryParse( string buffer, EdfRecordingInfo field )
		{
			var parts = buffer.Split( ' ' );
			if( parts.Length < 5 )
			{
				return false;
			}

			if( string.Compare( parts[ 0 ], "Startdate", StringComparison.OrdinalIgnoreCase ) != 0 )
			{
				return false;
			}

			DateTime startDate  = default;
			string   code       = parts[ 2 ].Replace( '_', ' ' );
			string   technician = parts[ 3 ].Replace( '_', ' ' );
			string   equipment  = parts[ 4 ].Replace( '_', ' ' );

			if( String.Compare( parts[ 1 ], "X", StringComparison.OrdinalIgnoreCase ) == 0 )
			{
				return false;
			}
			else
			{
				if( !DateTime.TryParse( parts[ 1 ], out DateTime parsedDate ) )
				{
					return false;
				}
				
				startDate = parsedDate;
			}

			if( string.Compare( code,       "X", StringComparison.Ordinal ) == 0 ) { code       = string.Empty; }
			if( string.Compare( technician, "X", StringComparison.Ordinal ) == 0 ) { technician = string.Empty; }
			if( string.Compare( equipment,  "X", StringComparison.Ordinal ) == 0 ) { equipment  = string.Empty; }

			field.StartDate  = startDate;
			field.Code       = code;
			field.Technician = technician;
			field.Equipment  = equipment;

			field.AdditionalFields.Clear();
			for( int i = 5; i < parts.Length; i++ )
			{
				field.AdditionalFields.Add( parts[ i ] );
			}

			return true;
		}

		#endregion

		#region Base class overrides

		internal override void ReadFrom( BinaryReader buffer )
		{
			// NOTE: It is not expected that this function ever be called by user code. 
			
			base.ReadFrom( buffer );

			if( !TryParse( this.Value, this ) )
			{
				throw new FormatException( $"The value '{Value}' does not appear to be a valid format for {nameof( EdfRecordingInfo )}" );
			}
		}

		/// <summary>
		/// Returns a formatted string containing the Recording Identification information
		/// </summary>
		public override string ToString()
		{
			var buffer = new StringBuilder();

			buffer.Append( "Startdate " );
			buffer.Append( StartDate.ToString( "dd-MMM-yyyy" ).ToUpperInvariant() ?? "X" );
			buffer.Append( ' ' );

			buffer.Append( string.IsNullOrEmpty( Code ) ? "X" : Code?.Replace( ' ', '_' ) );
			buffer.Append( ' ' );

			buffer.Append( string.IsNullOrEmpty( Technician ) ? "X" : Technician );
			buffer.Append( ' ' );

			buffer.Append( string.IsNullOrEmpty( Equipment ) ? "X" : Equipment?.Replace( ' ', '_' ) );

			foreach( var field in AdditionalFields )
			{
				buffer.Append( ' ' );
				buffer.Append( field );
			}

			return buffer.ToString();
		}

		#endregion
	}
}
