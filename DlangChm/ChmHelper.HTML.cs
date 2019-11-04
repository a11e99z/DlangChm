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
		// Changing HTMLs

		static void Parse( string html, string img, string jqry )
		{
			var doc = new HtmlDocument();
			doc.Load( html, Encoding.UTF8 );
			var root = doc.DocumentNode;
			Console.WriteLine( $"parsing {html}" );

			// change TOP
			var del = root.SelectSingleNode( "//div[@id=\"copyright\"]" );
			var cprt = del?.InnerHtml;
			del?.Remove();
			var top = TOP.Replace( "%TEXT%", cprt ).Replace( "%IMG%", img );
			del = root.SelectSingleNode( "//div[@id=\"top\"]" );
			//del?.Remove();
			del?.ParentNode.ReplaceChild(
				HtmlNode.CreateNode( top ), del );

			// delete ISSUES
			del = root.SelectSingleNode( "//div[@id=\"tools\"]" );
			del?.Remove();

			// delete RIGHT TREE, CHM has own
			del = root.SelectSingleNode( "//div[@class=\"subnav-helper\"]" );
			del?.Remove();
			del = root.SelectSingleNode( "//div[@class=\"subnav\"]" );
			del?.Remove();

			// replace ARROWS (no need load font for symbols "<" and ">"
			// <i class="fa fa-angle-left" aria-hidden="true"></i>
			var upd = root.SelectSingleNode( "//i[@class=\"fa fa-angle-left\"]" );
			upd?.ParentNode.ReplaceChild(
				HtmlNode.CreateNode( "<b>&lt;</b>" ), upd );
			// <i class="fa fa-angle-right" aria-hidden="true"></i>
			upd = root.SelectSingleNode( "//i[@class=\"fa fa-angle-right\"]" );
			upd?.ParentNode.ReplaceChild(
				HtmlNode.CreateNode( "<b>&gt;</b>" ), upd );

			var dir = Path.GetDirectoryName( html );
			// SCRIPTs: delete all except jQuery & listanchors
			bool contains( HtmlNode hn, string what )
			{
				var txt = (hn?.InnerText ?? "").ToLower().Replace( " ", "" );
				var attr = hn.GetAttributeValue( "src", "" );
				return (txt + attr).Contains( what );
			}
			var scripts = (root.SelectNodes( "//script" )
				?? Enumerable.Empty<HtmlNode>()).ToArray();
			var delScripts = scripts;
			var anchors = scripts.Where( n => contains( n, "listanchors" ) );
			if (anchors.Count() == 2)
			{
				var fst = anchors.First();
				var htm = $"<script type=\"text/javascript\" src=\"{jqry}\"/>\n";
				fst.ParentNode.InsertBefore( HtmlNode.CreateNode( htm ), fst );
				delScripts = scripts.Except( anchors ).ToArray();
			}
			// delete others
			foreach (var scr in delScripts)
				scr.Remove();

			// replece hrefs that are !local and !exists to text
			foreach (var nod in root.SelectNodes( "//*[@href]" ))
			{
				var href = nod.Attributes[ "href" ].Value.Trim().ToLower();
				if (href[ 0 ] == '#') continue;
				if (!FileExists( dir, href ))
					nod.ParentNode.ReplaceChild(
						HtmlNode.CreateNode( nod.InnerText ), nod );
			}

			// remove "- D programming language" from title
			const string Dlang = "- d programming language";
			upd = root.SelectSingleNode( "//title" );
			if ((upd?.InnerText ?? "").ToLower().EndsWith( Dlang ))
			{
				var ih = upd.InnerHtml;
				upd.InnerHtml = ih.Substring( 0, ih.Length - Dlang.Length ).Trim();
			}

			Titles[ html ] = upd?.InnerText ?? html.Replace( RootFolder, "" );

			// remove empty lines
			root.InnerHtml = Regex.Replace( root.InnerHtml, @"^\s+$[\r\n]*", "", RegexOptions.Multiline );
			doc.Save( html, Encoding.UTF8 );

			// bonus:
			// at this point out HTMLs takes 15Mb on disk instead 85Mb (for v2.088.0)
		}

		static void ParseDir( string dir )
		{
			if (!Directory.Exists( dir ))
				throw new ArgumentException( $"Folder {dir} doesn\'t exists" );

			// process files in dir
			foreach (var htmlFile in Directory.EnumerateFiles( dir, "*.htm*" ))
			{
				var img = FindFile( htmlFile, "images/dlogo.png" );
				var jqry = FindFile( htmlFile, "js/jquery-1.7.2.min.js" );
				Parse( htmlFile, img, jqry );
			}

			// process child dirs
			foreach (var idir in Directory.EnumerateDirectories( dir ))
				ParseDir( idir );
		}
	}
}
