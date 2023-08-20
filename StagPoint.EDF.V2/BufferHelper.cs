using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace StagPoint.EDF.Net
{
	public static class BufferHelper
	{
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static void WriteToBuffer( BinaryWriter buffer, string value, int fieldLength )
		{
			// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
			if( value.Length >= fieldLength )
			{
				buffer.Write( Encoding.ASCII.GetBytes( value.Substring( 0, fieldLength ) ) );
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
}
