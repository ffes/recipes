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
	public class GenerateEpub: GenerateBase
	{
		public GenerateEpub(List<Recipe> recipes, List<Keyword> keywords, List<Document> documents): base(recipes, keywords, documents)
		{
		}

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

		private void AddRecipeToHtml(XmlDocument doc, XmlNode html, Recipe recipe)
		{
			// First add the body element
			var body = doc.CreateElement("body");
			html.AppendChild(body);

			// Add the name of the recipe
			var title = doc.CreateElement("h1");
			title.InnerXml = recipe.Name;
			body.AppendChild(title);

			// Add the description
			if (!string.IsNullOrEmpty(recipe.Description))
			{
				var descr = doc.CreateElement("p");
				descr.InnerXml = recipe.Description;
				body.AppendChild(descr);
			}

			// Add the author or publisher
			var by = doc.CreateElement("p");
			if (recipe.Author != null)
			{
				by.InnerXml = $"Gemaakt door {recipe.Author.Name}";
			}
			else if (!string.IsNullOrWhiteSpace(recipe.Publisher))
			{
				by.InnerXml= "Gepubliceerd door ";

				// Add a link to original URL
				if (!string.IsNullOrWhiteSpace(recipe.Url))
				{
					var pub = doc.CreateElement("a");
					pub.InnerXml = recipe.Publisher;
					pub.SetAttribute("href", recipe.Url);
					by.AppendChild(pub);
				}
				else
					by.InnerXml += recipe.Publisher;
			}

			// Add the publication date
			if (recipe.DatePublished != null && recipe.DatePublished.Year > 1900)
			{
				if (string.IsNullOrWhiteSpace(by.InnerXml))
					by.InnerXml = "Gepubliceerd ";

				by.InnerXml += " in " + recipe.DatePublished.ToString("MMMM yyyy", CultureInfo.GetCultureInfo(recipe.InLanguage));
			}
			body.AppendChild(by);

			// Add the total time
			if (recipe.TotalTime != null)
			{
				var ts = XmlConvert.ToTimeSpan(recipe.TotalTime);
				var time = doc.CreateElement("p");
				time.InnerText = $"Totale bereidingstijd: {ts.ToReadableString()}";
				body.AppendChild(time);
			}

			// Add the yields
			var yields = doc.CreateElement("p");
			yields.InnerText = $"Voor {recipe.RecipeYield.Value} {recipe.RecipeYield.UnitText}";
			body.AppendChild(yields);

			// Add the ingredients
			var ingr_title = doc.CreateElement("h2");
			ingr_title.InnerXml = "Ingrediënten";
			body.AppendChild(ingr_title);

			var ingredients = doc.CreateElement("ul");
			body.AppendChild(ingredients);

			foreach (var ingredient in recipe.RecipeIngredient)
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
			foreach (var instruction in recipe.RecipeInstructions)
			{
				var li = doc.CreateElement("li");
				li.InnerXml = instruction;
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

		private void Write(string dir, Recipe recipe = null, Document document = null)
		{
			if (recipe == null && document == null)
				return;

			// It all starts with a document
			var doc = CreateXmlDocument(appsettings.EPUB.Language, recipe?.Name ?? document.Name);
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
			var doc = CreateXmlDocument(appsettings.EPUB.Language, "Cover page");
			var html = doc.SelectSingleNode("/html");

			// Add the body and fill it
			var body = doc.CreateElement("body");
			html.AppendChild(body);

			// Add the name of the ebook
			var title = doc.CreateElement("h1");
			title.InnerXml = appsettings.EPUB.Name;
			body.AppendChild(title);

			// Add the author
			var author = doc.CreateElement("h2");
			author.InnerXml = appsettings.EPUB.Author;
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
			title.InnerXml = appsettings.EPUB.Name;
			docTitle.AppendChild(title);

			// The actual TOC starts with a navMap
			var navMap = doc.CreateElement("navMap");
			ncx.AppendChild(navMap);

			// First add the documents
			int i = 0;
			foreach (var document in Documents)
			{
				navMap.AppendChild(CreateTocNavPoint(doc, document.Id, ++i, document.Name, "OEBPS/" + document.FilenameHtml));
			}

			// Now add the recipes
			foreach (var recipe in Recipes)
			{
				navMap.AppendChild(CreateTocNavPoint(doc, recipe.Id, ++i, recipe.Name, "OEBPS/" + recipe.FilenameHtml));
			}

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
			dc_title.InnerXml = appsettings.EPUB.Name;
			metadata.AppendChild(dc_title);

			var dc_creator = doc.CreateElement("dc", "creator", dc_namespace);
			dc_creator.SetAttribute("role", opf_namespace, "aut");
			dc_creator.InnerXml = appsettings.EPUB.Author;
			metadata.AppendChild(dc_creator);

			var dc_language = doc.CreateElement("dc", "language", dc_namespace);
			dc_language.InnerXml = appsettings.EPUB.Language;
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
				item.SetAttribute("id", document.Id);
				item.SetAttribute("href", "OEBPS/" + document.FilenameHtml);
				item.SetAttribute("media-type", "application/xhtml+xml");
				manifest.AppendChild(item);
			}

			// Now add all the recipes to the manifest
			foreach (var recipe in Recipes)
			{
				var item = doc.CreateElement("item", opf_namespace);
				item.SetAttribute("id", recipe.Id);
				item.SetAttribute("href", "OEBPS/" + recipe.FilenameHtml);
				item.SetAttribute("media-type", "application/xhtml+xml");
				manifest.AppendChild(item);
			}

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
				item.SetAttribute("idref", document.Id);
				spine.AppendChild(item);
			}

			// Add all the recipes to the spine
			foreach (var recipe in Recipes)
			{
				var item = doc.CreateElement("itemref", opf_namespace);
				item.SetAttribute("idref", recipe.Id);
				spine.AppendChild(item);
			}

			doc.Save(Path.Combine(dir, "content.opf"));
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

			foreach (var document in Documents)
				Write(contentDir, document: document);

			foreach (var recipe in Recipes)
				Write(contentDir, recipe: recipe);

			// Create the EPUB
			CreateEpub(baseDir, appsettings.EPUB.Filename);

#if !DEBUG
			Directory.Delete(tempDir, true);
#endif
		}
	}
}
