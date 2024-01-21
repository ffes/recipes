using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Ionic.Zip;
using Ionic.Zlib;
using Recipes.Models;

namespace Recipes
{
	public class GenerateEpub(List<RecipeModel> recipes, List<Keyword> keywords, List<Document> documents) : GenerateBase(recipes, keywords, documents)
	{
		public override bool Enabled => appsettings.EPUB.Enabled;

		private XmlDocument CreateXmlDocument(string language, string title)
		{
			// It all starts with a document
			var doc = new XmlDocument();

			// Add the html root element
			var html = doc.CreateElement("html");
			html.SetAttribute("xmlns", "http://www.w3.org/1999/xhtml");
			html.SetAttribute("xml:lang", language);
			html.SetAttribute("lang", language);
			doc.AppendChild(html);

			// Create an XML declaration
			var xmldecl = doc.CreateXmlDeclaration("1.0", null, null);
			doc.InsertBefore(xmldecl, html);

			// Create the head and add it to the html
			var head = doc.CreateElement("head");
			html.AppendChild(head);

			// Add basics to head
			var meta = doc.CreateElement("meta");
			meta.SetAttribute("content", "http://www.w3.org/1999/xhtml; charset=utf-8");
			meta.SetAttribute("http-equiv", "Content-Type");
			head.AppendChild(meta);

			var ttl = doc.CreateElement("title");
			ttl.InnerXml = title;
			head.AppendChild(ttl);

			return doc;
		}

		private void AddTimeSpan(XmlDocument doc, XmlElement element, TimeSpan time, string title)
		{
			// Is the time set
			if (time.TotalMinutes == 0)
				return;

			// Do we need to add a break?
			if (!element.IsEmpty)
				element.AppendChild(doc.CreateElement("br"));

			// Add the time
			element.AppendChild(doc.CreateTextNode($"{title}: {time.ToReadableString()}"));
		}

		private void AddRecipeToHtml(XmlDocument doc, XmlNode html, RecipeModel recipe)
		{
			// First add the body element
			var body = doc.CreateElement("body");
			html.AppendChild(body);

			// Add the name of the recipe
			var title = doc.CreateElement("h1");
			title.InnerXml = recipe.Name;
			body.AppendChild(title);

			// Add the description
			if (!string.IsNullOrWhiteSpace(recipe.Description))
			{
				var descr = doc.CreateElement("p");
				descr.InnerXml = recipe.Description;
				body.AppendChild(descr);
			}

			// Add the author or publisher
			var by = doc.CreateElement("p");
			if (!string.IsNullOrWhiteSpace(recipe.Author))
			{
				by.InnerXml = $"Gemaakt door {recipe.Author}";
			}
			else if (!string.IsNullOrWhiteSpace(recipe.Publisher))
			{
				by.InnerXml= "Gepubliceerd door ";

				// Add a link to original URL
				if (recipe.PublishedURL != null)
				{
					var pub = doc.CreateElement("a");
					pub.InnerXml = recipe.Publisher;

					var url = recipe.PublishedURL.AbsoluteUri;
					pub.SetAttribute("href", url);
					by.AppendChild(pub);
				}
				else
					by.InnerXml += recipe.Publisher;
			}

			// Add the publication date
			if (recipe.DatePublished.Year > 1900)
			{
				if (string.IsNullOrWhiteSpace(by.InnerXml))
					by.InnerXml = "Gepubliceerd";

				by.InnerXml += " in " + recipe.DatePublished.ToString("MMMM yyyy", CultureInfo.GetCultureInfo(recipe.InLanguage));
			}
			body.AppendChild(by);

			// Add the time(s)
			var time = doc.CreateElement("p");
			AddTimeSpan(doc, time, recipe.PrepTime, "Voorbereidingstijd");
			AddTimeSpan(doc, time, recipe.CookTime, "Bereidingstijd");
			AddTimeSpan(doc, time, recipe.TotalTime, "Totale bereidingstijd");
			if (!time.IsEmpty)
				body.AppendChild(time);

			// Add how much the recipe yields
			if (!string.IsNullOrWhiteSpace(recipe.Yield))
			{
				var yields = doc.CreateElement("p");
				yields.InnerText = $"Voor {recipe.Yield}";
				body.AppendChild(yields);
			}

			// Add the ingredients
			var ingr_title = doc.CreateElement("h2");
			ingr_title.InnerXml = "Ingrediënten";
			body.AppendChild(ingr_title);

			var ingredients = doc.CreateElement("ul");
			body.AppendChild(ingredients);

			foreach (var ingredient in recipe.Ingredients)
			{
				var li = doc.CreateElement("li");
				li.InnerText = ingredient;
				ingredients.AppendChild(li);
			}

			// Add the instructions
			var instr_title = doc.CreateElement("h2");
			instr_title.InnerXml = "Bereidingswijze";
			body.AppendChild(instr_title);

			var instructions = doc.CreateElement("ol");
			body.AppendChild(instructions);
			foreach (var instruction in recipe.Instructions)
			{
				var li = doc.CreateElement("li");
				li.InnerXml = instruction.ToString();
				instructions.AppendChild(li);
			}
		}

