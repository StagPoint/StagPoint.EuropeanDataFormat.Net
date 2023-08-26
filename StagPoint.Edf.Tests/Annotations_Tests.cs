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
	
	private static string GetTempFilename()
	{
		return Path.Combine( Path.GetTempPath(), $"{Guid.NewGuid()}.edf" );
	}
}

