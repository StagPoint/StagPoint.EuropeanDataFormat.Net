namespace StagPoint.EDF.V2;

/// <summary>
/// Stores a fixed-length ASCII string representing a whole number. For consistency
/// </summary>
public class EdfAsciiInteger : IEdfHeaderField
{
	#region Public properties

	public int FieldLength { get; private set; }

	public int Value { get; set; }

	#endregion

	#region Constructors

	public EdfAsciiInteger( int fieldLength )
	{
		this.FieldLength = fieldLength;
	}

	public EdfAsciiInteger( int fieldLength, int value ) 
		: this( fieldLength )
	{
		this.Value = value;
	}
	
	#endregion 

	#region IEdfHeaderField interface implementation

	public void ReadFromBuffer( BinaryReader buffer )
	{
		var temp = BufferHelper.ReadFromBuffer( buffer, this.FieldLength );

		this.Value = int.Parse( temp );
	}

	public void WriteToBuffer( BinaryWriter buffer )
	{
		var stringVal            = this.Value.ToString();
		var remainingFieldLength = FieldLength;

		if( this.Value >= 0 )
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
