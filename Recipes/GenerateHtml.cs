using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HandlebarsDotNet;
using HtmlAgilityPack;
using NLog;
using Recipes.Models;

namespace Recipes
{
	public class GenerateHtml: GenerateBase
	{
		public GenerateHtml(List<RecipeModel> recipes, List<Keyword> keywords, List<Document> documents): base(recipes, keywords, documents)
		{
		}

		public override bool Enabled => appsettings.Website.Enabled;

		protected static readonly Logger logger = Program.logger;

		/// <summary>
		/// Add the basic elements to the head.
		/// </summary>
		/// <param name="head"></param>
		private void AddBasicsToHead(HtmlNode head, string title)
		{
			// <meta charset="utf-8">
			var meta = head.AppendChild(HtmlNode.CreateNode("<meta>"));
			meta.Attributes.Add("charset", "utf-8");

			// <meta name="viewport" content="width=device-width, initial-scale=1">
			meta = head.AppendChild(HtmlNode.CreateNode("<meta>"));
			meta.Attributes.Add("name", "viewport");
			meta.Attributes.Add("content", "width=device-width, initial-scale=1");

			// <link href="styles.css" rel="stylesheet">
			var link = head.AppendChild(HtmlNode.CreateNode("<link>"));
			link.Attributes.Add("href", appsettings.Website.Stylesheet);
			link.Attributes.Add("rel", "stylesheet");

			// Add the title of the page
			head.AppendChild(HtmlNode.CreateNode($"<title>{title}</title>"));
		}

		private void AddTimeSpan(HtmlNode node, TimeSpan time, string title)
		{
			// Is the time set
			if (time.TotalMinutes == 0)
				return;

			// Do we need to add a break?
			if (node.HasChildNodes)
				node.AppendChild(HtmlNode.CreateNode("<br>"));

			// Add the time
			node.AppendChild(HtmlNode.CreateNode($"{title}: {time.ToReadableString()}"));
		}

		private void AddRecipeToBody(HtmlNode body, RecipeModel recipe)
		{
			// Add the title at the start of the page
			body.AppendChild(HtmlNode.CreateNode($"<h1>{recipe.Name}</h1>"));

			// Add the description
			if (!string.IsNullOrWhiteSpace(recipe.Description))
				body.AppendChild(HtmlNode.CreateNode($"<p>{recipe.Description}</p>"));

			if (!string.IsNullOrWhiteSpace(recipe.Image))
			{
				var img = body.AppendChild(HtmlNode.CreateNode("<img>"));
				img.Attributes.Add("src", recipe.Image);
				img.Attributes.Add("title", recipe.Name);
				img.Attributes.Add("alt", recipe.Name);
			}

			// Add the author or publisher
			var by = body.AppendChild(HtmlNode.CreateNode("<p></p>"));
			if (!string.IsNullOrWhiteSpace(recipe.Author))
			{
				by.InnerHtml = $"Gemaakt door {recipe.Author}";
			}
			else if (!string.IsNullOrWhiteSpace(recipe.Publisher))
			{
				by.InnerHtml = "Gepubliceerd door ";

				// Add a link to original URL
				if (recipe.PublishedURL != null)
				{
					var pub = HtmlNode.CreateNode($"<a>{recipe.Publisher}</a>");
					pub.Attributes.Add("href", recipe.PublishedURL.AbsoluteUri);
					by.InnerHtml += pub.OuterHtml;
				}
				else
					by.InnerHtml += recipe.Publisher;
			}

			// Add the publication date
			if (recipe.DatePublished.Year > 1900)
			{
				if (string.IsNullOrWhiteSpace(by.InnerHtml))
					by.InnerHtml = "Gepubliceerd ";

				by.InnerHtml += " in " + recipe.DatePublished.ToString("MMMM yyyy", CultureInfo.GetCultureInfo(recipe.InLanguage));
			}

			// Add the time(s)
			var time = HtmlNode.CreateNode("<p></p>");
			AddTimeSpan(time, recipe.PrepTime, "Voorbereidingstijd");
			AddTimeSpan(time, recipe.CookTime, "Bereidingstijd");
			AddTimeSpan(time, recipe.TotalTime, "Totale bereidingstijd");
			if (time.HasChildNodes)
				body.AppendChild(time);

			// Add the yields
			if (!string.IsNullOrWhiteSpace(recipe.Yield))
				body.AppendChild(HtmlNode.CreateNode($"<p>Voor {recipe.Yield}</p>"));

			// Add the ingredients
			body.AppendChild(HtmlNode.CreateNode($"<h2>Ingrediënten</h2>"));

			var ingredients = body.AppendChild(HtmlNode.CreateNode("<ul></ul>"));
			foreach (var ingredient in recipe.Ingredients)
			{
				ingredients.AppendChild(HtmlNode.CreateNode($"<li>{ingredient}</li>"));
			}

			// Add the instructions
			body.AppendChild(HtmlNode.CreateNode($"<h2>Bereidingswijze</h2>"));

			var instructions = body.AppendChild(HtmlNode.CreateNode("<ol></ol>"));
			foreach (var instruction in recipe.Instructions)
			{
				instructions.AppendChild(HtmlNode.CreateNode($"<li>{instruction}</li>"));
			}
		}

