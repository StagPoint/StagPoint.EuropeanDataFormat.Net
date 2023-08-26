// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

using System;
using System.Collections.Generic;

namespace StagPoint.EDF.Net
{
	public class EdfStandardSignal : EdfSignalBase
	{
		#region Public fields

		public List<double> Samples { get; set; } = new List<double>();
		
		/// <summary>
		/// Returns the number of samples per second contained in this Signal.
		/// </summary>
		public double FrequencyInHz { get; internal set; }
		
		/// <summary>
		/// Calculates and returns the signal gain (in Units/bit) as defined by the four parameters
		/// PhysicalMaximum, PhysicalMinimum, DigitalMaximum, and DigitalMinimum.
		/// </summary>
		public double Gain { get => (PhysicalMaximum - PhysicalMinimum) / ((double)DigitalMaximum - DigitalMinimum); }
		
		/// <summary>
		/// Calculates and returns the signal offset as defined by the four parameters
		/// PhysicalMaximum, PhysicalMinimum, DigitalMaximum, and DigitalMinimum.
		/// </summary>
		public double Offset { get => (PhysicalMaximum / Gain) - DigitalMaximum; }

		#endregion
		
		#region Constructor 
		
		internal EdfStandardSignal( EdfSignalHeader header ) : base( header )
		{
			// Ensure that there is enough space in the list to store all of the expected samples
			Samples.Capacity = header.NumberOfDataRecords.Value * header.NumberOfSamplesPerRecord.Value;
		}
		
		#endregion
		
		#region Public functions

		/// <summary>
		/// Returns a list containing all samples for the given <see cref="EdfDataFragment"/>
		/// </summary>
		public List<double> GetFragment( EdfDataFragment fragment )
		{
			var startIndex = fragment.DataRecordIndex * NumberOfSamplesPerRecord;
			var endIndex   = Math.Min( Samples.Count, startIndex + NumberOfSamplesPerRecord );

			return Samples.GetRange( startIndex, endIndex - startIndex );
		}

		public List<double> GetTimestamps( EdfDataFragment fragment )
		{
			var interval   = fragment.DataRecordDuration / NumberOfSamplesPerRecord;
			var startIndex = fragment.DataRecordIndex * NumberOfSamplesPerRecord;
			var endIndex   = (int)Math.Min( Samples.Count, startIndex + NumberOfSamplesPerRecord * fragment.Duration );

			var result = new List<double>( endIndex - startIndex );
			
			for( int i = 0; i < endIndex - startIndex; i++ )
			{
				result.Add( fragment.Onset + interval * i );
			}

			return result;
		}

		#endregion
	}
}
