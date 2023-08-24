using System.Collections.Generic;

namespace StagPoint.EDF.Net
{
	public class EdfStandardSignal : EdfSignalBase
	{
		#region Public fields

		public List<double> Samples { get; set; } = new List<double>();
		
		public double FrequencyInHz { get; internal set; }

		#endregion
		
		#region Constructor 
		
		internal EdfStandardSignal( EdfSignalHeader header ) : base( header )
		{
			// Ensure that there is enough space in the list to store all of the expected samples
			Samples.Capacity = header.NumberOfDataRecords.Value * header.NumberOfSamplesPerRecord.Value;
		}
		
		#endregion
	}
}
