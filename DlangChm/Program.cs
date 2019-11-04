using System;
using System.IO;

namespace DlangChm
{
	static class Program
	{
		public static void Main( string[] args )
		{
			var dir = args.Length < 1 ? null : args[ 0 ];
			if (dir == null || !Directory.Exists( dir ))
			{
				Console.WriteLine( "EXE <copy of Dlang docs folder like C:/dmd2/html/d-copy> {version-string}" );
				return;
			}

			ChmHelper.ProcessDir( dir, args.Length > 1 ? args[ 1 ] : null );

			Console.WriteLine( "Done." );
			Console.ReadLine();
		}
	}
}
