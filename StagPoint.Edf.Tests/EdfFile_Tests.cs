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

		var file = EdfFile.Open( filename );

		Assert.AreEqual( StandardTexts.FileType.EDF_Plus_Continuous, file.Header.Reserved );
		Assert.AreEqual( 90,                                         file.Header.NumberOfDataRecords );
		Assert.AreEqual( 20,                                         file.Header.NumberOfSignals );
		Assert.AreEqual( 10,                                         (int)file.Header.DurationOfDataRecord );
		Assert.AreEqual( file.Header.NumberOfSignals,                file.Signals.Count + file.AnnotationSignals.Count );
		Assert.IsNotNull( file.Signals.First( x => x.Label.Value == StandardTexts.SignalType.OxygenSaturation ) );
		Assert.IsNotNull( file.AnnotationSignals.First( x => x.Label.Value == StandardTexts.SignalType.EdfAnnotations ) );
	}
	
	[TestMethod]
	public void ReadAndWriteEdfFileWithAnnotations()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "annotations_and_signals2.edf" );
		if( !File.Exists( filename ) )
		{
			Assert.Fail( "Test file missing" );
		}

		var file = EdfFile.Open( filename );

		var tempFilename = GetTempFilename();

		try
		{
			file.WriteTo( tempFilename );

			var compareFile = EdfFile.Open( tempFilename );
			
			Assert.AreEqual( file.Header.Reserved.Value,             compareFile.Header.Reserved );
			Assert.AreEqual( file.Header.NumberOfDataRecords.Value,  compareFile.Header.NumberOfDataRecords );
			Assert.AreEqual( file.Header.NumberOfSignals.Value,      compareFile.Header.NumberOfSignals );
			Assert.AreEqual( file.Header.DurationOfDataRecord.Value, (int)compareFile.Header.DurationOfDataRecord );
			Assert.AreEqual( file.Header.NumberOfSignals.Value,      compareFile.Signals.Count + compareFile.AnnotationSignals.Count );
			
			Assert.IsNotNull( compareFile.Signals.First( x => x.Label.Value == StandardTexts.SignalType.OxygenSaturation ) );
			Assert.IsNotNull( compareFile.AnnotationSignals.First( x => x.Label.Value == StandardTexts.SignalType.EdfAnnotations ) );
		}
		finally
		{
			File.Delete( tempFilename );
		}
	}
	
	[TestMethod]
	public void VerifyCorrectSignalFrequency()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "signals_only2.edf" );
		if( !File.Exists( filename ) )
		{
			Assert.Fail( "Test file missing" );
		}

		var file = EdfFile.Open( filename );

		// Frequencies obtained by reading the file with Polyman
		var correctFrequencies = new double[]
		{
			128, 128, 128, 128, 128, 128, 128, 128, 128, 
			128, 128, 128, 128, 128, 128, 128, 32, 32, 4
		};

		for( int i = 0; i < file.Signals.Count; i++ )
		{
			var signal = file.Signals[ i ];
			
			Assert.AreEqual( correctFrequencies[ i ], signal.FrequencyInHz );
		}
	}

	[TestMethod]
	public void ReadDiscontinuousFile()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "Discontinuous1.edf" );
		if( !File.Exists( filename ) )
		{
			Assert.Fail( "Test file missing" );
		}

		var file = EdfFile.Open( filename );

		Assert.AreEqual( EdfFileType.EDF_Plus_Discontinuous, file.FileType );
		Assert.AreEqual( file.Header.NumberOfDataRecords,    file.Fragments.Count );
		Assert.AreEqual( 1.0,                                file.Fragments[ 0 ].Duration );
		Assert.AreEqual( 1.5,                                file.Fragments[ 1 ].Onset - file.Fragments[ 0 ].Onset );

		var signal = file.Signals[ 0 ];
		Assert.IsNotNull( signal, "Failed to find a signal to verify" );
		
		var fragment   = signal.GetFragment( file.Fragments[ 1 ] );
		var timestamps = signal.GetTimestamps( file.Fragments[ 1 ] );

		Assert.AreEqual( signal.NumberOfSamplesPerRecord, fragment.Count );
		Assert.AreEqual( 1.5,                             timestamps[0],     0.1 );
		Assert.AreEqual( 2.5,                             timestamps.Last(), 0.1 );
	}

	[TestMethod]
	public void WriteDiscontinuousFile()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "Discontinuous1.edf" );
		if( !File.Exists( filename ) )
		{
			Assert.Fail( "Test file missing" );
		}
		
		var tempFilename = GetTempFilename();

		try
		{
			var file = EdfFile.Open( filename );
			file.WriteTo( tempFilename );

			var compare = EdfFile.Open( tempFilename );

			Assert.AreEqual( EdfFileType.EDF_Plus_Discontinuous, compare.FileType );
			Assert.AreEqual( file.Header.NumberOfDataRecords,    compare.Fragments.Count );
			Assert.AreEqual( file.Fragments.Count,               compare.Fragments.Count );

			for( int i = 0; i < file.Fragments.Count; i++ )
			{
				var lhs = file.Fragments[ i ];
				var rhs = compare.Fragments[ i ];

				Assert.AreEqual( lhs.Onset,    rhs.Onset );
				Assert.AreEqual( lhs.Duration, rhs.Duration );
			}

			var signal = compare.Signals[ 0 ];
			Assert.IsNotNull( signal, "Failed to find a signal to verify" );

			var fragment   = signal.GetFragment( file.Fragments[ 1 ] );
			var timestamps = signal.GetTimestamps( file.Fragments[ 1 ] );

			Assert.AreEqual( signal.NumberOfSamplesPerRecord, fragment.Count );
			Assert.AreEqual( 1.5,                             timestamps[ 0 ],   0.1 );
			Assert.AreEqual( 2.5,                             timestamps.Last(), 0.1 );
		}
		finally
		{
			File.Delete( tempFilename );
		}
	}

	private static string GetTempFilename()
	{
		return Path.Combine( Path.GetTempPath(), $"{Guid.NewGuid()}.edf" );
	}
}
