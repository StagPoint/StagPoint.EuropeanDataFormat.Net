using StagPoint.EDF.V2;

namespace StagPoint.Edf.Tests;

[TestClass]
public class UnitTest1
{
	[TestMethod]
	public void EdfStringFieldWriteExactNumberOfBytes()
	{
		using var buffer = new MemoryStream( new byte[ 1024 ] );
		using var writer = new BinaryWriter( buffer );

		// Test empty string first
		buffer.Position = 0;
		var stringField = new EdfStringField( 20 );
		stringField.WriteToBuffer( writer );
		Assert.AreEqual( stringField.FieldLength, buffer.Position, "The number of bytes written doesn't match the field length" );
		
		// Test shorter string
		buffer.Position   = 0;
		stringField       = new EdfStringField( 20 );
		stringField.Value = "Test Value";
		stringField.WriteToBuffer( writer );
		Assert.AreEqual( stringField.FieldLength, buffer.Position, "The number of bytes written doesn't match the field length" );
		
		// Test much longer string
		buffer.Position   = 0;
		stringField       = new EdfStringField( 20 );
		stringField.Value = "Now is the time for all good men to come to the aid of their country.";
		stringField.WriteToBuffer( writer );
		Assert.AreEqual( stringField.FieldLength, buffer.Position, "The number of bytes written doesn't match the field length" );
		
		// Expect an ArgumentNullException when assigning null to an EdfStringField
		try
		{
			stringField.Value = null;
			Assert.Fail( "Expected an ArgumentNullException" );
		}
		catch( ArgumentNullException e ) { }
	}

	[TestMethod]
	public void EdfStringRoundTrip()
	{
		using var buffer = new MemoryStream( new byte[ 1024 ] );
		using var writer = new BinaryWriter( buffer );
		using var reader = new BinaryReader( buffer );

		// Test empty string first
		buffer.Position = 0;
		var stringField = new EdfStringField( 20 );
		stringField.WriteToBuffer( writer );
		Assert.AreEqual( stringField.FieldLength, buffer.Position, "The number of bytes written doesn't match the field length" );
		buffer.Position = 0;
		stringField.ReadFromBuffer( reader );
		Assert.AreEqual( string.Empty, stringField.Value );
		
		// Test shorter string
		buffer.Position   = 0;
		stringField       = new EdfStringField( 20 );
		stringField.Value = "Test Value";
		stringField.WriteToBuffer( writer );
		Assert.AreEqual( stringField.FieldLength, buffer.Position, "The number of bytes written doesn't match the field length" );
		
		// Test much longer string
		buffer.Position   = 0;
		stringField       = new EdfStringField( 20 );
		stringField.Value = "Now is the time for all good men to come to the aid of their country.";
		stringField.WriteToBuffer( writer );
		Assert.AreEqual( stringField.FieldLength, buffer.Position, "The number of bytes written doesn't match the field length" );
		
	}
}
