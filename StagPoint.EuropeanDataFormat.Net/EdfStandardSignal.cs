// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

using System;
using System.Collections.Generic;

namespace StagPoint.EDF.Net
{
	/// <summary>
	/// A standard EDF Signal, containing a list of samples provided by the signal generator. 
	/// </summary>
	public class EdfStandardSignal : EdfSignalBase
	{
		#region Public fields

		/// <summary>
		/// The list of sample values to be stored for this Signal
		/// </summary>
		public List<double> Samples { get; set; } = new List<double>();
		
		/// <summary>
		/// Returns the number of samples per second contained in this Signal.
		/// </summary>
		public double FrequencyInHz { get; internal set; }
		
		/// <summary>
		/// Calculates and returns the signal sensitivity (Units/bit) as defined by the four parameters
		/// PhysicalMaximum, PhysicalMinimum, DigitalMaximum, and DigitalMinimum.
		/// </summary>
		public double Sensitivity { get => (PhysicalMaximum - PhysicalMinimum) / ((double)DigitalMaximum - DigitalMinimum); }
		
		/// <summary>
		/// Calculates and returns the signal offset as defined by the four parameters
		/// PhysicalMaximum, PhysicalMinimum, DigitalMaximum, and DigitalMinimum.
		/// </summary>
		public double Offset { get => (PhysicalMaximum / Sensitivity) - DigitalMaximum; }

		#endregion
		
		#region Constructor 
		
		/// <summary>
		/// Initializes a new instance of the EdfStandardSignal class 
		/// </summary>
		internal EdfStandardSignal( EdfSignalHeader header ) : base( header )
		{
			// Ensure that there is enough space in the list to store all of the expected samples
			Samples.Capacity = header.NumberOfDataRecords.Value * header.NumberOfSamplesPerRecord.Value;
		}
		
		#endregion
		
		#region Public functions

		/// <summary>
		/// Returns a list of all sample values contained in the provided <see cref="EdfDataFragment"/>
		/// </summary>
		public List<double> GetFragment( EdfDataFragment fragment )
		{
			var startIndex = fragment.StartRecordIndex * NumberOfSamplesPerRecord;
			var endIndex   = Math.Min( Samples.Count, startIndex + NumberOfSamplesPerRecord );

			return Samples.GetRange( startIndex, endIndex - startIndex );
		}

		/// <summary>
		/// Returns a list of time values (offsets from the start of the file) specified in seconds,
		/// one for each signal value in the indicated Data Fragment. This is useful when a signal has
		/// been stored discontinuously (in an EDF+D file) and you need to graph the fragments
		/// individually, for instance. 
		/// </summary>
		public List<double> GetTimestamps( EdfDataFragment fragment )
		{
			var interval   = fragment.DataRecordLength / NumberOfSamplesPerRecord;
			var startIndex = fragment.StartRecordIndex * NumberOfSamplesPerRecord;
			var endIndex   = (int)Math.Min( Samples.Count, startIndex + NumberOfSamplesPerRecord * fragment.Duration );

			var result = new List<double>( endIndex - startIndex );
			
			for( int i = 0; i < endIndex - startIndex; i++ )
			{
				result.Add( fragment.StartTime + interval * i );
			}

			return result;
		}

		#endregion
	}
}
