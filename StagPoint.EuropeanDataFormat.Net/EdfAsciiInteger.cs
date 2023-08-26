// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

using System.IO;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace StagPoint.EDF.Net
{

	/// <summary>
	/// Stores a fixed-length ASCII string representing a whole number. For consistency
	/// </summary>
	public class EdfAsciiInteger : EdfAsciiField
	{
		#region Public properties

		public int Value { get; set; }

		#endregion

		#region Constructors

		public EdfAsciiInteger( int fieldLength ) : base( fieldLength ) { }

		public EdfAsciiInteger( int fieldLength, int value )
			: base( fieldLength )
		{
			this.Value = value;
		}

		#endregion

		#region EdfAsciiField overrides

		internal override void ReadFrom( BinaryReader buffer )
		{
			var temp = BufferHelper.ReadFromBuffer( buffer, this.FieldLength );

			this.Value = int.Parse( temp );
		}

		internal override void WriteTo( BinaryWriter buffer )
		{
			var stringVal = this.Value.ToString();

			BufferHelper.WriteToBuffer( buffer, stringVal, FieldLength );
		}

		#endregion

		#region Base class overrides and implicit type conversion

		public override string ToString()
		{
			return Value.ToString();
		}

		public override int GetHashCode()
		{
			// ReSharper disable once NonReadonlyMemberInGetHashCode
			return Value.GetHashCode();
		}

		public static implicit operator int( EdfAsciiInteger field )
		{
			return field.Value;
		}

		#endregion		
	}
}
