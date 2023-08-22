namespace StagPoint.EDF.Net
{
	public class EdfSignalBase
	{
		#region Stored properties 
		
		public EdfAsciiString  Label                    { get; internal set; } = new EdfAsciiString( 16 );
		public EdfAsciiString  TransducerType           { get; internal set; } = new EdfAsciiString( 80 );
		public EdfAsciiString  PhysicalDimension        { get; internal set; } = new EdfAsciiString( 8 );
		public EdfAsciiFloat   PhysicalMinimum          { get; internal set; } = new EdfAsciiFloat( 8 );
		public EdfAsciiFloat   PhysicalMaximum          { get; internal set; } = new EdfAsciiFloat( 8 );
		public EdfAsciiInteger DigitalMinimum           { get; internal set; } = new EdfAsciiInteger( 8 );
		public EdfAsciiInteger DigitalMaximum           { get; internal set; } = new EdfAsciiInteger( 8 );
		public EdfAsciiString  Prefiltering             { get; internal set; } = new EdfAsciiString( 80 );
		public EdfAsciiInteger NumberOfSamplesPerRecord { get; internal set; } = new EdfAsciiInteger( 8 );
		public EdfAsciiString  Reserved                 { get; internal set; } = new EdfAsciiString( 32 );
		
		#endregion
		
		#region Constructors

		internal EdfSignalBase( EdfSignalHeader header )
		{
			Label.Value                    = header.Label;
			TransducerType.Value           = header.TransducerType;
			PhysicalDimension.Value        = header.PhysicalDimension;
			PhysicalMinimum.Value          = header.PhysicalMinimum;
			PhysicalMaximum.Value          = header.PhysicalMaximum;
			DigitalMinimum.Value           = header.DigitalMinimum;
			DigitalMaximum.Value           = header.DigitalMaximum;
			Prefiltering.Value             = header.Prefiltering;
			NumberOfSamplesPerRecord.Value = header.NumberOfSamplesPerRecord;
			Reserved.Value                 = header.Reserved;
		}
		
		#endregion 
		
		#region Base class overrides

		public override string ToString()
		{
			return $"{Label.Value} ({TransducerType.Value})";
		}

		#endregion 
	}
}
