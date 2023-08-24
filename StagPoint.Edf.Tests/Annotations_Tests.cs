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

		var signal  = file.Signals[ 0 ] as EdfAnnotationSignal;
		Assert.IsNotNull( signal, "No Annotations Signal found in file" );
		
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
		
		var tempFilename = Path.ChangeExtension( Path.GetTempFileName(), ".edf" );

		try
		{
			file.WriteTo( tempFilename );
			
			var compareFile = EdfFile.Open( tempFilename );
			
			var signal = compareFile.Signals[ 0 ] as EdfAnnotationSignal;
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
}

