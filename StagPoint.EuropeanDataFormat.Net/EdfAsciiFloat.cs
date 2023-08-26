// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

using System.Globalization;
using System.IO;

namespace StagPoint.EDF.Net
{

	public class EdfAsciiFloat : EdfAsciiField
	{
		#region Public properties

		public double Value { get; set; }

		#endregion

		#region Private fields

		// NOTE: The following only keeps 8 digits of precision. If this is insufficient, this is where you'd change it ;)
		private const string STRING_FORMAT = "0.########";

		#endregion

		#region Constructors

		public EdfAsciiFloat( int fieldLength ) : base( fieldLength ) { }

		public EdfAsciiFloat( int fieldLength, double value, bool requireSignPrefix = false )
			: this( fieldLength )
		{
			this.Value = value;
		}

		#endregion

		#region EdfAsciiField overrides

		internal override void ReadFrom( BinaryReader buffer )
		{
			var temp = BufferHelper.ReadFromBuffer( buffer, this.FieldLength );

			this.Value = double.Parse( temp );
		}

		internal override void WriteTo( BinaryWriter buffer )
		{
			var stringVal = this.Value.ToString( STRING_FORMAT, CultureInfo.InvariantCulture );

			BufferHelper.WriteToBuffer( buffer, stringVal, FieldLength );
		}

		#endregion

		#region Base class overrides and implicit type conversion
	
		public override string ToString()
		{
			return Value.ToString( CultureInfo.InvariantCulture );
		}

		public static implicit operator double( EdfAsciiFloat field )
		{
			return field.Value;
		}

		#endregion
	}
}
