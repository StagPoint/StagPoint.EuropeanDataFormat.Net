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
			var loop = file.Signals[ i ];
			
			if( loop is EdfStandardSignal signal )
			{
				Assert.AreEqual( correctFrequencies[ i ], signal.FrequencyInHz );
			}
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

		var signal     = file.Signals[ 0 ] as EdfStandardSignal;
		Assert.IsNotNull( signal, "Failed to find a signal to verify" );
		
		var fragment   = signal.GetFragment( file.Fragments[ 1 ] );
		var timestamps = signal.GetTimestamps( file.Fragments[ 1 ] );

		Assert.AreEqual( signal.NumberOfSamplesPerRecord, fragment.Count );
		Assert.AreEqual( 1.5,                             timestamps[0],     0.1 );
		Assert.AreEqual( 2.5,                             timestamps.Last(), 0.1 );
	}

	[TestMethod]
	public void AutoCalculateDataRecordSize()
	{
		var signalFrequencies = new double[]
		{
			128, 128, 128, 128, 128, 128, 128, 128, 128, 
			128, 128, 128, 128, 128, 128, 128, 32, 32, 4,
			0.25,
		};

		var lcm = LCM( signalFrequencies );

		foreach( var number in signalFrequencies )
		{
			Assert.AreEqual( 0, lcm % number, 0.1 );
		}
	}
	
	static long LCM( params double[] numbers)
	{
		return (long)numbers.Aggregate( lcm );
		
		double lcm(double a, double b)
		{
			var longA = (long)Math.Floor( a * 100 );
			var longB = (long)Math.Floor( b * 100 );
			
			var result = Math.Abs(longA * longB) / gcd(longA, longB);

			return Math.Floor( result / 100.0 );
		}

		long gcd( long a, long b )
		{
			while( true )
			{
				if( a < b )
				{
					(a, b) = (b, a);
					continue;
				}

				if( b == 0 )
				{
					return a;
				}

				var a1 = a;
				a = b;
				b = a1 - (long)Math.Floor( (double)a1 / b ) * b;
			}
		}
	}
	
	private static string GetTempFilename()
	{
		return Path.Combine( Path.GetTempPath(), $"{Guid.NewGuid()}.edf" );
	}
}
