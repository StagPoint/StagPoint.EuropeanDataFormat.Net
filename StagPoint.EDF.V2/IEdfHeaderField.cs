namespace StagPoint.EDF.V2;

public interface IEdfHeaderField
{
	int  FieldLength { get;  }
	void WriteToBuffer( BinaryWriter  buffer );
	void ReadFromBuffer( BinaryReader buffer );
}