		private void Write(string dir, RecipeModel recipe = null, Document document = null)
		{
			if (recipe == null && document == null)
				return;

			// It all starts with a document
			var doc = new HtmlDocument();

			// Add the html5 doctype
			var doctype = doc.CreateComment("<!doctype html>");
			doc.DocumentNode.AppendChild(doctype);

			// Create html document
			var html = HtmlNode.CreateNode("<html></html>");
			doc.DocumentNode.AppendChild(html);

			// Add the head and fill it
			var head = html.AppendChild(HtmlNode.CreateNode("<head></head>"));
			AddBasicsToHead(head, recipe?.Name ?? document.Name);

			// Add the body and fill it
			var body = html.AppendChild(HtmlNode.CreateNode("<body></body>"));
			if (recipe != null)
				AddRecipeToBody(body, recipe);
			else
				body.InnerHtml = document.Html;

			// Save the document
			doc.Save(Path.Combine(dir, recipe?.FilenameHtml ?? document.FilenameHtml));
		}

		private static bool WriteRecipe(string templateFile, RecipeModel recipe, string outputFile)
		{
			string templateSource;
			try
			{
				// Read the template from the file
				templateSource = File.ReadAllText(templateFile);
			}
			catch (FileNotFoundException)
			{
				logger.Error($"File not found: {templateFile}");
				return false;
			}
			catch (Exception e)
			{
				logger.Error(e, "Exception reading: {templateFile}");
				return false;
			}

			if (string.IsNullOrWhiteSpace(templateSource))
			{
				logger.Error($"Unable to read: {templateFile}");
				return false;
			}

			// Combine the template and the data
			var template = Handlebars.Compile(templateSource);
			var result = template(recipe);

			// TODO: Add Exception handling
			logger.Debug($"OutputFile: {outputFile}");
			File.WriteAllText(outputFile, result);

			return true;
		}

		private void WriteKeywordsPage(string filename)
		{
			// It all starts with a document
			var doc = new HtmlDocument();

			// Add the html5 doctype
			var doctype = doc.CreateComment("<!doctype html>");
			doc.DocumentNode.AppendChild(doctype);

			// Create html document
			var html = HtmlNode.CreateNode("<html></html>");
			doc.DocumentNode.AppendChild(html);

			// Add the head and fill it
			var head = html.AppendChild(HtmlNode.CreateNode("<head></head>"));
			AddBasicsToHead(head, "Index");

			// Add the body
			var body = html.AppendChild(HtmlNode.CreateNode("<body></body>"));

			// Add the title at the top
			body.AppendChild(HtmlNode.CreateNode($"<h1>Index</h1>"));

			foreach (var keyword in Keywords)
			{
				var p = body.AppendChild(HtmlNode.CreateNode($"<p>{keyword.Name}</p>"));

				var list = body.AppendChild(HtmlNode.CreateNode("<ul></ul>"));
				foreach (var recipe in keyword.Recipes)
				{
					var li = list.AppendChild(HtmlNode.CreateNode("<li></li>"));
					var a = li.AppendChild(HtmlNode.CreateNode($"<a>{recipe.Name}</a>"));
					a.Attributes.Add("href", recipe.FilenameHtml);
				}
			}

			// Save the document
			doc.Save(filename);
		}

