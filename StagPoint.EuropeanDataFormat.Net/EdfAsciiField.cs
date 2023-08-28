// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

using System.IO;

namespace StagPoint.EDF.Net
{
	/// <summary>
	/// Base class for all fields that will be stored in an EDF File
	/// </summary>
	public abstract class EdfAsciiField
	{
		/// <summary>
		/// Gets/Sets the number of ASCII characters that will be used to store this field's value
		/// The Field Length for each field is described in the EDF Specification
		/// </summary>
		public int FieldLength { get; private set; }

		/// <summary>
		/// Initializes a new instance of the EdfAsciiFloat class
		/// </summary>
		/// <param name="fieldLength">The length of the ASCII string used to store this field's value <see cref="EdfAsciiField.FieldLength"/></param>
		protected EdfAsciiField( int fieldLength )
		{
			this.FieldLength = fieldLength;
		}
		
		/// <summary>
		/// Write the field's data to the output stream
		/// </summary>
		/// <param name="buffer"></param>
		internal abstract void WriteTo( BinaryWriter  buffer );
		
		/// <summary>
		/// Read the field's data from the input stream 
		/// </summary>
		/// <param name="buffer"></param>
		internal abstract void ReadFrom( BinaryReader buffer );
	}
}
