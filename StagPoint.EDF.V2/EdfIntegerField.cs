namespace StagPoint.EDF.V2;

public class EdfIntegerField : IEdfHeaderField
{
	#region Public properties

	public int FieldLength { get; private set; }

	public int Value { get; set; }

	#endregion

	#region Constructors

	public EdfIntegerField( int fieldLength )
	{
		this.FieldLength = fieldLength;
	}

	public EdfIntegerField( int fieldLength, int value ) 
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

	public static implicit operator int( EdfIntegerField field )
	{
		return field.Value;
	}

	#endregion
}
