using StagPoint.EDF.Net;

namespace StagPoint.Edf.Tests;

[TestClass]
public class EdfAsciiDateTime_Tests
{
	[TestMethod]
	public void RoundTrippedDateTimeStoresExactlySixteenBytes()
	{
		using var buffer = new MemoryStream( new byte[ 1024 ] );
		using var writer = new BinaryWriter( buffer );

		// Obtain the current date and time, to the nearest whole second
		var testDate = Trim( DateTime.Now, TimeSpan.TicksPerSecond );

		var dateField = new EdfAsciiDateTime( testDate );
		dateField.WriteTo( writer );
		Assert.AreEqual( dateField.FieldLength, buffer.Position, "The number of bytes written doesn't match the field length" );

		buffer.Position = 0;
		dateField.ReadFrom( new BinaryReader( buffer ) );
		
		Assert.AreEqual( testDate, dateField, "Round-tripped date does not match original value" );
	}

	public static DateTime Trim( DateTime date, long ticks )
	{
		return new DateTime( date.Ticks - (date.Ticks % ticks), date.Kind );
	}
}
