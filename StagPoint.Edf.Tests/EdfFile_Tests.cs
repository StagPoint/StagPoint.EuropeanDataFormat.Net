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

			Extensions.AssertSignalsSame( file.Signals[ 0 ], compareFile.Signals[ 0 ], 0.001 );
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

		// FrequencyInHz is a value calculated from the <code>Signal.NumberOfSamplesPerRecord / Header.DurationOfDataRecord</code>
		// "Correct" frequencies obtained by reading the file with Polyman
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
		Assert.AreEqual( 1.5,                                file.Fragments[ 1 ].StartTime - file.Fragments[ 0 ].StartTime );

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
			
			// In this test, we're just checking to ensure that an existing Discontinuous file can be written
			// without altering it. S
			file.WriteTo( tempFilename );

			// Read the file back in so we can check it. 
			var compare = EdfFile.Open( tempFilename );

			Assert.AreEqual( EdfFileType.EDF_Plus_Discontinuous, compare.FileType );
			Assert.AreEqual( file.Header.NumberOfDataRecords,    compare.Fragments.Count );
			Assert.AreEqual( file.Fragments.Count,               compare.Fragments.Count );

			for( int i = 0; i < file.Fragments.Count; i++ )
			{
				var lhs = file.Fragments[ i ];
				var rhs = compare.Fragments[ i ];

				Assert.AreEqual( lhs.StartTime, rhs.StartTime );
				Assert.AreEqual( lhs.Duration,  rhs.Duration );
			}

			var signal = compare.Signals[ 0 ];
			Assert.IsNotNull( signal, "Failed to find a signal to verify" );

			var fragment   = signal.GetFragment( file.Fragments[ 1 ] );
			var timestamps = signal.GetTimestamps( file.Fragments[ 1 ] );

			Assert.AreEqual( signal.NumberOfSamplesPerRecord, fragment.Count );
			Assert.AreEqual( 1.5,                             timestamps[ 0 ],   0.1 );
			Assert.AreEqual( 2.5,                             timestamps.Last(), 0.1 );

			// Ensure that nothing changed behind the scenes with which Signal samples got saved.
			for( int i = 0; i < file.Signals.Count; i++ )
			{
				Extensions.AssertSignalsSame( file.Signals[ i ], compare.Signals[ i ], 0.05 );
			}
		}
		finally
		{
			File.Delete( tempFilename );
		}
	}

	[TestMethod]
	public void ManuallyAddFragments_FileIsStillContiguous()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "Female57yrs 07-MAR-2009 00h00m00s APSG.edf" );
		if( !File.Exists( filename ) )
		{
			Assert.Fail( "Test file missing" );
		}

		var file = EdfFile.Open( filename );
		
		// Make sure that we're starting out with a contiguous file
		Assert.IsTrue( file.FileType != EdfFileType.EDF_Plus_Discontinuous );

		// Split the file into four still contiguous segments by marking the start of each segment
		for( int i = 0; i < 4; i++ )
		{
			file.MarkFragment(
				dataRecordIndex: i * 10,
				startTime:  i * 10 * file.Header.DurationOfDataRecord
			);
		}
		
		// Verify that the fragments are in fact contiguous (fragment duration is always updated automatically internally)
		for( int i = 0; i < 3; i++ )
		{
			var fragment = file.Fragments[ i ];
			Assert.AreEqual( fragment.StartTime + fragment.Duration, file.Fragments[ i + 1 ].StartTime );
		}

		var tempFilename = GetTempFilename();

		try
		{
			file.WriteTo( tempFilename );

			var compare = EdfFile.Open( tempFilename );
			
			// NOTE: Because the segments we added manually above are contiguous, EdfFile.WriteTo() did
			// not retain the specified fragments, and instead merged them back together.
			// This is the desired behavior, and is especially useful when appending files.
			Assert.AreEqual( 1, compare.Fragments.Count );
		}
		finally
		{
			File.Delete( tempFilename );
		}
	}

	[TestMethod]
	public void ManuallyAddFragments_FileIsNowDiscontiguous()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "Female57yrs 07-MAR-2009 00h00m00s APSG.edf" );
		if( !File.Exists( filename ) )
		{
			Assert.Fail( "Test file missing" );
		}

		var file = EdfFile.Open( filename );
		
		// Make sure that we're starting out with a contiguous file
		Assert.IsTrue( file.FileType != EdfFileType.EDF_Plus_Discontinuous );

		const double GAP_SIZE = 5.0;

		// Split the file into four still discontiguous segments by marking the start of each segment
		// with a five second gap between each
		for( int i = 0; i < 4; i++ )
		{
			file.MarkFragment(
				dataRecordIndex: i * 10,
				startTime:
				i * 10 * file.Header.DurationOfDataRecord + // Base start time 
				(i * GAP_SIZE)                              // Plus five seconds 
			);
		}
		
		// Verify that the fragments are in fact not contiguous (fragment duration is always updated automatically internally)
		for( int i = 0; i < 3; i++ )
		{
			var fragment = file.Fragments[ i ];
			Assert.AreEqual( fragment.StartTime + fragment.Duration + 5.0, file.Fragments[ i + 1 ].StartTime );
		}

		var tempFilename = GetTempFilename();

		try
		{
			// IMPORTANT: File must be marked as EDF+D in order to save discontinuous files
			file.FileType = EdfFileType.EDF_Plus_Discontinuous;

			// Write the file and read it back, for comparison
			file.WriteTo( tempFilename );
			var compare = EdfFile.Open( tempFilename );
			
			// Make sure that the marked fragments are included as such in the stored file. 
			Assert.AreEqual( 4, compare.Fragments.Count );
			
			// Ensure tha expected gap between fragments exists 
			for( int i = 1; i < compare.Fragments.Count; i++ )
			{
				var expectedStartTime = compare.Fragments[ i - 1 ].StartTime + compare.Fragments[ i - 1 ].Duration;
				var recordedStartTime = compare.Fragments[ i ].StartTime;
				
				Assert.AreEqual( expectedStartTime + GAP_SIZE, recordedStartTime, 0.001 );
			}
		}
		finally
		{
			File.Delete( tempFilename );
		}
	}

	[TestMethod]
	public void AppendFilesWithGaps()
	{
		string filename1 = Path.Combine( Environment.CurrentDirectory, "Test Files", "PulseOximetry1.edf" );
		string filename2 = Path.Combine( Environment.CurrentDirectory, "Test Files", "PulseOximetry2.edf" );
		string filename3 = Path.Combine( Environment.CurrentDirectory, "Test Files", "PulseOximetry3.edf" );
		
		if( !File.Exists( filename1 ) || !File.Exists( filename2 ) || !File.Exists( filename3 ) )
		{
			Assert.Fail( "Test file missing" );
		}

		var file1 = EdfFile.Open( filename1 );
		var file2 = EdfFile.Open( filename2 );
		var file3 = EdfFile.Open( filename3 );

		var mergedFile = file1.Clone();
		mergedFile.Append( file2 );
		mergedFile.Append( file3 );
		
		var tempFilename = GetTempFilename();

		try
		{
			// Write the file and read it back, for comparison
			mergedFile.WriteTo( tempFilename );
			var compare = EdfFile.Open( tempFilename );

			// Should have been assigned EDF+D file type automatically when calling Append with any file that results in a gap
			Assert.AreEqual( EdfFileType.EDF_Plus_Discontinuous, compare.Header.FileType );
			
			// Make sure that all auto-generated Fragments were properly written and read back
			Assert.AreEqual( mergedFile.Fragments.Count, compare.Fragments.Count );
			
			// Ensure that nothing changed behind the scenes with which Signal samples got saved, and all samples from
			// all three files got correctly saved. 
			for( int i = 0; i < mergedFile.Signals.Count; i++ )
			{
				Extensions.AssertSignalsSame( mergedFile.Signals[ i ], compare.Signals[ i ], 0.001 );
			}

			// For each Fragment, compare the actual samples from each Signal from the saved file 
			for( int fragmentIndex = 0; fragmentIndex < mergedFile.Fragments.Count; fragmentIndex++ )
			{
				var fragment = mergedFile.Fragments[ fragmentIndex ];

				for( int signalIndex = 0; signalIndex < mergedFile.Signals.Count; signalIndex++ )
				{
					var original = mergedFile.Signals[ signalIndex ].GetFragment( fragment );
					var test     = compare.Signals[ signalIndex ].GetFragment( fragment );

					CollectionAssert.AreEqual( original, test );
				}
			}
			
			// Just for good measure, make sure that extracted Fragments match the original non-appended file as well
			var middleFragment = mergedFile.Fragments[ 1 ];
			for( int signalIndex = 0; signalIndex < file2.Signals.Count; signalIndex++ )
			{
				var original = file2.Signals[ signalIndex ].Samples;
				var test     = compare.Signals[ signalIndex ].GetFragment( middleFragment );
				
				CollectionAssert.AreEqual( original, test );
			}
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
