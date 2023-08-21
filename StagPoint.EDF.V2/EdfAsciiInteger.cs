using System.IO;

namespace StagPoint.EDF.Net
{

	/// <summary>
	/// Stores a fixed-length ASCII string representing a whole number. For consistency
	/// </summary>
	public class EdfAsciiInteger : EdfAsciiField
	{
		#region Public properties

		public int Value { get; set; }

		public bool RequireSignPrefix { get; private set; }

		#endregion

		#region Constructors

		public EdfAsciiInteger( int fieldLength, bool requireSignPrefix = false ) 
			: base( fieldLength )
		{
			this.RequireSignPrefix = requireSignPrefix;
		}

		public EdfAsciiInteger( int fieldLength, int value, bool forceSignPrefix = false )
			: this( fieldLength, forceSignPrefix )
		{
			this.Value = value;
		}

		#endregion

		#region IEdfAsciiValue interface implementation

		public override void ReadFrom( BinaryReader buffer )
		{
			var temp = BufferHelper.ReadFromBuffer( buffer, this.FieldLength );

			this.Value = int.Parse( temp );
		}

		public override void WriteTo( BinaryWriter buffer )
		{
			var stringVal            = this.Value.ToString();
			var remainingFieldLength = FieldLength;

			if( RequireSignPrefix && this.Value >= 0 )
			{
				buffer.Write( '+' );
				remainingFieldLength -= 1;
			}

			BufferHelper.WriteToBuffer( buffer, stringVal, remainingFieldLength );
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
