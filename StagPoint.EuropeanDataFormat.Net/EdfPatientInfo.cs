// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace StagPoint.EDF.Net
{
	/// <summary>
	/// Stores Patient Identification information according to the
	/// <a href="https://www.edfplus.info/specs/edfplus.html#additionalspecs">EDF+ specification</a>
	/// </summary>
	public class EdfPatientInfo : EdfAsciiString
	{
		#region Public properties

		/// <summary>
		/// The code by which the patient is known in the hospital administration.
		/// </summary>
		public string Code { get; set; }

		/// <summary>
		/// The patient's sex assigned at birth (Note that the EDF+ specification only allows for a single
		/// character, which is expected to be either M or F, but that is not enforced by this library).
		/// </summary>
		public string Sex { get; set; }

		/// <summary>
		/// The patient's date of birth
		/// </summary>
		public DateTime? BirthDate { get; set; }

		/// <summary>
		/// The patient's name
		/// </summary>
		public string PatientName { get; set; }

		/// <summary>
		/// Any additional data not included in the primary subfields 
		/// </summary>
		public List<string> AdditionalFields { get; } = new List<string>();

		/// <summary>
		/// Returns all Patient Identification data as a single formatted string 
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
		/// Initializes a new instance of the EdfPatientInfo class
		/// </summary>
		public EdfPatientInfo( int fieldLength ) : base( fieldLength )
		{
			if( fieldLength != 80 )
			{
				throw new Exception( "Invalid field length for the Patient Identification field" );
			}
		}

		/// <summary>
		/// Initializes a new instance of the EdfPatientInfo class
		/// </summary>
		public EdfPatientInfo( int fieldLength, string value ) : base( fieldLength, value )
		{
			if( fieldLength != 80 )
			{
				throw new Exception( "Invalid field length for the Patient Identification field" );
			}
		}

		#endregion

		#region Static functions

		/// <summary>
		/// Returns true if a string value matches the specification for an <a href="https://www.edfplus.info/specs/edfplus.html#additionalspecs">EDF+ Patient Identification field</a> 
		/// </summary>
		/// <param name="value">The value to be checked.</param>
		/// <returns>Returns true if the string matches the pattern required to conform to the EDF+ Patient Identification Field specification.</returns>
		internal static bool IsMatch( string value )
		{
			const string MATCH_PATTERN = @"[\x21-\x7E]+\x20[\x21-\x7E]+\x20[\x21-\x7E]+\x20[\x21-\x7E]+($|[\x20-\x7E]+)";

			return Regex.IsMatch( value, MATCH_PATTERN );
		}

		/// <summary>
		/// Attempts to parse subfield information from a text buffer. Returns an EdfPatientInfo if successful.
		/// </summary>
		internal static EdfPatientInfo Parse( string buffer, bool throwOnFormatInvalid = true )
		{
			EdfPatientInfo result = new EdfPatientInfo( 80 );
			
			if( TryParse( buffer, result ) )
			{
				return result;
			}

			if( throwOnFormatInvalid )
			{
				throw new FormatException( $"The value '{buffer}' does not appear to be a valid format for {nameof( EdfPatientInfo )}" );
			}

			return null;
		}

		/// <summary>
		/// Attempts to parse subfield information from a text buffer. Returns an EdfPatientInfo if successful.
		/// </summary>
		private static bool TryParse( string buffer, EdfPatientInfo field )
		{
			var parts = buffer.Split( ' ' );
			if( parts.Length < 4 )
			{
				return false;
			}

			string    code        = parts[ 0 ].Replace( '_', ' ' );
			string    sex         = parts[ 1 ].Substring( 0, 1 );
			string    patientName = parts[ 3 ].Replace( '_', ' ' );
			DateTime? birthDate   = null;

			if( String.Compare( parts[ 2 ], "X", StringComparison.OrdinalIgnoreCase ) != 0 )
			{
				if( !DateTime.TryParse( parts[ 2 ], out DateTime parsedDate ) )
				{
					return false;
				}
				birthDate = parsedDate;
			}

			if( string.Compare( code,        "X", StringComparison.Ordinal ) == 0 ) { code        = string.Empty; }
			if( string.Compare( sex,         "X", StringComparison.Ordinal ) == 0 ) { sex         = string.Empty; }
			if( string.Compare( patientName, "X", StringComparison.Ordinal ) == 0 ) { patientName = string.Empty; }

			field.Code        = code;
			field.Sex         = sex;
			field.BirthDate   = birthDate;
			field.PatientName = patientName;

			field.AdditionalFields.Clear();
			for( int i = 4; i < parts.Length; i++ )
			{
				field.AdditionalFields.Add( parts[ i ] );
			}

			return true;
		}

		#endregion

		#region Base class overrides

		internal override void ReadFrom( BinaryReader buffer )
		{
			// This class will not be used to read directly from a buffer
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns a formatted string containing the Patient Identification information
		/// </summary>
		public override string ToString()
		{
			var buffer = new StringBuilder();

			buffer.Append( string.IsNullOrEmpty( Code ) ? "X" : Code?.Replace( ' ', '_' ) );
			buffer.Append( ' ' );

			buffer.Append( string.IsNullOrEmpty( Sex ) ? "X" : Sex );
			buffer.Append( ' ' );

			buffer.Append( BirthDate?.ToString( "dd-MMM-yyyy" ).ToUpperInvariant() ?? "X" );
			buffer.Append( ' ' );

			buffer.Append( string.IsNullOrEmpty( PatientName ) ? "X" : PatientName?.Replace( ' ', '_' ) );

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
