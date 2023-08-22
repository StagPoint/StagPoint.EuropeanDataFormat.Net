namespace StagPoint.EDF.Net
{
	public class EdfSignalBase
	{
		#region Stored properties 
		
		public EdfAsciiString  Label                    { get; set; } = new EdfAsciiString( 16 );
		public EdfAsciiString  TransducerType           { get; set; } = new EdfAsciiString( 80 );
		public EdfAsciiString  PhysicalDimension        { get; set; } = new EdfAsciiString( 8 );
		public EdfAsciiFloat   PhysicalMinimum          { get; set; } = new EdfAsciiFloat( 8 );
		public EdfAsciiFloat   PhysicalMaximum          { get; set; } = new EdfAsciiFloat( 8 );
		public EdfAsciiInteger DigitalMinimum           { get; set; } = new EdfAsciiInteger( 8 );
		public EdfAsciiInteger DigitalMaximum           { get; set; } = new EdfAsciiInteger( 8 );
		public EdfAsciiString  Prefiltering             { get; set; } = new EdfAsciiString( 80 );
		public EdfAsciiInteger NumberOfSamplesPerRecord { get; set; } = new EdfAsciiInteger( 8 );
		public EdfAsciiString  Reserved                 { get; set; } = new EdfAsciiString( 32 );
		
		#endregion
		
		#region Constructors

		public EdfSignalBase( EdfSignalHeader header )
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
