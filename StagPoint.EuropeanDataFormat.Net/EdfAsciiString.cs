// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

using System;
using System.IO;
using System.Text;

namespace StagPoint.EDF.Net
{

	/// <summary>
	/// Stores a fixed-length ASCII string, which will be right-padded with spaces as necessary to maintain
	/// the fixed-length requirement. In the Header file, these string must only contain the ASCII characters
	/// 32..126 (inclusive).
	/// </summary>
	public class EdfAsciiString : EdfAsciiField
	{
		#region Public properties

		/// <summary>
		/// Gets or sets the string value stored in the field
		/// </summary>
		/// <exception cref="ArgumentNullException">You may not assign NULL (use string.Empty instead)</exception>
		public virtual string Value
		{
			get { return _value; }
			set
			{
				if( value == null )
				{
					throw new ArgumentNullException( $"NULL values are not supported by the {nameof( Value )} property. Use String.Empty for empty strings.", nameof( Value ) );
				}

				if( value.Length < FieldLength )
				{
					_value = value;
				}
				else
				{
					_value = value.Substring( 0, FieldLength );
				}
			}
		}

		#endregion

		#region Constructors

		/// <inheritdoc />
		public EdfAsciiString( int fieldLength ) : base( fieldLength ) { }

		/// <summary>
		/// Initializes a new instance of the EdfAsciiString field 
		/// </summary>
		/// <param name="fieldLength">The length of the ASCII string used to store this field's value <see cref="EdfAsciiField.FieldLength"/></param>
		/// <param name="value">The initial value of this field</param>
		public EdfAsciiString( int fieldLength, string value )
			: base( fieldLength )
		{
			this.Value = value;
		}

		#endregion

		#region Private fields

		private string _value = string.Empty;

		#endregion

		#region EdfAsciiField overrides

		/// <inheritdoc />
		internal override void WriteTo( BinaryWriter buffer )
		{
			if( Value == null )
			{
				throw new NullReferenceException( $"The {nameof( Value )} property of the {nameof( EdfAsciiString )} object was set to NULL" );
			}

			BufferHelper.WriteToBuffer( buffer, Value, FieldLength );
		}

		/// <inheritdoc />
		internal override void ReadFrom( BinaryReader buffer )
		{
			_value = BufferHelper.ReadFromBuffer( buffer, this.FieldLength );
		}

		#endregion

		#region Base class overrides and implicit type conversion

		/// <inheritdoc />
		public override string ToString()
		{
			return _value;
		}

		/// <summary>
		/// Returns the string representation of the value of the field 
		/// </summary>
		public static implicit operator string( EdfAsciiString field )
		{
			// It may be tempting to change this function to return the _value field directly. Don't. Subclasses rely on calling ToString(). 
			return field.ToString();
		}

		#endregion
	}
}
