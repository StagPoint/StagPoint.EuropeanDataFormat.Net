using System.Diagnostics;

using StagPoint.EDF.Net;

namespace StagPoint.Edf.Tests;

[TestClass]
public class EdfFile_Tests
{
	[TestMethod]
	public void ReadEdfFileWithSignals()
	{
		//string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "Female57yrs 07-MAR-2009 00h00m00s APSG.edf" );
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "annotations.edf" );
		if( !File.Exists( filename ) )
		{
			Assert.Fail( "Test file missing" );
		}

		var file = new EdfFile();
		file.ReadFrom( filename );
		
		// Assert.AreEqual( 90, file.Header.NumberOfDataRecords );
		// Assert.AreEqual( 20, file.Signals.Count );
		//
		// Assert.IsTrue( file.Signals.Any( x => x.Label.Value.Equals( StandardTexts.SignalType.Electrocardiogram ) ) );
		// Assert.IsTrue( file.Signals.Any( x => x.Label.Value.Equals( StandardTexts.SignalType.OxygenSaturation ) ) );
		// Assert.IsTrue( file.Signals.Any( x => x.Label.Value.Equals( StandardTexts.SignalType.EdfAnnotations ) ) );
		//
		// foreach( var signal in file.Signals )
		// {
		// 	if( signal is EdfStandardSignal )
		// 	{
		// 		Assert.AreEqual( signal.NumberOfSamplesPerRecord * file.Header.NumberOfDataRecords, ((EdfStandardSignal)signal).Samples.Count );
		// 	}
		// }
	}
}
