using System.Diagnostics;

using StagPoint.EDF.Net;

namespace StagPoint.Edf.Tests;

[TestClass]
public class EdfFileHeader_Tests
{
	[TestMethod]
	public void ReadKnownOximetryFileHeader()
	{
		string filename = Path.Combine(Environment.CurrentDirectory, "Test Files", "annotations_and_signals2.edf");
		if (!File.Exists(filename))
		{
			Assert.Fail( "Test file missing" );
		}

		var header = new EdfFileHeader( filename );

		Assert.AreEqual( "X F X Female57yrs",                                    header.PatientIdentification );
		Assert.AreEqual( "Startdate 07-MAR-2009 X Tech:X Somnoscreen_Plus_1500", header.RecordingInfo );
		Assert.AreEqual( 768,                                                    header.HeaderRecordSize );
		Assert.AreEqual( "EDF+C",                                                header.Reserved );
		Assert.AreEqual( 1,                                                      header.NumberOfDataRecords );
		Assert.AreEqual( 900.0,                                                  header.DurationOfDataRecord );
		Assert.AreEqual( new DateTime( 2009, 03, 07 ),                           header.StartTime );
		
		Assert.IsInstanceOfType( header.PatientIdentification, typeof( EdfPatientIdentificationField ) );
				
		Assert.AreEqual( 2,               header.NumberOfSignals );
		Assert.AreEqual( 2,               header.TransducerType.Count );
		Assert.AreEqual( "SaO2",          header.Labels[ 0 ] );
		Assert.AreEqual( "PulseOxiMeter", header.TransducerType[0] );
		Assert.AreEqual( "%",             header.PhysicalDimension[ 0 ] );
		Assert.AreEqual( 3600,            header.SamplesPerDataRecord[ 0 ] * header.NumberOfDataRecords );
		Assert.AreEqual( 0.0,             header.PhysicalMinimum[ 0 ] );
		Assert.AreEqual( 255.0,           header.PhysicalMaximum[ 0 ] );
		Assert.AreEqual( -32768,          header.DigitalMinimum[ 0 ] );
		Assert.AreEqual( 32767,           header.DigitalMaximum[ 0 ] );
	}