		private void AddDocumentToHtml(XmlDocument doc, XmlNode html, Document document)
		{
			// An XML document can only have one root element, so first we add the body around it
			string xhtml = $"<body>{document.Html}</body>";

			// Now we load that XHTML into another XmlDocument
			var doc2 = new XmlDocument();
			doc2.LoadXml(xhtml);

			// Then we import that second XmlDocument in our XmlDocument
			var imported = doc.ImportNode(doc2.DocumentElement, true);

			// Now add the imported body node to the html
			html.AppendChild(imported);
		}

		private void Write(string dir, RecipeModel recipe = null, Document document = null)
		{
			if (recipe == null && document == null)
				return;

			// It all starts with a document
			var doc = CreateXmlDocument(appsettings.General.Language, recipe?.Name ?? document.Name);
			var html = doc.SelectSingleNode("/html");

			// Add the recipe or the document
			if (recipe != null)
				AddRecipeToHtml(doc, html, recipe);
			else
				AddDocumentToHtml(doc, html, document);

			// Save the document
			doc.Save(Path.Combine(dir, recipe?.FilenameHtml ?? document.FilenameHtml));
		}

		private void WriteCoverPage(string dir)
		{
			// It all starts with a document
			var doc = CreateXmlDocument(appsettings.General.Language, "Cover page");
			var html = doc.SelectSingleNode("/html");

			// Add the body and fill it
			var body = doc.CreateElement("body");
			html.AppendChild(body);

			// Add the name of the ebook
			var title = doc.CreateElement("h1");
			title.InnerXml = appsettings.General.Name;
			body.AppendChild(title);

			// Add the author
			var author = doc.CreateElement("h2");
			author.InnerXml = appsettings.General.Author;
			body.AppendChild(author);

			// Add an empty line
			var separator = doc.CreateElement("p");
			separator.InnerXml = "\u00A0";
			body.AppendChild(separator);

			// Add the publication date
			var pubdate = doc.CreateElement("p");
			pubdate.InnerXml = DateTime.Now.ToString("d MMMM yyyy");
			body.AppendChild(pubdate);

			// Save the document
			doc.Save(Path.Combine(dir, "coverpage.html"));
		}

		private void WriteContainerXML(string dir)
		{
			// It all starts with a document
			var doc = new XmlDocument();

			var container = doc.CreateElement("container");
			container.SetAttribute("version", "1.0");
			container.SetAttribute("xmlns", "urn:oasis:names:tc:opendocument:xmlns:container");
			doc.AppendChild(container);

			// Create an XML declaration
			var xmldecl = doc.CreateXmlDeclaration("1.0", null, null);
			doc.InsertBefore(xmldecl, container);

			// Add the rootfile
			var rootfiles = doc.CreateElement("rootfiles");
			container.AppendChild(rootfiles);

			var rootfile = doc.CreateElement("rootfile");
			rootfile.SetAttribute("full-path", "content.opf");
			rootfile.SetAttribute("media-type", "application/oebps-package+xml");
			rootfiles.AppendChild(rootfile);

			doc.Save(Path.Combine(dir, "container.xml"));
		}

