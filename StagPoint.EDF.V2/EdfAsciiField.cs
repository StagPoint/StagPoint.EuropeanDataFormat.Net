using System.IO;

namespace StagPoint.EDF.Net
{
	public abstract class EdfAsciiField
	{
		public int FieldLength { get; private set; }

		protected EdfAsciiField( int fieldLength )
		{
			this.FieldLength = fieldLength;
		}
		
		public abstract void WriteTo( BinaryWriter  buffer );
		public abstract void ReadFrom( BinaryReader buffer );
	}
}
