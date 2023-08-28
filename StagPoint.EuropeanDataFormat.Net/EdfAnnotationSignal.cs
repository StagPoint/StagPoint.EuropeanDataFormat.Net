// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StagPoint.EDF.Net
{
	/// <summary>
	/// An EDF+ Signal that is specially coded to store text annotations, time, events and stimuli
	/// instead of numerical signal information. 
	/// <a href="https://www.edfplus.info/specs/edfplus.html#edfplusannotations">Annotations in the EDF+ Specification</a>
	/// </summary>
	public class EdfAnnotationSignal : EdfSignalBase
	{
		#region Public properties

		/// <summary>
		/// Contains the full list of Annotations stored in this Signal
		/// </summary>
		public List<EdfAnnotation> Annotations { get; private set; } = new List<EdfAnnotation>(); 
		
		#endregion
		
		#region Constructor

		/// <summary>
		/// Initializes a new instance of the EdfAnnotationSignal class
		/// </summary>
		public EdfAnnotationSignal()
		{
			// From the EDF+ Specification: https://www.edfplus.info/specs/edfplus.html#annotationssignal
			// The 'EDF Annotations' signal only has meaningful header fields 'label' and 'nr of samples in each data
			// record'. For the sake of EDF compatibility, the fields 'digital minimum' and 'digital maximum' must be
			// filled with -32768 and 32767, respectively. The 'Physical maximum' and 'Physical minimum' fields must
			// contain values that differ from each other. The other fields of this signal are filled with spaces.
			DigitalMinimum.Value  = short.MinValue;
			DigitalMaximum.Value  = short.MaxValue;
			PhysicalMinimum.Value = short.MinValue;
			PhysicalMaximum.Value = short.MaxValue;
			TransducerType.Value  = string.Empty;
			Prefiltering.Value    = string.Empty;
			Reserved.Value        = string.Empty;

			// All Annotation Signals must have a label of "EDF Annotations"
			Label.Value = StandardTexts.SignalType.EdfAnnotations;
            
			// The signal must allocate enough memory to store timekeeping annotations, at the very least. 
			NumberOfSamplesPerRecord.Value = 8;
		}
		
		/// <summary>
		/// Initializes a new instance of the EdfAnnotationSignal class
		/// </summary>
		/// <param name="header">An EdfSignalHeader instance containing all of the essential Signal information</param>
		internal EdfAnnotationSignal( EdfSignalHeader header ) : base( header )
		{
		}
		
		#endregion
		
		#region Public functions

		/// <summary>
		/// Copies all of this object's instance data to the other instance
		/// </summary>
		public void CopyTo( EdfAnnotationSignal other, bool includeTimekeepingAnnotations = false )
		{
			other.Label.Value                    = Label;
			other.TransducerType.Value           = TransducerType;
			other.PhysicalDimension.Value        = PhysicalDimension;
			other.PhysicalMinimum.Value          = PhysicalMinimum;
			other.PhysicalMaximum.Value          = PhysicalMaximum;
			other.DigitalMinimum.Value           = DigitalMinimum;
			other.DigitalMaximum.Value           = DigitalMaximum;
			other.Prefiltering.Value             = Prefiltering;
			other.NumberOfSamplesPerRecord.Value = NumberOfSamplesPerRecord;
			other.Reserved.Value                 = Reserved;

			AppendAnnotations( other.Annotations, includeTimekeepingAnnotations );
		}

		internal void AppendAnnotations( List<EdfAnnotation> list, bool includeTimekeepingAnnotations = false )
		{
			foreach( var annotation in Annotations )
			{
				if( !annotation.IsTimeKeepingAnnotation || includeTimekeepingAnnotations )
				{
					list.Add( annotation.Clone() );
				}
			}
		}
		
		#endregion
	}
}
