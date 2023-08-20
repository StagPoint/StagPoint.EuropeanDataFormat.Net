using System.Text;

namespace StagPoint.EDF.V2;

public class EdfStringField : IEdfHeaderField
{
	#region Public properties

	public int FieldLength { get; private set; }

	public string Value
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

	public EdfStringField( int fieldLength )
	{
		this.FieldLength = fieldLength;
	}

	public EdfStringField( int fieldLength, string value ) 
		: this( fieldLength )
	{
		this.Value = value;
	}
	
	#endregion 

	#region Private fields

	private string _value = string.Empty;

	#endregion

	#region IEdfHeaderField interface implementation

	public void WriteToBuffer( BinaryWriter buffer )
	{
		if( _value == null )
		{
			throw new NullReferenceException( $"The {nameof( Value )} property of the {nameof( EdfStringField )} object was set to NULL" );
		}

		BufferHelper.WriteToBuffer( buffer, _value, FieldLength );
	}

	public void ReadFromBuffer( BinaryReader buffer )
	{
		_value = BufferHelper.ReadFromBuffer( buffer, this.FieldLength );
	}

	#endregion

	#region Base class overrides and implicit type conversion

	public override string ToString()
	{
		return _value;
	}

	public static implicit operator string( EdfStringField field )
	{
		return field._value;
	}

	#endregion
}
