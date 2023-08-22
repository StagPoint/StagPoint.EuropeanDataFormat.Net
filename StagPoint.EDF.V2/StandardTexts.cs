// ReSharper disable IdentifierTypo
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace StagPoint.EDF.Net
{
	public static class StandardTexts
	{
		/// <summary>
		/// Standard labels to indicate whether a file contains EDF, EDF+C (Continuous), or EDF+D (Discontinuous) data. 
		/// </summary>
		public static class FileType
		{
			public const string EDF                    = "";
			public const string EDF_Plus_Continuous    = "EDF+C";
			public const string EDF_Plus_Discontinuous = "EDF+D";
		}
		
		/// <summary>
		/// Standard labels for common signal types. Not a complete list.
		/// https://www.edfplus.info/specs/edftexts.html#signals"/>
		/// </summary>
		public static class SignalType
		{
			public const string EdfAnnotations       = "EDF Annotations";
			public const string Electroencephalogram = "EEG";
			public const string Electrocardiogram    = "ECG";
			public const string Electroöculogram     = "EOG";
			public const string Electroretinogram    = "ERG";
			public const string Electromyogram       = "EMG";
			public const string MagnetoEncephalogram = "MEG";
			public const string MagnetoCardiogram    = "MCG";
			public const string EvokedPotential      = "EP";
			public const string Temperature          = "TEMP";
			public const string Respiration          = "RESP";
			public const string OxygenSaturation     = "SaO2";
		}
	}
}
