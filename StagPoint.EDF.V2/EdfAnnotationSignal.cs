using System;
using System.Collections.Generic;

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
		/// <param name="header">And EdfSignalHeader instance containing all of the essential Signal information</param>
		internal EdfAnnotationSignal( EdfSignalHeader header ) : base( header )
		{
		}
		
		#endregion
	}

	public class EdfAnnotation
	{
		public double Onset      { get; set; } = default;
		public double Duration   { get; set; } = default;
		public string Annotation { get; set; } = string.Empty;

		/// <summary>
		/// TimeKeeping Annotations are automatically stored in the file for purposes of
		/// indicating when each DataRecord begins relative to the start of the file.
		/// </summary>
		public bool IsTimeKeepingAnnotation
		{
			get => Math.Abs( Onset ) > float.Epsilon && 
			       Duration == 0.0 && 
			       Annotation == String.Empty;
		}
	}
}