		private XmlElement CreateTocNavPoint(XmlDocument doc, string id, int order, string name, string filename)
		{
			var navPoint = doc.CreateElement("navPoint");
			navPoint.SetAttribute("id", id);
			navPoint.SetAttribute("playOrder", order.ToString());

			var navLabel = doc.CreateElement("navLabel");
			var navLabelText = doc.CreateElement("text");
			navLabelText.InnerXml = name;
			navLabel.AppendChild(navLabelText);
			navPoint.AppendChild(navLabel);

			var content = doc.CreateElement("content");
			content.SetAttribute("src", filename);
			navPoint.AppendChild(content);

			return navPoint;
		}

		private void WriteTOC(string dir)
		{
			// It all starts with a document
			var doc = new XmlDocument();

			// Add the html root element
			var ncx = doc.CreateElement("ncx");
			ncx.SetAttribute("xmlns", "http://www.daisy.org/z3986/2005/ncx/");
			ncx.SetAttribute("version", "2005-1");
			doc.AppendChild(ncx);

			// Create an XML declaration
			var xmldecl = doc.CreateXmlDeclaration("1.0", null, null);
			doc.InsertBefore(xmldecl, ncx);

			// Create the head and add to the ncx
			var head = doc.CreateElement("head");
			ncx.AppendChild(head);

			// Add the values to the head
			var meta_uid = doc.CreateElement("meta");
			meta_uid.SetAttribute("name", "dtb:uid");
			meta_uid.SetAttribute("content", appsettings.EPUB.BookId.ToString("D"));
			head.AppendChild(meta_uid);

			var meta_depth = doc.CreateElement("meta");
			meta_depth.SetAttribute("name", "dtb:depth");
			meta_depth.SetAttribute("content", "1");
			head.AppendChild(meta_depth);

			// Add the title of the cookbook
			var docTitle = doc.CreateElement("docTitle");
			ncx.AppendChild(docTitle);

			var title = doc.CreateElement("text");
			title.InnerXml = appsettings.General.Name;
			docTitle.AppendChild(title);

			// The actual TOC starts with a navMap
			var navMap = doc.CreateElement("navMap");
			ncx.AppendChild(navMap);

			// First add the documents
			int i = 0;
			foreach (var document in Documents)
			{
				navMap.AppendChild(CreateTocNavPoint(doc, document.EpubID, ++i, document.Name, "OEBPS/" + document.FilenameHtml));
			}

			// Now add the recipes
			foreach (var recipe in Recipes)
			{
				navMap.AppendChild(CreateTocNavPoint(doc, recipe.EpubID, ++i, recipe.Name, "OEBPS/" + recipe.FilenameHtml));
			}

			// Finally add the index page
			navMap.AppendChild(CreateTocNavPoint(doc, "index", ++i, "Index", "OEBPS/indexpage.html"));

			doc.Save(Path.Combine(dir, "toc.ncx"));
		}

