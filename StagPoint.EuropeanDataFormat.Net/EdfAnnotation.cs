using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StagPoint.EDF.Net
{
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
		/// If this Annotation is linked to a specific channel, this property will contain that channel's Label value. 
		/// See <a href="https://www.edfplus.info/specs/edftexts.html#linkannotations">Linking annotations to signal channels</a>
		/// </summary>
		public string LinkedChannel { get; set; }

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
		
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the EdfAnnotation class
		/// </summary>
		public EdfAnnotation() { }

		/// <summary>
		/// Initializes a new instance of the EdfAnnotation class
		/// </summary>
		/// <param name="onset"><see cref="Onset"/></param>
		/// <param name="duration"><see cref="Duration"/> </param>
		/// <param name="description"><see cref="Annotation"/></param>
		public EdfAnnotation( double onset, double? duration, string description )
		{
			Onset = onset;
			Duration   = duration;
			Annotation = description;
		}

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

			// For a Timestamped Annotation List, make sure to add the size of each annotation and its delimiter
			foreach( var description in AnnotationList )
			{
				size += Encoding.UTF8.GetByteCount( description );
				size += 1; // 0x14 delimiter
			}

			// If there is a linked channel, add the size of the signal label and the delimiter
			if( !string.IsNullOrEmpty( LinkedChannel ) )
			{
				size += 2; // '@@' delimiter
				size += Encoding.ASCII.GetByteCount( LinkedChannel );
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

			var buffer = new StringBuilder();
			buffer.Append( $"Onset: {Onset}, " );

			if( Duration.HasValue )
			{
				buffer.Append( $"Duration: {Duration}, " );
			}

			buffer.Append( $"Annotation: {Annotation}" );

			if( !string.IsNullOrEmpty( LinkedChannel ) )
			{
				buffer.Append( $", Channel: {LinkedChannel}" );
			}

			return buffer.ToString();
		}

		#endregion
	}
}
