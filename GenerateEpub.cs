using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Ionic.Zip;
using Ionic.Zlib;
using Microsoft.Extensions.Configuration;
using Recipes.Models;

namespace Recipes
{
	public class GenerateEpub
	{
		private static void AddBasicsToHead(XmlDocument doc, XmlElement head, string title)
		{
			var meta = doc.CreateElement("meta");
			meta.SetAttribute("content", "http://www.w3.org/1999/xhtml; charset=utf-8");
			meta.SetAttribute("http-equiv", "Content-Type");
			head.AppendChild(meta);

			var ttl = doc.CreateElement("title");
			ttl.InnerXml = title;
			head.AppendChild(ttl);
		}

		private static void AddRecipeToBody(XmlDocument doc, XmlElement body, Recipe recipe)
		{
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
			yields.InnerText = $"voor: {recipe.RecipeYield.Value} {recipe.RecipeYield.UnitText}";
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

		private static void WriteRecipe(Recipe recipe, string dir)
		{
			// It all starts with a document
			var doc = new XmlDocument();

			// Add the html root element
			var appsettings = Program.config.Get<AppSettings>();
			var html = doc.CreateElement("html");
			html.SetAttribute("xmlns", "http://www.w3.org/1999/xhtml");
			html.SetAttribute("xml:lang", appsettings.EPUB.Language);
			html.SetAttribute("lang", appsettings.EPUB.Language);
			doc.AppendChild(html);

			// Create an XML declaration
			var xmldecl = doc.CreateXmlDeclaration("1.0", null, null);
			doc.InsertBefore(xmldecl, html);

			// Create the head and add it to the html
			var head = doc.CreateElement("head");

			html.AppendChild(head);
			AddBasicsToHead(doc, head, recipe.Name);

			// Add the body and fill it
			var body = doc.CreateElement("body");
			html.AppendChild(body);
			AddRecipeToBody(doc, body, recipe);

			// Save the document
			doc.Save(Path.Combine(dir, recipe.FilenameHtml));
		}

		private static void WriteContainerXML(string dir)
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

		private static void WriteTOC(List<Recipe> recipes, string dir)
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
			var appsettings = Program.config.Get<AppSettings>();
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

			// Go through the recipes and and them to the actual table of content
			var navMap = doc.CreateElement("navMap");
			ncx.AppendChild(navMap);

			int i = 0;
			foreach (var recipe in recipes)
			{
				i++;
				var navPoint = doc.CreateElement("navPoint");
				navPoint.SetAttribute("id", recipe.Id);
				navPoint.SetAttribute("playOrder", i.ToString());
				navMap.AppendChild(navPoint);

				var navLabel = doc.CreateElement("navLabel");
				var navLabelText = doc.CreateElement("text");
				navLabelText.InnerXml = recipe.Name;
				navLabel.AppendChild(navLabelText);
				navPoint.AppendChild(navLabel);

				var content = doc.CreateElement("content");
				content.SetAttribute("src", "OEBPS/" + recipe.FilenameHtml);    // Not using Path.Combine because it always needs to be '/'
				navPoint.AppendChild(content);
			}

			doc.Save(Path.Combine(dir, "toc.ncx"));
		}

		private static void WriteContentOpf(List<Recipe> recipes, string dir)
		{
			// It all starts with a document
			var doc = new XmlDocument();

			// Define the namespace URLs
			const string opf_namespace = "http://www.idpf.org/2007/opf";
			const string dc_namespace = "http://purl.org/dc/elements/1.1/";

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
			var appsettings = Program.config.Get<AppSettings>();

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

			// Now add all the recipes to the manifest
			foreach (var recipe in recipes)
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

			foreach (var recipe in recipes)
			{
				var item = doc.CreateElement("itemref", opf_namespace);
				item.SetAttribute("idref", recipe.Id);
				spine.AppendChild(item);
			}

			doc.Save(Path.Combine(dir, "content.opf"));
		}

		private static void CreateEpub(string dir, string filename)
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

		public static void Generate(List<Recipe> recipes)
		{
			var appsettings = Program.config.Get<AppSettings>();

			string baseDir = Path.Combine(Path.GetTempPath(), appsettings.EPUB.BookId.ToString("D"));
			Directory.CreateDirectory(baseDir);

			// Write container.xml in the META-INF directory
			var metaDir = Path.Combine(baseDir, "META-INF");
			Directory.CreateDirectory(metaDir);
			WriteContainerXML(metaDir);

			// Write the content file and the table of content
			WriteContentOpf(recipes, baseDir);
			WriteTOC(recipes, baseDir);

			// Write all the recipes in the OEBPS directory
			var contentDir = Path.Combine(baseDir, "OEBPS");
			Directory.CreateDirectory(contentDir);

			foreach (var recipe in recipes)
				WriteRecipe(recipe, contentDir);

			// Create the EPUB
			CreateEpub(baseDir, appsettings.EPUB.Filename);

#if !DEBUG
			Directory.Delete(tempDir, true);
#endif
		}
	}
}