		private void WriteContentOpf(string dir)
		{
			// It all starts with a document
			var doc = new XmlDocument();

			// Define the namespace URLs, and other strings
			const string opf_namespace = "http://www.idpf.org/2007/opf";
			const string dc_namespace = "http://purl.org/dc/elements/1.1/";
			const string coverpage = "coverpage";
			const string indexpage = "indexpage";

			// Add the html root element
			var package = doc.CreateElement("package", opf_namespace);
			package.SetAttribute("version", "2.0");
			package.SetAttribute("unique-identifier", "BookId");
			doc.AppendChild(package);

			// Create an XML declaration
			var xmldecl = doc.CreateXmlDeclaration("1.0", null, null);
			doc.InsertBefore(xmldecl, package);

			// Add the metadata element
			var metadata = doc.CreateElement("metadata", opf_namespace);
			metadata.SetAttribute("xmlns:dc", dc_namespace);
			metadata.SetAttribute("xmlns:opf", opf_namespace);
			package.AppendChild(metadata);

			// Add the values to the metadata
			var dc_title = doc.CreateElement("dc", "title", dc_namespace);
			dc_title.InnerXml = appsettings.General.Name;
			metadata.AppendChild(dc_title);

			var dc_creator = doc.CreateElement("dc", "creator", dc_namespace);
			dc_creator.SetAttribute("role", opf_namespace, "aut");
			dc_creator.InnerXml = appsettings.General.Author;
			metadata.AppendChild(dc_creator);

			var dc_language = doc.CreateElement("dc", "language", dc_namespace);
			dc_language.InnerXml = appsettings.General.Language;
			metadata.AppendChild(dc_language);

			var dc_id = doc.CreateElement("dc", "identifier", dc_namespace);
			dc_id.SetAttribute("id", "BookId");
			dc_id.SetAttribute("scheme", opf_namespace, "UUID");
			dc_id.InnerXml = appsettings.EPUB.BookId.ToString("D");
			metadata.AppendChild(dc_id);

			var dc_date = doc.CreateElement("dc", "date", dc_namespace);
			dc_date.SetAttribute("event", opf_namespace, "publication");
			dc_date.InnerXml = DateTime.Now.ToString("yyyy-MM-dd");
			metadata.AppendChild(dc_date);

			// Now add the manifest
			var manifest = doc.CreateElement("manifest", opf_namespace);
			package.AppendChild(manifest);

			// Add the TOC to the manifest
			var toc = doc.CreateElement("item", opf_namespace);
			toc.SetAttribute("id", "ncx");
			toc.SetAttribute("href", "toc.ncx");
			toc.SetAttribute("media-type", "application/x-dtbncx+xml");
			manifest.AppendChild(toc);

			// Add the cover page to the manifest
			var coverpg = doc.CreateElement("item", opf_namespace);
			coverpg.SetAttribute("id", coverpage);
			coverpg.SetAttribute("href", "OEBPS/coverpage.html");
			coverpg.SetAttribute("media-type", "application/xhtml+xml");
			manifest.AppendChild(coverpg);

			// Now add all the documents to the manifest
			foreach (var document in Documents)
			{
				var item = doc.CreateElement("item", opf_namespace);
				item.SetAttribute("id", document.EpubID);
				item.SetAttribute("href", "OEBPS/" + document.FilenameHtml);
				item.SetAttribute("media-type", "application/xhtml+xml");
				manifest.AppendChild(item);
			}

			// Now add all the recipes to the manifest
			foreach (var recipe in Recipes)
			{
				var item = doc.CreateElement("item", opf_namespace);
				item.SetAttribute("id", recipe.EpubID);
				item.SetAttribute("href", "OEBPS/" + recipe.FilenameHtml);
				item.SetAttribute("media-type", "application/xhtml+xml");
				manifest.AppendChild(item);
			}

			// Add the index page to the manifest
			var indexpg = doc.CreateElement("item", opf_namespace);
			indexpg.SetAttribute("id", indexpage);
			indexpg.SetAttribute("href", "OEBPS/indexpage.html");
			indexpg.SetAttribute("media-type", "application/xhtml+xml");
			manifest.AppendChild(indexpg);

			// Add the spine
			var spine = doc.CreateElement("spine", opf_namespace);
			spine.SetAttribute("toc", "ncx");
			package.AppendChild(spine);

			// Add the cover page to the spine
			var cover = doc.CreateElement("itemref", opf_namespace);
			cover.SetAttribute("idref", coverpage);
			spine.AppendChild(cover);

			// Now add all the documents to the spine
			foreach (var document in Documents)
			{
				var item = doc.CreateElement("itemref", opf_namespace);
				item.SetAttribute("idref", document.EpubID);
				spine.AppendChild(item);
			}

			// Add all the recipes to the spine
			foreach (var recipe in Recipes)
			{
				var item = doc.CreateElement("itemref", opf_namespace);
				item.SetAttribute("idref", recipe.EpubID);
				spine.AppendChild(item);
			}

			// Add the index page to the spine
			var idx = doc.CreateElement("itemref", opf_namespace);
			idx.SetAttribute("idref", indexpage);
			spine.AppendChild(idx);

/*
			<guide>
				<reference type="cover" title="Cover Image" href="cover.xhtml" />
				<reference type="toc" title="Table of Contents" href="toc.xhtml" />
				<reference type="text" title="Startup Page" href="chapter1.xhtml" />
				<reference type="index" title="Index" href="index.xhtml" />
			</guide>
*/

			doc.Save(Path.Combine(dir, "content.opf"));
		}

