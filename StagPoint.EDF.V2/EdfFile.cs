using System.Collections.Generic;
using System.IO;

namespace StagPoint.EDF.Net
{
	public class EdfFile
	{
		public EdfFileHeader Header { get; set; } = new EdfFileHeader();

		public List<EdfSignalBase> Signals { get; set; } = new List<EdfSignalBase>();

		public void ReadFrom( string filename )
		{
			using( var file = File.OpenRead( filename ) )
			{
				ReadFrom( file );
			}
		}

		public void ReadFrom( Stream file )
		{
			using( var reader = new BinaryReader( file ) )
			{
				this.Header.ReadFrom( reader );
				allocateSignals();
			}
		}
		
		private void allocateSignals()
		{
			Signals.Clear();

			foreach( var header in Header.SignalHeaders )
			{
				if( header.Label.Value.Equals( StandardTexts.SignalType.Edf_Annotation ) )
				{
					Signals.Add( new EdfAnnotationSignal( header ) );
				}
				else
				{
					Signals.Add( new EdfStandardSignal( header ) );
				}
			}
		}
	}
}
