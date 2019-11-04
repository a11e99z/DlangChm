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
		// Creating Index

		static void IndexRefs( string html, bool readTitle = false )
		{
			var dir = Path.GetDirectoryName( html );
			// dont want indexing Changelogs
			if (Path.GetFileName( dir ).ToLower() == "changelog") return;

			var doc = new HtmlDocument();
			doc.Load( html, Encoding.UTF8 );
			var root = doc.DocumentNode;
			Console.WriteLine( $"indexing {html}" );

			var currTitle = root.SelectSingleNode( "//title" )?.InnerText
				?? html.Replace( RootFolder, "" );
			var currRel = html.Replace( RootFolder, "" );

			var hrefs = root.SelectNodes( "//a[@href]" )
				?? Enumerable.Empty<HtmlNode>();
			foreach (var nod in hrefs)
			{
				var txt = nod.InnerText?.Trim() ?? "";

				// dont want indexing DMD command keys
				// TODO do I miss something? "-unary"?
				if (txt.Length > 0 && txt[ 0 ] == '-') continue;

				// values with NL are wrong
				txt = txt.Replace( "\r\n", " " ).Replace( "\n", " " );

				var url = nod.GetAttributeValue( "href", "" ).Trim();
				var head = RemoveRefFromLink( url, out var tail );
				if (string.IsNullOrEmpty( head ))
				{
					Links[ $"{currRel}#{tail}" ] = $"{txt} ({currTitle})".Trim();
					continue;
				}

				var full = CombineDir( dir, head );
				var rel = full.Replace( RootFolder, "" );
				var titl = readTitle ? ReadTitle( full ) : Titles[ full ];
				Links[ $"{rel}#{tail}" ] = $"{txt} ({titl})".Trim();
			}
		}

		static void IndexDir( string dir )
		{
			// process files in dir
			foreach (var htmlFile in Directory.EnumerateFiles( dir, "*.htm*" ))
				IndexRefs( htmlFile );//, true );

			// process child dirs
			foreach (var idir in Directory.EnumerateDirectories( dir ))
				IndexDir( idir );
		}

		static void SaveIndex( string file )
		{
			// we dont want write almost same values
			// .html#.byDchar
			// .html#byDchar
			// so remember with dots and later ignore w/o dots
			var dots = new HashSet<string>();
			foreach (var k in Links.Select( e => e.Key ))
				if (k.Contains( "#." ))
					dots.Add( k.Replace( "#.", "#" ).ToLower() );

			using (var fout = new StreamWriter( file, false, Encoding.UTF8 ))
			{
				fout.WriteLine( IndexHeader );

				// sorted elements
				foreach (var p in Links.Select( e => (e.Value, e.Key) ).OrderBy( e => e.Item1 ))
				{
					if (dots.Contains( p.Item2.ToLower() )) continue;

					fout.WriteLine( IndexElement
						.Replace( "%NAME%", p.Item1 )
						.Replace( "%URL%", p.Item2 ) );
				}

				fout.WriteLine( IndexFooter );
			}
		}
	}
}
