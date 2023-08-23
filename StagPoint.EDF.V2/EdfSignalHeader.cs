namespace StagPoint.EDF.Net
{
	internal class EdfSignalHeader
	{
		#region Stored properties 
		
		public EdfAsciiString  Label                    { get; internal set;} = new EdfAsciiString( 16 );
		public EdfAsciiString  TransducerType           { get; internal set;} = new EdfAsciiString( 80 );
		public EdfAsciiString  PhysicalDimension        { get; internal set;} = new EdfAsciiString( 8 );
		public EdfAsciiFloat   PhysicalMinimum          { get; internal set;} = new EdfAsciiFloat( 8 );
		public EdfAsciiFloat   PhysicalMaximum          { get; internal set;} = new EdfAsciiFloat( 8 );
		public EdfAsciiInteger DigitalMinimum           { get; internal set;} = new EdfAsciiInteger( 8 );
		public EdfAsciiInteger DigitalMaximum           { get; internal set;} = new EdfAsciiInteger( 8 );
		public EdfAsciiString  Prefiltering             { get; internal set;} = new EdfAsciiString( 80 );
		public EdfAsciiInteger NumberOfSamplesPerRecord { get; internal set;} = new EdfAsciiInteger( 8 );
		public EdfAsciiString  Reserved                 { get; internal set;} = new EdfAsciiString( 32 );
		
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