		private void WriteIndexPage(string dir)
		{
			// It all starts with a document
			var doc = CreateXmlDocument(appsettings.General.Language, "Index page");
			var html = doc.SelectSingleNode("/html");

			// Add the body and fill it
			var body = doc.CreateElement("body");
			html.AppendChild(body);

			// Add the title at the top
			var title = doc.CreateElement("h1");
			title.InnerXml = "Index";
			body.AppendChild(title);

			foreach (var keyword in Keywords)
			{
				// First the keyword itself
				var p = doc.CreateElement("p");
				p.InnerXml = keyword.Name;
				body.AppendChild(p);

				// Then add a unorderd list with the recipes containing this keyword
				var list = doc.CreateElement("ul");
				body.AppendChild(list);

				// Go through the recipes
				foreach (var recipe in keyword.Recipes)
				{
					// Create a list item
					var li = doc.CreateElement("li");
					list.AppendChild(li);

					// Create an anchor and fill it with the name of the recipe and its filename
					// Note that the filename is relative to all the other pages, so they is no
					// need for the "OEBPS/" prefix
					var pub = doc.CreateElement("a");
					pub.InnerXml = recipe.Name;
					pub.SetAttribute("href", recipe.FilenameHtml);
					li.AppendChild(pub);
				}
			}

			// Save the document
			doc.Save(Path.Combine(dir, "indexpage.html"));
		}

		private void CreateEpub(string dir, string filename)
		{
			// First delete an existing file (just in case)
			File.Delete(filename);

			// Creating ZIP file and write the file "mimetype"
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			using (var zs = new ZipOutputStream(filename))
			{
				var o = zs.PutNextEntry("mimetype");
				o.CompressionLevel = CompressionLevel.None;

				byte[] mimetype = Encoding.ASCII.GetBytes("application/epub+zip");
				zs.Write(mimetype, 0, mimetype.Length);
			}

			// Adding all the generated files to the just created file
			using var zip = new ZipFile(filename);
			zip.AddDirectory(dir);
			zip.Save();
		}

		public override void Generate()
		{
			string baseDir = Path.Combine(Path.GetTempPath(), appsettings.EPUB.BookId.ToString("D"));
			Directory.CreateDirectory(baseDir);

			// Write container.xml in the META-INF directory
			var metaDir = Path.Combine(baseDir, "META-INF");
			Directory.CreateDirectory(metaDir);
			WriteContainerXML(metaDir);

			// Write the content file and the table of content
			WriteContentOpf(baseDir);
			WriteTOC(baseDir);

			// Write all the documents and recipes in the OEBPS directory
			var contentDir = Path.Combine(baseDir, "OEBPS");
			Directory.CreateDirectory(contentDir);

			WriteCoverPage(contentDir);
			WriteIndexPage(contentDir);

			foreach (var document in Documents)
				Write(contentDir, document: document);

			foreach (var recipe in Recipes)
				Write(contentDir, recipe: recipe);

			// Create the EPUB
			CreateEpub(baseDir, appsettings.EPUB.Filename);

#if !DEBUG
			Directory.Delete(baseDir, true);
#endif
		}
	}
}
