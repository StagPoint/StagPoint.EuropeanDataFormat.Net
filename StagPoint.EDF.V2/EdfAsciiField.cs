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
		
		internal abstract void WriteTo( BinaryWriter  buffer );
		internal abstract void ReadFrom( BinaryReader buffer );
	}
}
