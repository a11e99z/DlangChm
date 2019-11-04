using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using HtmlAgilityPack;

namespace DlangChm
{
	static partial class ChmHelper
	{
		#region ProcessDir
		public static void ProcessDir( string dir, string ver = null )
		{
			ver = ver ?? FindVer( dir ) ?? DateTime.UtcNow.ToShortDateString();
			DVersion = ver;
			Console.WriteLine( $"version: {ver}" );
			TOP = TOP.Replace( "%VER%", ver );

			RootFolder = Path.GetFullPath( dir );
			if (!RootFolder.EndsWith( ""+Path.DirectorySeparatorChar ))
				RootFolder += Path.DirectorySeparatorChar;
			
			ParseDir( RootFolder );
			Console.WriteLine( "\n" );

			IndexDir( RootFolder );
			SaveIndex( CombineDir( RootFolder, "Index.hhk" ) );
			Console.WriteLine( "\n" );

			BooksFromDir( RootFolder );
			Console.WriteLine( "\n" );
		}
		#endregion

		//===========================================================================

		#region Internals
		static string RootFolder, DVersion;

		// htmlFileName : title
		static Dictionary<string, string> Titles
			= new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );

		// relative htmlFileName : linkName
		static Dictionary<string, string> Links
			= new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );

		// removes extra spaces
		static string ToLine( this string txt ) 
		{
			var arr = txt.Split( " \t\v\f\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries );
			return string.Join( " ", arr );
		}

		static int IndexDiff( this string a, string b )
			=> a.Zip( b, ( c1, c2 ) => c1 == c2 ).TakeWhile( v => v ).Count();

		static string RemoveRefFromLink( string lnk, out string lref )
		{
			lref = "";
			var idx = lnk.IndexOf( '#' );
			if (idx < 0) return lnk;
			lref = lnk.Substring( idx + 1 );
			return lnk.Substring( 0, idx );
		}

		static string ReadTitle( string htmlName )
		{
			var doc = new HtmlDocument();
			doc.Load( htmlName, Encoding.UTF8 );
			var root = doc.DocumentNode;
			return root.SelectSingleNode( "//title" )?.InnerText;
		}

		static string CombineDir( string dir, string file )
			=> Path.GetFullPath( Path.Combine( dir, file ) );

		static string FindVer( string dir )
		{
			var cdir = Path.Combine( dir, "changelog" );
			if (!Directory.Exists( cdir )) return null;
			var fn = Directory.GetFiles( cdir, "*.???.*.htm*" )
				.OrderByDescending( n => n ).FirstOrDefault();
			return fn == null ? null : Path.GetFileNameWithoutExtension( fn );
		}

		static T TryElse<T>( Func<T> dg, T def )
		{
			try { return dg(); }
			catch { return def; }
		}

		static bool FileExists( string dir, string file )
		{
			var idx = file.IndexOf( '#' );
			file = idx < 0 ? file : file.Substring( 0, idx );
			return TryElse( () => File.Exists( CombineDir( dir, file ) ), false );
		}

		static string Repeat( this string s, int n )
			=> new StringBuilder( s.Length * n ).Insert( 0, s, n ).ToString();

		static string FindFile( string html, string file )
		{
			var dir = Path.GetDirectoryName( html );
			if (FileExists( dir, file )) return file;
			for (int k = 0; k < 7; ++k)
			{
				file = "../" + file;
				if (FileExists( dir, file )) return file;
			}
			throw new Exception( $"Cannot determing path to {file}" );
		}

		// for replacing Dlang menu to simple header
		static string TOP =
@"<div id=""top"">
	<table><tr>
		<td width=""5%""><img src=""%IMG%"" width=""50""/></td>
		<td width=""5%""><span><font size=""+2"" color=""white"">%VER%</font></span></td>
		<td width=""90%"" style=""vertical-align:bottom; text-align:right;""><span><font size=""-2"">%TEXT%</font></span></td>
	</tr></table>
</div>";

		const string IndexHeader =
@"<!DOCTYPE HTML PUBLIC ""-//IETF//DTD HTML//EN"">
<HTML><HEAD>
	<meta name=""GENERATOR"" content=""MS&reg; HTML Help Workshop 4.1"">
	<!--Sitemap 1.0-->
</HEAD>
<BODY>
   <UL>";

		const string IndexElement =
@"		<LI><OBJECT type=""text/sitemap"">
			 <param name =""Name"" value =""%NAME%"">
			 <param name =""Local"" value=""%URL%"">
		</OBJECT>";

		const string IndexFooter =
@"	</UL>
</BODY>
</HTML>";

		const string NewBook = BookPage + "\n%TAB%<UL>";

		const string BookPage =
@"%TAB%<LI><OBJECT type=""text/sitemap"">
%TAB%	<param name=""Name"" value=""%TITLE%"">
%TAB%	<param name=""Local"" value=""%URL%"">
%TAB%</OBJECT>";

		const string EndBook = @"%TAB%</UL>";
		#endregion
	}
}

//=============================================================================
