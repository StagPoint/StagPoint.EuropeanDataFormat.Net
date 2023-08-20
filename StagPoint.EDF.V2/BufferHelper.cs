using System.Runtime.CompilerServices;
using System.Text;

namespace StagPoint.EDF.V2;

public static class BufferHelper
{
	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static void WriteToBuffer( BinaryWriter buffer, string value, int fieldLength )
	{
		if( value.Length >= fieldLength )
		{
			buffer.Write( Encoding.ASCII.GetBytes( value[ ..fieldLength ] ) );
		}
		else
		{
			buffer.Write( Encoding.ASCII.GetBytes( value.PadRight( fieldLength ) ) );
		}
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static string ReadFromBuffer( BinaryReader reader, int fieldLength )
	{
		return Encoding.ASCII.GetString( reader.ReadBytes( fieldLength ) ).Trim();
	}
}
