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

		using var file   = File.OpenRead( filename );
		using var reader = new BinaryReader( file );
		var       header = new EdfFileHeader();
		
		header.ReadFromBuffer( reader );

		Assert.AreEqual( "X F X Female57yrs",                                    header.PatientInfo );
		Assert.AreEqual( "Startdate 07-MAR-2009 X Tech:X Somnoscreen_Plus_1500", header.RecordingInfo );
		Assert.AreEqual( 768,                                                    header.HeaderRecordSize );
		Assert.AreEqual( "EDF+C",                                                header.Reserved );
		Assert.AreEqual( 1,                                                      header.NumberOfDataRecords );
		Assert.AreEqual( 900.0,                                                  header.DurationOfDataRecord );
		Assert.AreEqual( new DateTime( 2009, 03, 07 ),                           header.StartTime );
				
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

		using var file   = File.OpenRead( filename );
		using var reader = new BinaryReader( file );
		var       header = new EdfFileHeader();
		
		header.ReadFromBuffer( reader );

		Assert.AreEqual( "X F X Female57yrs",                                    header.PatientInfo );
		Assert.AreEqual( "Startdate 07-MAR-2009 X Tech:X Somnoscreen_Plus_1500", header.RecordingInfo );
		Assert.AreEqual( 5376,                                                   header.HeaderRecordSize );
		Assert.AreEqual( "EDF+C",                                                header.Reserved );
		Assert.AreEqual( 90,                                                     header.NumberOfDataRecords );
		Assert.AreEqual( 10.0,                                                   header.DurationOfDataRecord );
		Assert.AreEqual( new DateTime( 2009, 03, 07 ),                           header.StartTime );

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
			Assert.AreEqual( sourceHeader.PatientInfo.Value,          compareHeader.PatientInfo );
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

		var testFiles = Directory.GetFiles( currentFolder, "*.edf" );
		Assert.IsTrue( testFiles != null && testFiles.Length > 0, "No test files found" );

		foreach( var filename in testFiles )
		{
			try
			{
				var sourceHeader = readHeaderFromFile( filename );
			
				writeHeaderToFile( sourceHeader, tempFileName );

				var compareHeader = readHeaderFromFile( tempFileName );
			
				Assert.AreEqual( sourceHeader.Version.Value,              compareHeader.Version );
				Assert.AreEqual( sourceHeader.PatientInfo.Value,          compareHeader.PatientInfo );
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
	
	#region Utility functions

	private bool listsAreEqual<T>( List<T> source, List<T> compare ) where T : class, IEdfAsciiField
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
		using( var outputFile = File.Create( filename ) )
		{
			using( var outputWriter = new BinaryWriter( outputFile ) )
			{
				header.WriteToBuffer( outputWriter );
			}
		}
	}

	private static EdfFileHeader readHeaderFromFile( string filename )
	{
		using var sourceFile = File.OpenRead( filename );

		return new EdfFileHeader( sourceFile );
	}
	
	#endregion 
}
