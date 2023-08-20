using System.Globalization;

namespace StagPoint.EDF.V2;

/// <summary>
/// Stores a fixed-length ASCII string representing a whole number. For consistency
/// </summary>
public class EdfAsciiDateTime
{
	#region Public properties

	public int FieldLength { get => 16; }

	public DateTime Value { get; set; }
	
	#endregion
	
	#region Constructors

	public EdfAsciiDateTime()
	{
		this.Value = DateTime.Today;
	}

	public EdfAsciiDateTime( DateTime value ) 
	{
		this.Value = value;
	}
	
	#endregion 

	#region IEdfAsciiValue interface implementation

	public void ReadFromBuffer( BinaryReader buffer )
	{
		// Dates are stored as dd.MM.yy and times as HH.mm.ss
		var dateString = BufferHelper.ReadFromBuffer( buffer, 8 );
		var timeString = BufferHelper.ReadFromBuffer( buffer, 8 );

		this.Value = DateTime.ParseExact( $"{dateString} {timeString}", "dd.MM.yy HH.mm.ss", CultureInfo.InvariantCulture );
	}

	public void WriteToBuffer( BinaryWriter buffer )
	{
		BufferHelper.WriteToBuffer( buffer, Value.ToString( "dd.MM.yy" ), 8 );
		BufferHelper.WriteToBuffer( buffer, Value.ToString( "HH.mm.ss" ), 8 );
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

	public static implicit operator DateTime( EdfAsciiDateTime field )
	{
		return field.Value;
	}

	#endregion
}
