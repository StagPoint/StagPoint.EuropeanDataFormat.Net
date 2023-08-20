using System.IO;

namespace StagPoint.EDF.Net
{
	public interface IEdfAsciiField
	{
		int  FieldLength { get; }
		
		void WriteToBuffer( BinaryWriter  buffer );
		void ReadFromBuffer( BinaryReader buffer );
	}
}
