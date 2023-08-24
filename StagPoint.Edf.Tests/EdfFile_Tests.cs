using System.Diagnostics;

using StagPoint.EDF.Net;

namespace StagPoint.Edf.Tests;

[TestClass]
public class EdfFile_Tests
{
	[TestMethod]
	public void ReadEdfFileWithSignals()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "Female57yrs 07-MAR-2009 00h00m00s APSG.edf" );
		//string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "annotations.edf" );
		//string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "signals_only2.edf" );
		if( !File.Exists( filename ) )
		{
			Assert.Fail( "Test file missing" );
		}

		var file = new EdfFile();
		file.ReadFrom( filename );

		Assert.AreEqual( StandardTexts.FileType.EDF_Plus_Continuous, file.Header.Reserved );
		Assert.AreEqual( 90,                                         file.Header.NumberOfDataRecords );
		Assert.AreEqual( 20,                                         file.Header.NumberOfSignals );
		Assert.AreEqual( 10,                                         (int)file.Header.DurationOfDataRecord );
		Assert.AreEqual( file.Header.NumberOfSignals,                file.Signals.Count );
		Assert.IsNotNull( file.Signals.First( x => x.Label.Value == StandardTexts.SignalType.OxygenSaturation ) );
		Assert.IsNotNull( file.Signals.First( x => x.Label.Value == StandardTexts.SignalType.EdfAnnotations ) );
	}
	
	[TestMethod]
	public void ReadAndWriteEdfFileWithAnnotations()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "annotations_and_signals2.edf" );
		if( !File.Exists( filename ) )
		{
			Assert.Fail( "Test file missing" );
		}

		var file = new EdfFile();
		file.ReadFrom( filename );

		var tempFilename = Path.ChangeExtension( Path.GetTempFileName(), ".edf" );

		try
		{
			file.WriteTo( tempFilename );
			
			var compareFile = new EdfFile();
			compareFile.ReadFrom( tempFilename );
			
			Assert.AreEqual( file.Header.Reserved.Value,             compareFile.Header.Reserved );
			Assert.AreEqual( file.Header.NumberOfDataRecords.Value,  compareFile.Header.NumberOfDataRecords );
			Assert.AreEqual( file.Header.NumberOfSignals.Value,      compareFile.Header.NumberOfSignals );
			Assert.AreEqual( file.Header.DurationOfDataRecord.Value, (int)compareFile.Header.DurationOfDataRecord );
			Assert.AreEqual( file.Header.NumberOfSignals.Value,      compareFile.Signals.Count );
			
			Assert.IsNotNull( compareFile.Signals.First( x => x.Label.Value == StandardTexts.SignalType.OxygenSaturation ) );
			Assert.IsNotNull( compareFile.Signals.First( x => x.Label.Value == StandardTexts.SignalType.EdfAnnotations ) );
		}
		finally
		{
			File.Delete( tempFilename );
		}
	}
}
