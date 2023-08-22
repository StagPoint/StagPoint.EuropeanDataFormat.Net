namespace StagPoint.EDF.Net
{
	internal class EdfSignalHeader
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
		
		#region Derived properties

		public double ScaleFactor { get { return (PhysicalMaximum - PhysicalMinimum) / (DigitalMaximum - DigitalMinimum); } }

		#endregion
		
		#region Base class overrides

		public override string ToString()
		{
			return $"'{Label}' - {TransducerType} - {NumberOfSamplesPerRecord} - Scale: {ScaleFactor:F3}";
		}

		#endregion
	}
}
