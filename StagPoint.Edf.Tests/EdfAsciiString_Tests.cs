using StagPoint.EDF.Net;

namespace StagPoint.Edf.Tests;

[TestClass]
public class EdfAsciiString_Tests
{
	[TestMethod]
	public void EdfStringFieldWriteExactNumberOfBytes()
	{
		using var buffer = new MemoryStream( new byte[ 1024 ] );
		using var writer = new BinaryWriter( buffer );

		// Test empty string first
		buffer.Position = 0;
		var stringField = new EdfAsciiString( 20 );
		stringField.WriteTo( writer );
		Assert.AreEqual( stringField.FieldLength, buffer.Position, "The number of bytes written doesn't match the field length" );
		
		// Test shorter string
		buffer.Position   = 0;
		stringField       = new EdfAsciiString( 20 );
		stringField.Value = "Test Value";
		stringField.WriteTo( writer );
		Assert.AreEqual( stringField.FieldLength, buffer.Position, "The number of bytes written doesn't match the field length" );
		
		// Test much longer string
		buffer.Position   = 0;
		stringField       = new EdfAsciiString( 20 );
		stringField.Value = "Now is the time for all good men to come to the aid of their country.";
		stringField.WriteTo( writer );
		Assert.AreEqual( stringField.FieldLength, buffer.Position, "The number of bytes written doesn't match the field length" );
		
		// Expect an ArgumentNullException when assigning null to an EdfAsciiString
		try
		{
			stringField.Value = null!;
			
			// Test fails if an ArgumentNullException was not thrown
			Assert.Fail( "Expected an ArgumentNullException" );
		}
		catch( ArgumentNullException ) { }
	}

	[TestMethod]
	public void EdfStringRoundTrip()
	{
		using var buffer = new MemoryStream( new byte[ 1024 ] );
		using var writer = new BinaryWriter( buffer );
		using var reader = new BinaryReader( buffer );

		// Test empty string first
		buffer.Position = 0;
		var stringField = new EdfAsciiString( 20 );
		stringField.WriteTo( writer );
		Assert.AreEqual( stringField.FieldLength, buffer.Position, "The number of bytes written doesn't match the field length" );
		buffer.Position = 0;
		stringField.ReadFrom( reader );
		Assert.AreEqual( string.Empty, stringField.Value );
		
		// Test shorter string
		buffer.Position   = 0;
		stringField       = new EdfAsciiString( 20 );
		stringField.Value = "Test Value";
		stringField.WriteTo( writer );
		Assert.AreEqual( stringField.FieldLength, buffer.Position, "The number of bytes written doesn't match the field length" );
		
		// Test much longer string
		buffer.Position   = 0;
		stringField       = new EdfAsciiString( 20 );
		stringField.Value = "Now is the time for all good men to come to the aid of their country.";
		stringField.WriteTo( writer );
		Assert.AreEqual( stringField.FieldLength, buffer.Position, "The number of bytes written doesn't match the field length" );
		
	}
}
