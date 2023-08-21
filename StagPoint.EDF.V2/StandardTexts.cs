// ReSharper disable IdentifierTypo
namespace StagPoint.EDF.Net
{
	public static class StandardTexts
	{
		/// <summary>
		/// Standard labels for common signal types. Not a complete list.
		/// <seealso cref="https://www.edfplus.info/specs/edftexts.html#signals"/>
		/// </summary>
		public static class SignalType
		{
			public const string Edf_Annotation       = "EDF Annotations";
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
