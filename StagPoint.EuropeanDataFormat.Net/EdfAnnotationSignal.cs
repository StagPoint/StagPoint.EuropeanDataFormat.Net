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

	/// <summary>
	/// Can be used to store text annotations, time, events, stimuli, etc. 
	/// Annotations may only contain UCS characters (ISO 10646, the 'Universal Character Set', which is
	/// identical to the Unicode version 3+ character set) encoded by UTF-8.
	/// </summary>
	public class EdfAnnotation
	{
		#region Public properties 
		
		/// <summary>
		/// Specifies the number of seconds by which the onset of the annotated event follows ('+') or precedes ('-')
		/// the startdate/time of the file (the StartTime that is specified in the file header)
		/// </summary>
		public double Onset { get; set; } = default;

		/// <summary>
		/// Specifies the duration of the annotated event in seconds. If such a specification is not relevant,
		/// Duration can be skipped by setting the value to null.
		/// </summary>
		public double? Duration { get; set; } = null;

		/// <summary>
		/// Gets or sets the text of an annotation, when there is only one annotation in a Timestamped Annotation List.
		/// This is a convenience function for accessing the first element of the <see cref="AnnotationList"/> list;
		/// </summary>
		public string Annotation
		{
			get
			{
				return AnnotationList.Count > 0 ? AnnotationList[ 0 ] : string.Empty;
			}
			set
			{
				if( AnnotationList.Count == 0 )
				{
					AnnotationList.Add( value );
				}
				else
				{
					AnnotationList[ 0 ] = value;
				}
			}
		}

		/// <summary>
		/// Holds the list of all annotations contained in a
		/// <a href="https://www.edfplus.info/specs/edfplus.html#tal">Timestamped Annotation List</a>.
		/// </summary>
		public List<string> AnnotationList { get; private set; } = new List<string>();
		
		/// <summary>
		/// TimeKeeping Annotations are automatically stored in the file for purposes of
		/// indicating when each DataRecord begins relative to the start of the file.
		/// They may optionally be added to the EdfAnnotationSignal when loading an EDF+ file,
		/// but will never be saved from an EdfAnnotationSignal when writing to a file.
		/// </summary>
		public bool IsTimeKeepingAnnotation { get; internal set; }
		
		#endregion 
		
		#region Public functions

		/// <summary>
		/// Returns a deep clone of this instance
		/// </summary>
		public EdfAnnotation Clone()
		{
			var clone = new EdfAnnotation()
			{
				Onset                   = Onset,
				Duration                = Duration,
				IsTimeKeepingAnnotation = IsTimeKeepingAnnotation,
			};

			clone.AnnotationList.AddRange( this.AnnotationList );

			return clone;
		}

		/// <summary>
		/// Returns the number of bytes that would be needed to store this Annotation
		/// </summary>
		internal int GetSize()
		{
			int size = 0;

			// Onset must be preceded by a '-' or '+' character.
			if( this.Onset >= 0 )
			{
				size += 1;
			}

			size += Onset.ToString( CultureInfo.InvariantCulture ).Length;

			if( IsTimeKeepingAnnotation )
			{
				// 0x14 0x14 0x00 delimiters
				return size + 3; 
			}

			if( this.Duration.HasValue )
			{
				size += 1; // 0x15 delimiter
				size += Duration.Value.ToString( CultureInfo.InvariantCulture ).Length;
			}

			// 0x14 Annotation text delimiter
			size += 1;

			foreach( var description in AnnotationList )
			{
				size += Encoding.UTF8.GetByteCount( description );
				size += 1; // 0x14 delimiter
			}

			// 0x00 End of annotation delimiter
			size += 1;

			return size;
		}
		
		#endregion 

		#region Base class overrides

		/// <summary>
		/// Returns a string representation of this Annotation
		/// </summary>
		public override string ToString()
		{
			if( IsTimeKeepingAnnotation )
			{
				return $"Start Time: {Onset} (Timekeeping Annotation)";
			}
			
			if( Duration.HasValue )
			{
				return $"Onset: {Onset}, Duration: {Duration}, Annotation: {Annotation}";
			}
			
			return $"Onset: {Onset}, Annotation: {Annotation ?? string.Empty}";
		}

		#endregion
	}
}
