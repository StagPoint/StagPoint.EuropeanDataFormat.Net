// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

using System.Globalization;
using System.IO;

namespace StagPoint.EDF.Net
{
	/// <summary>
	/// An EDF Field whose floating point value is stored in the header as a fixed-length ASCII string
	/// </summary>
	public class EdfAsciiFloat : EdfAsciiField
	{
		#region Public properties

		/// <summary>
		/// Gets or sets the floating point value of this field 
		/// </summary>
		public double Value { get; set; }

		#endregion

		#region Private fields

		// NOTE: The following only keeps 8 digits of precision. If this is insufficient, this is where you'd change it ;)
		private const string STRING_FORMAT = "0.########";

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the EdfAsciiFloat class
		/// </summary>
		/// <param name="fieldLength">The length of the ASCII string used to store this field's value <see cref="EdfAsciiField.FieldLength"/></param>
		public EdfAsciiFloat( int fieldLength ) : base( fieldLength ) { }

		/// <summary>
		/// Initializes a new instance of the EdfAsciiFloat class
		/// </summary>
		/// <param name="fieldLength">The length of the ASCII string used to store this field's value <see cref="EdfAsciiField.FieldLength"/></param>
		/// <param name="value">The initial floating point value of this field</param>
		public EdfAsciiFloat( int fieldLength, double value )
			: this( fieldLength )
		{
			this.Value = value;
		}

		#endregion

		#region EdfAsciiField overrides

		/// <inheritdoc />
		internal override void ReadFrom( BinaryReader buffer )
		{
			var temp = BufferHelper.ReadFromBuffer( buffer, this.FieldLength );

			this.Value = double.Parse( temp );
		}

		/// <inheritdoc />
		internal override void WriteTo( BinaryWriter buffer )
		{
			// From the specification: Never use any digit grouping symbol in numbers.
			// Never use a comma "," for a for a decimal separator.
			// When a decimal separator is required, use a dot (".").
			//    https://www.edfplus.info/specs/edfplus.html#additionalspecs:~:text=Never%20use%20any%20digit%20grouping%20symbol
			var stringVal = this.Value.ToString( STRING_FORMAT, CultureInfo.InvariantCulture );

			BufferHelper.WriteToBuffer( buffer, stringVal, FieldLength );
		}

		#endregion

		#region Base class overrides and implicit type conversion
	
		/// <inheritdoc />
		public override string ToString()
		{
			return Value.ToString( CultureInfo.InvariantCulture );
		}

		/// <summary>
		/// Returns the double-precision floating point value stored in the field
		/// </summary>
		public static implicit operator double( EdfAsciiFloat field )
		{
			return field.Value;
		}

		#endregion
	}
}
