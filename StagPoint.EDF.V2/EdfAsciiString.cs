using System.Text;

namespace StagPoint.EDF.V2;

/// <summary>
/// Stores a fixed-length ASCII string, which will be right-padded with spaces as necessary to maintain
/// the fixed-length requirement. In the Header file, these string must only contain the ASCII characters
/// 32..126 (inclusive).
/// </summary>
public class EdfAsciiString
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

	public EdfAsciiString( int fieldLength )
	{
		this.FieldLength = fieldLength;
	}

	public EdfAsciiString( int fieldLength, string value ) 
		: this( fieldLength )
	{
		this.Value = value;
	}
	
	#endregion 

	#region Private fields

	private string _value = string.Empty;

	#endregion

	#region IEdfAsciiValue interface implementation

	public void WriteToBuffer( BinaryWriter buffer )
	{
		if( _value == null )
		{
			throw new NullReferenceException( $"The {nameof( Value )} property of the {nameof( EdfAsciiString )} object was set to NULL" );
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

	public static implicit operator string( EdfAsciiString field )
	{
		return field._value;
	}

	#endregion
}
