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
	}
	
	public void WriteEdfFileWithSignalsAndAnnotations()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "Female57yrs 07-MAR-2009 00h00m00s APSG.edf" );
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
		}
		finally
		{
			File.Delete( tempFilename );
		}
	}
}
