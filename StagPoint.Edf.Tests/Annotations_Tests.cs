using System.Diagnostics;

using StagPoint.EDF.Net;

namespace StagPoint.Edf.Tests;

[TestClass]
public class Annotations_Tests
{
	[TestMethod]
	public void ReadTimestampedAnnotationsLists()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "annotations.edf" );
		if( !File.Exists( filename ) )
		{
			Assert.Fail( "Test file missing" );
		}

		var file = EdfFile.Open( filename );
		
		Assert.IsTrue( file.AnnotationSignals.Count > 0 );

		var signal  = file.AnnotationSignals[ 0 ];
		
		var listTAL = signal.Annotations.Where( x => x.AnnotationList.Count > 1 ).ToList();
		Assert.IsTrue( listTAL is { Count: > 0 }, "Did not find any Annotation Signals with TAL annotations in file" );

		var TAL = listTAL.FirstOrDefault( x => x.AnnotationList.Count > 1 );
		Assert.IsNotNull( TAL, "Did not find any TAL annotations in file" );
		
		Assert.AreEqual( 5, TAL.AnnotationList.Count );
	}

	[TestMethod]
	public void RoundTripAllAnnotationsS()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "annotations.edf" );
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

			Assert.AreEqual( file.AnnotationSignals.Count,                  compareFile.AnnotationSignals.Count );

			var lhsList = file.AnnotationSignals[ 0 ].Annotations;
			var rhsList = compareFile.AnnotationSignals[ 0 ].Annotations;

			lhsList.RemoveAll( x => x.IsTimeKeepingAnnotation );
			rhsList.RemoveAll( x => x.IsTimeKeepingAnnotation );
			
			Assert.AreEqual( lhsList.Count, rhsList.Count );

			for( int i = 0; i < lhsList.Count; i++ )
			{
				var lhs = lhsList[ i ];
				var rhs = rhsList[ i ];

				Assert.AreEqual( lhs.ToString(), rhs.ToString() );
			}
		}
		finally
		{
			File.Delete( tempFilename );
		}
	}

	[TestMethod]
	public void RoundTripTimestampedAnnotationsLists()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "annotations.edf" );
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
			
			Assert.IsTrue( compareFile.AnnotationSignals.Count > 0 );
			
			var signal = compareFile.AnnotationSignals[ 0 ];
			Assert.IsNotNull( signal, "No Annotations Signal found in file" );
		
			var listTAL = signal.Annotations.Where( x => x.AnnotationList.Count > 1 ).ToList();
			Assert.IsTrue( listTAL is { Count: > 0 }, "Did not find any Annotation Signals with TAL annotations in file" );

			var TAL = listTAL.FirstOrDefault( x => x.AnnotationList.Count > 1 );
			Assert.IsNotNull( TAL, "Did not find any TAL annotations in file" );
		
			Assert.AreEqual( 5, TAL.AnnotationList.Count );
		}
		finally
		{
			File.Delete( tempFilename );
		}
	}

	[TestMethod]
	public void AnnotationTooLargeExceptionExpected()
	{
		const string VERY_LARGE_TEXT = @"
				But I must explain to you how all this mistaken idea of denouncing pleasure and praising pain was 
				born and I will give you a complete account of the system, and expound the actual 
				teachings of the great explorer of the truth, the master-builder of human happiness. 
				No one rejects, dislikes, or avoids pleasure itself, because it is pleasure, but because 
				those who do not know how to pursue pleasure rationally encounter consequences that are 
				extremely painful. Nor again is there anyone who loves or pursues or desires to obtain 
				pain of itself, because it is pain, but because occasionally circumstances occur in which 
				toil and pain can procure him some great pleasure. To take a trivial example, which of us 
				ever undertakes laborious physical exercise, except to obtain some advantage from it? But 
				who has any right to find fault with a man who chooses to enjoy a pleasure that has no 
				annoying consequences, or one who avoids a pain that produces no resultant pleasure?";
		
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "annotations_and_signals.edf" );
		if( !File.Exists( filename ) )
		{
			Assert.Fail( "Test file missing" );
		}

		// Load a file with an existing Annotations Signal, which only allocates a small amount of memory for annotations
		var file = EdfFile.Open( filename );
		
		var signal      = file.AnnotationSignals[ 0 ];
		var annotations = signal.Annotations;
		
		// Remove all timekeeping annotations
		annotations.RemoveAll( x => x.IsTimeKeepingAnnotation );
		
		// Add a very large annotation to the signal
		annotations.Add( new EdfAnnotation()
		{
			Onset = annotations.Last().Onset + 1,
			Annotation = VERY_LARGE_TEXT, 
		});

		var tempFilename = GetTempFilename();

		try
		{
			// Attempt to save the file with the new very large annotation
			file.WriteTo( tempFilename );

			// If no exception was thrown, this test has failed
			Assert.Fail( "Expected an exception to be thrown." );
		}
		catch( Exception e )
		{
			// We're expecting the exception message to explain that the annotation is too large  
			Assert.IsTrue( e.Message.Contains( "Annotation too large", StringComparison.OrdinalIgnoreCase ) );
		}
		finally
		{
			File.Delete( tempFilename );
		}
	}

	[TestMethod]
	public void TooManyAnnotations()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Test Files", "annotations_and_signals.edf" );
		if( !File.Exists( filename ) )
		{
			Assert.Fail( "Test file missing" );
		}

		// Load a file with an existing Annotations Signal, which we will fill with way too many annotations to 
		// store in the 600 Data Records allocated for standards Signals. 
		var file = EdfFile.Open( filename );
		var signal = file.AnnotationSignals[ 0 ];
		
		// Remove all existing annotations
		signal.Annotations.Clear();

		for( int i = 0; i < file.Header.NumberOfDataRecords * 5; i++ )
		{
			signal.Annotations.Add( new EdfAnnotation()
			{
				Onset      = i + 1,
				Annotation = "Now is the time for all good men to come to the aid of their country."
			} );
		}
		
		var tempFilename = GetTempFilename();

		try
		{
			// Attempt to save the file with the too many annotations
			file.WriteTo( tempFilename );

			// If no exception was thrown, this test has failed
			Assert.Fail( "Expected an exception to be thrown." );
		}
		catch( Exception e )
		{
			// We're expecting the exception message to explain that there was not enough space allocated to annotations  
			Assert.IsTrue( e.Message.Contains( "Not enough space", StringComparison.OrdinalIgnoreCase ) );
		}
		finally
		{
			//File.Delete( tempFilename );
		}
	}
	
	private static string GetTempFilename()
	{
		return Path.Combine( Path.GetTempPath(), $"{Guid.NewGuid()}.edf" );
	}
}

