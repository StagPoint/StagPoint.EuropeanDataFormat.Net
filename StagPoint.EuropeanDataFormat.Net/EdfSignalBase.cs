// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace StagPoint.EDF.Net
{
	public class EdfSignalBase
	{
		#region Stored properties 
		
		public EdfAsciiString  Label                    { get; } = new EdfAsciiString( 16 );
		public EdfAsciiString  TransducerType           { get; } = new EdfAsciiString( 80 );
		public EdfAsciiString  PhysicalDimension        { get; } = new EdfAsciiString( 8 );
		public EdfAsciiFloat   PhysicalMinimum          { get; } = new EdfAsciiFloat( 8 );
		public EdfAsciiFloat   PhysicalMaximum          { get; } = new EdfAsciiFloat( 8 );
		public EdfAsciiInteger DigitalMinimum           { get; } = new EdfAsciiInteger( 8 );
		public EdfAsciiInteger DigitalMaximum           { get; } = new EdfAsciiInteger( 8 );
		public EdfAsciiString  Prefiltering             { get; } = new EdfAsciiString( 80 );
		public EdfAsciiInteger NumberOfSamplesPerRecord { get; } = new EdfAsciiInteger( 8 );
		public EdfAsciiString  Reserved                 { get; } = new EdfAsciiString( 32 );
		
		#endregion
		
		#region Constructors

		protected EdfSignalBase()
		{
			PhysicalMinimum.Value = -32767;
			PhysicalMaximum.Value = 32767;
			DigitalMinimum.Value  = -32767;
			DigitalMaximum.Value  = 32767;
		}

		internal EdfSignalBase( EdfSignalHeader header )
		{
			Label.Value                    = header.Label.Value;
			TransducerType.Value           = header.TransducerType.Value;
			PhysicalDimension.Value        = header.PhysicalDimension.Value;
			PhysicalMinimum.Value          = header.PhysicalMinimum.Value;
			PhysicalMaximum.Value          = header.PhysicalMaximum.Value;
			DigitalMinimum.Value           = header.DigitalMinimum.Value;
			DigitalMaximum.Value           = header.DigitalMaximum.Value;
			Prefiltering.Value             = header.Prefiltering.Value;
			NumberOfSamplesPerRecord.Value = header.NumberOfSamplesPerRecord.Value;
			Reserved.Value                 = header.Reserved.Value;
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
