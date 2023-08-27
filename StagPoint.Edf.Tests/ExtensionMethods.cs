using StagPoint.EDF.Net;

namespace StagPoint.Edf.Tests;

public class Extensions
{
	public static void AssertSignalsSame( EdfStandardSignal expected, EdfStandardSignal actual, double delta = 0.001, string message = null )
	{
		Assert.AreEqual( expected.Label.Value,           actual.Label );
		Assert.AreEqual( expected.PhysicalMaximum.Value, actual.PhysicalMaximum );
		Assert.AreEqual( expected.PhysicalMinimum.Value, actual.PhysicalMinimum );
		Assert.AreEqual( expected.DigitalMaximum.Value,  actual.DigitalMaximum );
		Assert.AreEqual( expected.DigitalMinimum.Value,  actual.DigitalMinimum );
		Assert.AreEqual( expected.Samples.Count,         actual.Samples.Count );

		for( int i = 0; i < expected.Samples.Count; i++ )
		{
			Assert.AreEqual( expected.Samples[ i ], actual.Samples[ i ], delta, message );
		}
	}
}
