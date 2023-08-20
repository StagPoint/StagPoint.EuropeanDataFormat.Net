namespace StagPoint.EDF.V2;

public class EdfLongIntegerField : IEdfHeaderField
{
	#region Public properties

	public int FieldLength { get; private set; }

	public long Value { get; set; }

	#endregion

	#region Constructors

	public EdfLongIntegerField( int fieldLength )
	{
		this.FieldLength = fieldLength;
	}

	public EdfLongIntegerField( int fieldLength, long value ) 
		: this( fieldLength )
	{
		this.Value = value;
	}
	
	#endregion 

	#region IEdfHeaderField interface implementation

	public void ReadFromBuffer( BinaryReader buffer )
	{
		var temp = BufferHelper.ReadFromBuffer( buffer, this.FieldLength );
		
		this.Value = long.Parse( temp );
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

	public static implicit operator long( EdfLongIntegerField field )
	{
		return field.Value;
	}

	#endregion
}