		private void WriteStartPage(string filename)
		{
			// It all starts with a document
			var doc = new HtmlDocument();

			// Add the html5 doctype
			var doctype = doc.CreateComment("<!doctype html>");
			doc.DocumentNode.AppendChild(doctype);

			// Create html document
			var html = HtmlNode.CreateNode("<html></html>");
			doc.DocumentNode.AppendChild(html);

			// Add the head and fill it
			var head = html.AppendChild(HtmlNode.CreateNode("<head></head>"));
			AddBasicsToHead(head, "Recepten");

			// Add the body
			var body = html.AppendChild(HtmlNode.CreateNode("<body></body>"));

			// Add the title at the top
			body.AppendChild(HtmlNode.CreateNode($"<h1>{appsettings.EPUB.Name}</h1>"));

			// First fill it a list of all the documents
			if (Documents.Count > 0)
			{
				body.AppendChild(HtmlNode.CreateNode("<h2>Algemeen</h2>"));
				var general = body.AppendChild(HtmlNode.CreateNode("<ul></ul>"));
				foreach (var document in Documents)
				{
					var li = general.AppendChild(HtmlNode.CreateNode("<li></li>"));
					var a = li.AppendChild(HtmlNode.CreateNode($"<a>{document.Name}</a>"));
					a.Attributes.Add("href", document.FilenameHtml);
				}
			}

			// And then fill it a list of all the recipes
			body.AppendChild(HtmlNode.CreateNode("<h2>Recepten</h2>"));
			var list = body.AppendChild(HtmlNode.CreateNode("<ul></ul>"));
			foreach (var recipe in Recipes)
			{
				var li = list.AppendChild(HtmlNode.CreateNode("<li></li>"));
				var a = li.AppendChild(HtmlNode.CreateNode($"<a>{recipe.Name}</a>"));
				a.Attributes.Add("href", recipe.FilenameHtml);
			}

			// Add a line to the index page (that is based on the keywords)
			if (Keywords.Count > 0)
			{
				var header = HtmlNode.CreateNode("<h2></h2>");
				var a = header.AppendChild(HtmlNode.CreateNode($"<a>Index</a>"));
				a.Attributes.Add("href", "keywords.html");
				body.AppendChild(header);
			}

			// Add the publication date
			body.AppendChild(HtmlNode.CreateNode($"<p>{DateTime.Now:d MMMM yyyy}</p>"));

			// Save the document
			doc.Save(filename);
		}

		public override void Generate()
		{
			// Generate the recipes pages
			foreach (var recipe in Recipes)
			{
				// Copy the image file to the output directory
				if (!string.IsNullOrWhiteSpace(recipe.Image))
				{
					var from = Path.Combine(recipe.SourceFile.DirectoryName, recipe.Image);
					if (File.Exists(from))
					{
						var to = Path.Combine(appsettings.Website.Output, recipe.Image);
						File.Copy(from, to, true);
					}
					else
					{
						logger.Warn($"Image '{recipe.Image}' for recipe '{recipe.Name}' not found!");
						recipe.Image = null;
					}
				}

				// Generate the HTML output
				WriteRecipe(appsettings.Website.Templates.Recipes,
					recipe,
					Path.Combine(appsettings.Website.Output, recipe.FilenameHtml));
			}

			// Generate the markdown document pages
			foreach (var document in Documents)
			{
				Write(appsettings.Website.Output, document: document);
			}

			// Generate the index page based on the keywords
			WriteKeywordsPage(Path.Combine(appsettings.Website.Output, "keywords.html"));

			// Generate the index.html
			WriteStartPage(Path.Combine(appsettings.Website.Output, "index.html"));

			// Copy the stylesheet
			File.Copy(Path.Combine(appsettings.InputPaths.WebFiles, appsettings.Website.Stylesheet), Path.Combine(appsettings.Website.Output, appsettings.Website.Stylesheet), true);
		}
	}
}
