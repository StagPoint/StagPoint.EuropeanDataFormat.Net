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

		using( var file = File.OpenRead( filename ) )
		{
			using( var reader = new BinaryReader( file ) )
			{
				var header = new EdfFileHeader();
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
		}
	}
	
	[TestMethod]
	public void ReadKnownPSGFileHeader()
	{
		string filename = Path.Combine(Environment.CurrentDirectory, "Test Files", "Female57yrs 07-MAR-2009 00h00m00s APSG.edf");
		if (!File.Exists(filename))
		{
			Assert.Fail( "Test file missing" );
		}

		using( var file = File.OpenRead( filename ) )
		{
			using( var reader = new BinaryReader( file ) )
			{
				var header = new EdfFileHeader();
				header.ReadFromBuffer( reader );

				Assert.AreEqual( "X F X Female57yrs",                                    header.PatientInfo );
				Assert.AreEqual( "Startdate 07-MAR-2009 X Tech:X Somnoscreen_Plus_1500", header.RecordingInfo );
				Assert.AreEqual( 5376,                                                   header.HeaderRecordSize );
				Assert.AreEqual( "EDF+C",                                                header.Reserved );
				Assert.AreEqual( 90,                                                     header.NumberOfDataRecords );
				Assert.AreEqual( 10.0,                                                   header.DurationOfDataRecord );
				Assert.AreEqual( new DateTime( 2009, 03, 07 ),                           header.StartTime );

				Assert.AreEqual( 20,              header.NumberOfSignals );
				Assert.AreEqual( 20,              header.TransducerType.Count );

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
		}
	}
}
