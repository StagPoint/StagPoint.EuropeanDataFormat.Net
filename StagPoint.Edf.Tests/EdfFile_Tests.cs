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
		//string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "annotations.edf" );
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "signals_only2.edf" );
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
