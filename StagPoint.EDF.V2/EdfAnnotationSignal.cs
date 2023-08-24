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
	}

	/// <summary>
	/// Can be used to store text annotations, time, events, stimuli, etc. 
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
		/// Duration can be skipped by setting the value to 0.
		/// </summary>
		public double? Duration { get; set; } = default;

		/// <summary>
		/// These annotations may only contain UCS characters (ISO 10646, the 'Universal Character Set', which is
		/// identical to the Unicode version 3+ character set) encoded by UTF-8.
		/// </summary>
		public string Annotation { get; set; } = string.Empty;
		
		/// <summary>
		/// TimeKeeping Annotations are automatically stored in the file for purposes of
		/// indicating when each DataRecord begins relative to the start of the file.
		/// </summary>
		public bool IsTimeKeepingAnnotation { get; internal set; }
		
		#endregion 
		
		#region Public functions

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
				return size + 3;
			}

			if( this.Duration.HasValue )
			{
				size += 1; // 0x15 delimiter
				size += Duration.Value.ToString( CultureInfo.InvariantCulture ).Length;
			}

			if( !string.IsNullOrEmpty( Annotation ) )
			{
				size += 1; // 0x14 delimiter
				size += Encoding.UTF8.GetByteCount( Annotation );
			}

			size += 2; // 0x14 and 0x00 end delimiters

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