	[TestMethod]
	public void ReadKnownPSGFileHeader()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "Female57yrs 07-MAR-2009 00h00m00s APSG.edf" );
		if( !File.Exists( filename ) )
		{
			Assert.Fail( "Test file missing" );
		}

		var header = new EdfFileHeader( filename );
		
		Assert.AreEqual( "X F X Female57yrs",                                    header.PatientIdentification );
		Assert.AreEqual( "Startdate 07-MAR-2009 X Tech:X Somnoscreen_Plus_1500", header.RecordingInfo );
		Assert.AreEqual( 5376,                                                   header.HeaderRecordSize );
		Assert.AreEqual( "EDF+C",                                                header.Reserved );
		Assert.AreEqual( 90,                                                     header.NumberOfDataRecords );
		Assert.AreEqual( 10.0,                                                   header.DurationOfDataRecord );
		Assert.AreEqual( new DateTime( 2009, 03, 07 ),                           header.StartTime );

		Assert.IsInstanceOfType( header.PatientIdentification, typeof( EdfPatientIdentificationField ) );
				
		Assert.AreEqual( 20, header.NumberOfSignals );
		Assert.AreEqual( 20, header.TransducerType.Count );

		int saturationIndex = header.Labels.FindIndex( x => x.Value.Equals( "SaO2" ) );
		Assert.IsTrue( saturationIndex != -1, "Failed to find the 'SaO2' signal" );

		Assert.AreEqual( "SaO2",          header.Labels[ saturationIndex ] );
		Assert.AreEqual( "PulseOxiMeter", header.TransducerType[ saturationIndex ] );
		Assert.AreEqual( "%",             header.PhysicalDimension[ saturationIndex ] );
		Assert.AreEqual( 40,              header.SamplesPerDataRecord[ saturationIndex ] );
		Assert.AreEqual( 0.0,             header.PhysicalMinimum[ saturationIndex ] );
		Assert.AreEqual( 255.0,           header.PhysicalMaximum[ saturationIndex ] );
		Assert.AreEqual( -32768,          header.DigitalMinimum[ saturationIndex ] );
		Assert.AreEqual( 32767,           header.DigitalMaximum[ saturationIndex ] );
	}

	[TestMethod]
	public void RoundTripHeaderFile()
	{
		var tempFileName = Path.GetTempFileName();

		try
		{
			string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "Female57yrs 07-MAR-2009 00h00m00s APSG.edf" );
			if( !File.Exists( filename ) )
			{
				Assert.Fail( "Test file missing" );
			}

			var sourceHeader = readHeaderFromFile( filename );
			
			writeHeaderToFile( sourceHeader, tempFileName );

			var compareHeader = readHeaderFromFile( tempFileName );
			
			Assert.AreEqual( sourceHeader.Version.Value,              compareHeader.Version );
			Assert.AreEqual( sourceHeader.PatientIdentification.Value,          compareHeader.PatientIdentification );
			Assert.AreEqual( sourceHeader.RecordingInfo.Value,        compareHeader.RecordingInfo );
			Assert.AreEqual( sourceHeader.StartTime.Value,            compareHeader.StartTime );
			Assert.AreEqual( sourceHeader.HeaderRecordSize.Value,     compareHeader.HeaderRecordSize );
			Assert.AreEqual( sourceHeader.Reserved.Value,             compareHeader.Reserved );
			Assert.AreEqual( sourceHeader.NumberOfDataRecords.Value,  compareHeader.NumberOfDataRecords );
			Assert.AreEqual( sourceHeader.DurationOfDataRecord.Value, compareHeader.DurationOfDataRecord );
			Assert.AreEqual( sourceHeader.NumberOfSignals.Value,      compareHeader.NumberOfSignals );
		}
		finally
		{
			File.Delete( tempFileName );
		}
	}

	[TestMethod]
	public void RoundTripAllHeaderFiles()
	{
		var tempFileName = Path.GetTempFileName();

		var currentFolder = Path.Combine( Environment.CurrentDirectory, "Test Files" );
		Assert.IsTrue( Directory.Exists( currentFolder ) );

		var testFiles = Directory.GetFiles( currentFolder, "*.edf", SearchOption.AllDirectories );
		Assert.IsTrue( testFiles is { Length: > 0 }, "No test files found" );

		foreach( var filename in testFiles )
		{
			try
			{
				EdfFileHeader sourceHeader = default!;

				try
				{
					sourceHeader = readHeaderFromFile( filename );
				}
				catch( FormatException e )
				{
					// For files with an invalid date (some included intentionally for testing purposes), 
					// just skip the round-trip test. 
					if( e.ToString().Contains( "DateTime.ParseExact" ) )
					{
						continue;
					}
				}

				Debug.Assert( sourceHeader != null, nameof( sourceHeader ) + " != null" );
				writeHeaderToFile( sourceHeader, tempFileName );

				var compareHeader = readHeaderFromFile( tempFileName );
			
				Assert.AreEqual( sourceHeader.Version.Value,              compareHeader.Version );
				Assert.AreEqual( sourceHeader.PatientIdentification.Value,          compareHeader.PatientIdentification );
				Assert.AreEqual( sourceHeader.RecordingInfo.Value,        compareHeader.RecordingInfo );
				Assert.AreEqual( sourceHeader.StartTime.Value,            compareHeader.StartTime );
				Assert.AreEqual( sourceHeader.HeaderRecordSize.Value,     compareHeader.HeaderRecordSize );
				Assert.AreEqual( sourceHeader.Reserved.Value,             compareHeader.Reserved );
				Assert.AreEqual( sourceHeader.NumberOfDataRecords.Value,  compareHeader.NumberOfDataRecords );
				Assert.AreEqual( sourceHeader.DurationOfDataRecord.Value, compareHeader.DurationOfDataRecord );
				Assert.AreEqual( sourceHeader.NumberOfSignals.Value,      compareHeader.NumberOfSignals );

				Assert.IsTrue( listsAreEqual( sourceHeader.Labels,               compareHeader.Labels ) );
				Assert.IsTrue( listsAreEqual( sourceHeader.TransducerType,       compareHeader.TransducerType ) );
				Assert.IsTrue( listsAreEqual( sourceHeader.PhysicalDimension,    compareHeader.PhysicalDimension ) );
				Assert.IsTrue( listsAreEqual( sourceHeader.PhysicalMinimum,      compareHeader.PhysicalMinimum ) );
				Assert.IsTrue( listsAreEqual( sourceHeader.PhysicalMaximum,      compareHeader.PhysicalMaximum ) );
				Assert.IsTrue( listsAreEqual( sourceHeader.DigitalMinimum,       compareHeader.DigitalMinimum ) );
				Assert.IsTrue( listsAreEqual( sourceHeader.DigitalMaximum,       compareHeader.DigitalMaximum ) );
				Assert.IsTrue( listsAreEqual( sourceHeader.Prefiltering,         compareHeader.Prefiltering ) );
				Assert.IsTrue( listsAreEqual( sourceHeader.SamplesPerDataRecord, compareHeader.SamplesPerDataRecord ) );
				Assert.IsTrue( listsAreEqual( sourceHeader.SignalReserved,       compareHeader.SignalReserved ) );
			}
			finally
			{
				File.Delete( tempFileName );
			}
		}
	}

	[TestMethod]
	public void LoadHeaderWithInvalidStartDate()
	{
		// Some data files from existing medical databases contain invalid Start Date fields.  
		
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "InvalidDateField.edf" );
		if( !File.Exists( filename ) )
		{
			Assert.Fail( "Test file missing" );
		}

		using var file   = File.OpenRead( filename );
		using var reader = new BinaryReader( file );
		var       header = new EdfFileHeader();

		try
		{
			header.ReadFrom( reader );

			// Expected a FormatException
			Assert.Fail( "Expected a FormatException caused by attempting to parse an invalid DateTime" );
		}
		catch( FormatException e )
		{
			Assert.IsTrue( e.TargetSite!.DeclaringType!.Name == nameof( DateTime ), "Expected a FormatException caused by attempting to parse an invalid DateTime" );
		}
		
		// Setting EdfFileHeader.StartTime.UseAlternateDateFormat to TRUE uses a DateTime format that should allow reading the header.
		header.StartTime.UseAlternateDateFormat = true;
		
        // Rewind the file stream and try parsing the header file again 
        file.Position = 0;
        header.ReadFrom( reader );

		Assert.AreEqual( 11, header.NumberOfSignals );
	}

	[TestMethod]
	public void TestSignalLerp()
	{
		// Demonstrates the method used to encode Signal values, and the relationship between
		// PhysicalMinimum, PhysicalMaximum and DigitalMinimum, DigitalMaximum
		
		var physMax = (double)short.MaxValue;
		var physMin = (double)short.MinValue;
		var digiMax = 255;
		var digiMin = 0;

		for( int i = 0; i < 10; i++ )
		{
			var value = Random.Shared.Next( digiMin, digiMax );

			var invT              = inverseLerp( digiMin, digiMax, value );
			var interpolatedValue = lerp( physMin, physMax, invT );

			Debug.WriteLine( $"{value} -> {interpolatedValue}" );

		}

		physMax = 0;
		physMin = 1024;
		
		for( int i = 0; i < 10; i++ )
		{
			var value = Random.Shared.Next( digiMin, digiMax );

			var invT              = inverseLerp( digiMin, digiMax, value );
			var interpolatedValue = lerp( physMin, physMax, invT );

			Debug.WriteLine( $"{value} -> {interpolatedValue}" );

		}

		float inverseLerp( double a, double b, double value )
		{
			return (float)((value - a) / (b - a));
		}

		double lerp( double a, double b, float t )
		{
			return (1.0 - t) * a + b * t;
		}
	}
	
	#region Utility functions

	private bool listsAreEqual<T>( List<T> source, List<T> compare ) where T : EdfAsciiField
	{
		Assert.AreEqual( source.Count, compare.Count );

		for( int i = 0; i < source.Count; i++ )
		{
			Assert.AreEqual( source[ i ].ToString(), compare[ i ].ToString() );
		}

		return true;
	}

	private static void writeHeaderToFile( EdfFileHeader header, string filename )
	{
		using var outputFile   = File.Create( filename );
		using var outputWriter = new BinaryWriter( outputFile );
		
		header.WriteTo( outputWriter );
	}

	private static EdfFileHeader readHeaderFromFile( string filename )
	{
		using var sourceFile = File.OpenRead( filename );

		return new EdfFileHeader( sourceFile );
	}
	
	#endregion 
}
