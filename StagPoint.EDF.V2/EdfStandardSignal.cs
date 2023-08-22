using System.Collections.Generic;

namespace StagPoint.EDF.Net
{
	public class EdfStandardSignal : EdfSignalBase
	{
		#region Public fields

		public List<double> Samples { get; set; } = new List<double>();

		#endregion
		
		#region Constructor 
		
		public EdfStandardSignal( EdfSignalHeader header ) : base( header )
		{
		}
		
		#endregion
	}
}
