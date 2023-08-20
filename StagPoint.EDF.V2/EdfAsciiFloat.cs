using System.Globalization;

namespace StagPoint.EDF.V2;

public class EdfAsciiFloat
{
	#region Public properties

	public int FieldLength { get; private set; }

	public double Value { get; set; }
	
	public bool RequireSignPrefix { get; private set; }

	#endregion
	
	#region Private fields
	
	// NOTE: The following only keeps 8 digits of precision. If this is insufficient, this is where you'd change it ;)
	private const string STRING_FORMAT = "0.########"; 
	
	#endregion

	#region Constructors

	public EdfAsciiFloat( int fieldLength, bool requireSignPrefix = false )
	{
		this.FieldLength       = fieldLength;
		this.RequireSignPrefix = requireSignPrefix;
	}

	public EdfAsciiFloat( int fieldLength, double value, bool requireSignPrefix = false ) 
		: this( fieldLength, requireSignPrefix )
	{
		this.Value = value;
	}
	
	#endregion 

	#region IEdfAsciiValue interface implementation

	public void ReadFromBuffer( BinaryReader buffer )
	{
		var temp = BufferHelper.ReadFromBuffer( buffer, this.FieldLength );
		
		this.Value = double.Parse( temp );
	}

	public void WriteToBuffer( BinaryWriter buffer )
	{
		var stringVal = this.Value.ToString( STRING_FORMAT, CultureInfo.InvariantCulture );
		
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
		return Value.ToString( CultureInfo.InvariantCulture );
	}

	public override int GetHashCode()
	{
		// ReSharper disable once NonReadonlyMemberInGetHashCode
		return Value.GetHashCode();
	}

	public static implicit operator double( EdfAsciiFloat field )
	{
		return field.Value;
	}

	#endregion
}
