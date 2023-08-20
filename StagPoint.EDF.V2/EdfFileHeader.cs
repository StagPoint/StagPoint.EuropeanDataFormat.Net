namespace StagPoint.EDF.V2;

public class EdfFileHeader
{
	public EdfAsciiInteger  Version              { get; private set; } = new( 8 );
	public EdfAsciiString   PatientInfo          { get; set; }         = new( 80 );
	public EdfAsciiString   RecordingInfo        { get; set; }         = new( 80 );
	public EdfAsciiDateTime StartTime            { get; set; }         = new();
	public EdfAsciiInteger  HeaderRecordSize     { get; private set; } = new( 8 );
	public EdfAsciiString   Reserved             { get; set; }         = new( 44 );
	public EdfAsciiInteger  NumberOfDataRecords  { get; set; }         = new( 8 );
	public EdfAsciiFloat    DurationOfDataRecord { get; set; }         = new( 8 );
	public EdfAsciiInteger  NumberOfSignals      { get; set; }         = new( 4 );
	
	public List<EdfAsciiString>  Labels               { get; set; } = new();
	public List<EdfAsciiString>  TransducerType       { get; set; } = new();
	public List<EdfAsciiString>  PhysicalDimension    { get; set; } = new();
	public List<EdfAsciiInteger> PhysicalMinimum      { get; set; } = new();
	public List<EdfAsciiInteger> PhysicalMaximum      { get; set; } = new();
	public List<EdfAsciiInteger> DigitalMinimum       { get; set; } = new();
	public List<EdfAsciiInteger> DigitalMaximum       { get; set; } = new();
	public List<EdfAsciiString>  Prefiltering         { get; set; } = new();
	public List<EdfAsciiInteger> SamplesPerDataRecord { get; set; } = new();
	public List<EdfAsciiString>  SignalReserved       { get; set; } = new();

	public void WriteToBuffer( BinaryWriter buffer )
	{
		HeaderRecordSize.Value = calculateHeaderRecordSize();
		
		// Write the fixed-size portion of the header
		Version.WriteToBuffer( buffer );
		PatientInfo.WriteToBuffer( buffer );
		StartTime.WriteToBuffer( buffer );
		HeaderRecordSize.WriteToBuffer( buffer );
		Reserved.WriteToBuffer( buffer );
		NumberOfDataRecords.WriteToBuffer( buffer );
		DurationOfDataRecord.WriteToBuffer( buffer );
		NumberOfSignals.WriteToBuffer( buffer );
		
		// Write the signal information 
		Labels.ForEach( x => x.WriteToBuffer( buffer ) );
		TransducerType.ForEach( x => x.WriteToBuffer( buffer ) );
		PhysicalDimension.ForEach( x => x.WriteToBuffer( buffer ) );
		PhysicalMinimum.ForEach( x => x.WriteToBuffer( buffer ) );
		PhysicalMaximum.ForEach( x => x.WriteToBuffer( buffer ) );
		DigitalMinimum.ForEach( x => x.WriteToBuffer( buffer ) );
		DigitalMaximum.ForEach( x => x.WriteToBuffer( buffer ) );
		Prefiltering.ForEach( x => x.WriteToBuffer( buffer ) );
		SamplesPerDataRecord.ForEach( x => x.WriteToBuffer( buffer ) );
		SignalReserved.ForEach( x => x.WriteToBuffer( buffer ) );
	}
	
	#region Private functions 
	
	private int calculateHeaderRecordSize()
	{
		return 256 +                  // The fixed-size part of a header is 256 bytes
		       NumberOfSignals * 16 + // Labels
		       NumberOfSignals * 80 + // Transducer type
		       NumberOfSignals * 8 +  // Physical Dimension
		       NumberOfSignals * 8 +  // Physical Minimum
		       NumberOfSignals * 8 +  // Physical Maximum
		       NumberOfSignals * 8 +  // Digital Minimum
		       NumberOfSignals * 8 +  // Digital Maximum
		       NumberOfSignals * 80 + // Prefiltering
		       NumberOfSignals * 8 +  // Number of samples per Data Record
		       NumberOfSignals * 32;  // Reserved
	}
	
	#endregion 
}
