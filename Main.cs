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
	static class Program
	{
		#region Changing HTMLs
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
			var anchors = scripts.Where( n => contains( n, "listanchors" ));
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
				var href = nod.Attributes["href"].Value.Trim().ToLower();
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
		#endregion

		//===========================================================================

		#region Creating Index
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
		#endregion

		//===========================================================================

		#region Creating books
		static void BooksFromDir( string root )
		{
			// process files in dir
			//foreach (var htmlFile in Directory.EnumerateFiles( dir, "*.htm*" ))
			//	IndexRefs( htmlFile );//, true );

			// process child dirs
			foreach (var bdir in Directory.EnumerateDirectories( root ))
			{
				var name = Path.GetFileName( bdir ).ToLower();
				switch (name)
				{
					case "spec": BuildSpecBook( bdir ); break;
					case "phobos": BuildPhobosBook( bdir ); break;
					case "articles": BuildArticlesBook( bdir ); break;
					case "changelog": BuildChangelogBook( bdir ); break;
				}
			}
		}

		static string Gen( this string templ, string tabs, string title, string url )
			=> templ.Replace( "%TAB%", tabs )
				.Replace( "%TITLE%", title )
				.Replace( "%URL%", url );

		static void BuildChangelogBook( string dir )
		{
			Console.WriteLine( "building book: Changelog" );
			if (!FileExists( dir, "index.html" ))
			{
				Console.WriteLine( "please create index.html at /changelog" );
				return;
			}

			var fileName = CombineDir( RootFolder, "Changelog.hhc" );
			using (var fout = new StreamWriter( fileName, false, Encoding.UTF8 ))
			{
				// header
				fout.WriteLine( NewBook.Gen( "\t", "Changelog", "changelog/index.html" ) );

				// pages
				var tab2 = "\t\t";
				var vers = Directory.GetFiles( dir, "*.???*.htm*" )
					.OrderByDescending( f => f );
				foreach (var fn in vers)
				{
					var url = fn.Replace( RootFolder, "" );
					var title = Titles[ fn ];
					fout.WriteLine( BookPage.Gen( tab2, title, url ) );
				}

				//footer
				fout.WriteLine( EndBook.Gen( "\t", "", "" ) );
			}
		}

		static void BuildArticlesBook( string dir )
		{
			Console.WriteLine( "building book: Articles" );
			var content = "index.html";
			if (!FileExists( dir, content )) content = "articles.html";
			if (!FileExists( dir, content ))
			{
				Console.WriteLine( "please create articles- or index.html at /articles" );
				return;
			}
			var prefx = dir.Replace( RootFolder, "" );

			var fileName = CombineDir( RootFolder, "Articles.hhc" );
			using (var fout = new StreamWriter( fileName, false, Encoding.UTF8 ))
			{
				// header
				var hdr = NewBook.Gen( "\t", "Articles", Path.Combine( prefx, content ) );
				fout.WriteLine( hdr );

				// pages
				var doc = new HtmlDocument();
				doc.Load( CombineDir( dir, content ), Encoding.UTF8 );
				var tab2 = "\t\t";
				// <div class="boxes"> <div class="row"> 
				//		<div class="item"> <h4><a href="../articles/faq.html">FAQ</a></h4>
				var qry = "//div[@class=\"boxes\"]//a";
				foreach (var el in doc.DocumentNode.SelectNodes( qry ))
				{
					var url = el.GetAttributeValue( "href", "" );
					url = CombineDir( dir, url ).Replace( RootFolder, "" );
					var title = el.InnerText.ToLine();
					fout.WriteLine( BookPage.Gen( tab2, title, url ) );
				}

				//footer
				fout.WriteLine( EndBook.Gen( "\t", "", "" ) );
			}
		}

		static void BuildSpecBook( string dir )
		{
			Console.WriteLine( "building book: Specification" );
			var content = "spec.html";
			if (!FileExists( dir, content )) content = "index.html";
			if (!FileExists( dir, content ))
			{
				Console.WriteLine( "please create index- or spec.html at /spec" );
				return;
			}
			var prefx = dir.Replace( RootFolder, "" );

			var fileName = CombineDir( RootFolder, "Spec.hhc" );
			using (var fout = new StreamWriter( fileName, false, Encoding.UTF8 ))
			{
				// header
				var hdr = NewBook.Gen( "\t", "Language Reference", Path.Combine( prefx, content ) );
				fout.WriteLine( hdr );

				// pages
				var doc = new HtmlDocument();
				doc.Load( CombineDir( dir, content ), Encoding.UTF8 );
				var tab2 = "\t\t";
				// <div class="hyphenate" id="content"><ul><li><a>
				var qry = "//div[@id=\"content\"]/ul/li/a";
				foreach (var el in doc.DocumentNode.SelectNodes( qry ))
				{
					var url = Path.Combine( prefx, el.GetAttributeValue( "href", "" ));
					var title = el.InnerText.ToLine();
					fout.WriteLine( BookPage.Gen( tab2, title, url ) );
				}

				//footer
				fout.WriteLine( EndBook.Gen( "\t", "", "" ) );
			}
		}

		static void BuildPhobosBook( string dir )
		{
			Console.WriteLine( "building book: Phobos" );

			var allFiles = Directory.EnumerateFiles( dir, "*.htm*" )
				.Select( f => Path.GetFileNameWithoutExtension( f ).ToLower()).OrderBy( f => f ).ToList();
			var rt = allFiles.Where( // object
				f => f.StartsWith( "object")
					|| f.StartsWith( "dmd")
					|| f.StartsWith( "rt" )).ToList();
			var phob = allFiles.Except( rt ).ToList(); // index

			buildBook( "Library Reference", "index", phob );
			buildBook( "Internals", "object", rt );
		}

		class Context
		{
			public TextWriter File;
			public string RootFolder, Empty;
			public Dictionary<string, string> Titles;
		}

		class Book
		{
			static Book _curr;
			public static Context Context { get; set; }

			public static void Process( string page )
			{
				if (page == null)
				{
					while (_curr != null) _curr.Close();
					return;
				}

				if (_curr == null)
				{
					Debug.Assert( !page.Contains( '_' ) );
					_curr = new Book( null, page );
					return;
				}

				_curr.process( page );
			}

			public static string PhobosFileName( string page )
				=> $"phobos\\{page}.html";

			readonly Book _pred;
			readonly string _name;

			public Book( Book pred, string name )
			{
				_pred = pred;
				_name = name;
				PredTabs = pred?.Tabs ?? "\t";
				Tabs = "\t" + PredTabs;

				var url = PhobosFileName( _name );
				var file = CombineDir( Context.RootFolder, url );
				var dots = name.Replace( '_', '.' );
				var title = dots;
				if (!FileExists( Context.RootFolder, url ))
				{
					var htm = Context.Empty.Replace( "%TITLE%", dots );
					File.WriteAllText( file, htm, Encoding.UTF8 );
					Context.Titles[ file ] = dots;
				}
				else try { title = Context.Titles[ file ]; } catch { }

				// header
				var hdr = NewBook.Gen( PredTabs, title, url );
				Context.File.WriteLine( hdr );
			}

			public string Tabs { get; private set; }
			public string PredTabs { get; private set; }

			public void Close()
			{
				Context.File.WriteLine( EndBook.Gen( PredTabs, "", "" ) );
				_curr = _pred;
			}

			void process( string page )
			{
				var idx = _curr._name.IndexDiff( page );
				idx = idx == 0 || page[ idx - 1 ] == '_' ? idx
					: page.LastIndexOf( '_', idx );
				if (_pred != null && idx < _name.Length)
				{
					Close();
					_curr.process( page );
					return;
				}

				var tail = page.Substring( idx )
					.Split( "_".ToCharArray(), StringSplitOptions.RemoveEmptyEntries );
				if (tail.Length > 1)
				{
					var pre = idx > 0 ? _name.Substring( 0, idx ) + '_' : "";
					_curr = new Book( this, pre + tail[ 0 ] );
					_curr.process( page );
					return;
				}

				// just page
				var url = PhobosFileName( page );
				var file = CombineDir( Context.RootFolder, url );
				var title = Context.Titles[ file ];
				Context.File.WriteLine( BookPage.Gen( Tabs, title, url ) );
			}
		}

		static void buildBook( string name, string index, List<string> files )
		{
			// dumb for 1st page book that dont have own page
			var empty = File.ReadAllText( CombineDir( RootFolder, "empty.page" ), Encoding.UTF8 );
			empty = empty.Replace( "%VER%", DVersion );

			var main = files.FirstOrDefault( f => f == index );
			if (main == null)
			{
				Console.WriteLine( $"Phobos: can not find {index} in files" );
				return;
			}
			files.Remove( main );

			var fileName = CombineDir( RootFolder, $"{name}.hhc" );
			using (var fout = new StreamWriter( fileName, false, Encoding.UTF8 ))
			{
				Book.Context = new Context
				{
					Empty = empty,
					File = fout,
					RootFolder = RootFolder,
					Titles = Titles
				};

				Book.Process( index );
				foreach (var page in files)
					Book.Process( page );
				Book.Process( null );
			}
		}
		#endregion

		//===========================================================================

		#region Main
		static void Main( string[] args )
		{
			var dir = args.Length < 1 ? null : args[ 0 ];
			if (dir == null || !Directory.Exists( dir ))
			{
				Console.WriteLine( "EXE <copy of Dlang docs folder like C:/dmd2/html/d-copy> {version-string}" );
				return;
			}

			string ver = args.Length > 1 ? args[ 1 ] : FindVer( dir );
			if (ver == null) ver = DateTime.UtcNow.ToShortDateString();
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

			Console.WriteLine( "Done." );
			Console.ReadLine();
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
