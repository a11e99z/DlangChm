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
		// creating Books

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
					var url = Path.Combine( prefx, el.GetAttributeValue( "href", "" ) );
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
				.Select( f => Path.GetFileNameWithoutExtension( f ).ToLower() )
				.OrderBy( f => f ).ToList();

			// split RT and Phobos
			var rt = allFiles.Where(
				f => f.StartsWith( "object" )
					|| f.StartsWith( "dmd" )
					|| f.StartsWith( "rt" ) ).ToList();
			var phob = allFiles.Except( rt ).ToList();

			buildBook( "Library Reference", "index", phob );
			buildBook( "Internals", "object", rt );
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
				int idx = 0;
				foreach (var page in files)
				{
					// Issue #1: one file can be as pages and as book cover
					// FIX: 
					// if next file starts with curr_file_name (w/o ext)
					//		than the one is cover book page
					//		else the one is usual page (probably)
					var isBook = ++idx < files.Count && files[ idx ].StartsWith( page );
					Book.Process( page, isBook );
				}
				// close all subdirs/books
				Book.Process( null );
			}
		}

		//=====================================================================

		#region Book class
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

			public static void Process( string page, bool isBook = false )
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

				_curr.process( page, isBook );
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

			void process( string page, bool isBook = false )
			{
				var idx = _curr._name.IndexDiff( page );
				idx = idx == 0 || page[ idx - 1 ] == '_' ? idx
					: page.LastIndexOf( '_', idx );
				if (_pred != null && idx < _name.Length)
				{
					Close();
					_curr.process( page, isBook );
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

				//if (page == "std_datetime") Debugger.Break();
				if (isBook)
				{
					_curr = new Book( this, page );
					return;
				}

				// just page
				var url = PhobosFileName( page );
				var file = CombineDir( Context.RootFolder, url );
				var title = Context.Titles[ file ];
				Context.File.WriteLine( BookPage.Gen( Tabs, title, url ) );
			}
		}
		#endregion
	}
}
