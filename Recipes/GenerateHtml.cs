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
	internal class TemplateModel
	{
		public string Title { get; set; }
		public object Content { get; set; }
	}

	public class GenerateHtml(List<RecipeModel> recipes, List<Keyword> keywords, List<Document> documents) : GenerateBase(recipes, keywords, documents)
	{
		public override bool Enabled => appsettings.Website.Enabled;
		private readonly string keywordsFilename = "keywords.html";

		protected static readonly Logger logger = Program.logger;

		public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
		{
			try
			{
				// Check if the target directory exists
				if (!Directory.Exists(target.FullName))
				{
					Directory.CreateDirectory(target.FullName);
				}

				// Copy all the files into the new directory
				foreach (var file in source.GetFiles())
				{
					file.CopyTo(Path.Combine(target.ToString(), file.Name), true);
				}

				// Copy all the sub directories using recursion
				foreach (var dir in source.GetDirectories())
				{
					var nextTargetDir = target.CreateSubdirectory(dir.Name);
					CopyAll(dir, nextTargetDir);
				}
			}
			catch (IOException e)
			{
				logger.Error(e, $"Exception copying {source.FullName} to {target.FullName}");
			}
		}

		private static bool ReadContentFromFile(string filename, out string content)
		{
			content = "";
			try
			{
				// Read the template from the file
				content = File.ReadAllText(filename);
			}
			catch (FileNotFoundException)
			{
				logger.Error($"File not found: {filename}");
				return false;
			}
			catch (Exception e)
			{
				logger.Error(e, $"Exception reading: {filename}");
				return false;
			}

			if (string.IsNullOrWhiteSpace(content))
			{
				logger.Error($"Unable to read: {filename}");
				return false;
			}

			return true;
		}

		private void WriteRecipes()
		{
			if (!ReadContentFromFile(appsettings.Website.Templates.Base, out string baseTemplate))
				return;

			if (!ReadContentFromFile(appsettings.Website.Templates.Partials.Recipes, out string partialTemplate))
				return;

			// Link the partial to the content and combine it with the template
			Handlebars.RegisterTemplate("content", partialTemplate);
			var template = Handlebars.Compile(baseTemplate);

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

				var data = new TemplateModel
				{
					Title = recipe.Name,
					Content = recipe
				};
				var result = template(data);

				// TODO: Add Exception handling
				Directory.CreateDirectory(appsettings.Website.Output);
				string outputFile = Path.Combine(appsettings.Website.Output, recipe.FilenameHtml);
				logger.Debug($"OutputFile: {outputFile}");
				File.WriteAllText(outputFile, result);
			}
		}

		private void WriteDocuments()
		{
			if (!ReadContentFromFile(appsettings.Website.Templates.Base, out string baseTemplate))
				return;

			foreach (var document in Documents)
			{
				var data = new TemplateModel
				{
					Title = document.Name,
					Content = document.Html
				};

				// Combine the template and the data
				Handlebars.RegisterTemplate("content", document.Html);
				var template = Handlebars.Compile(baseTemplate);
				var result = template(data);

				// TODO: Add Exception handling
				Directory.CreateDirectory(appsettings.Website.Output);
				var outputFile = Path.Combine(appsettings.Website.Output, document.FilenameHtml);
				logger.Debug($"OutputFile: {outputFile}");
				File.WriteAllText(outputFile, result);
			}
		}

		private void WriteKeywords(string title, string filename)
		{
			// First read the template
			if (!ReadContentFromFile(appsettings.Website.Templates.Base, out string baseTemplate))
				return;

			// Now starts a HTML document to hold the content
			var doc = new HtmlDocument();

			// Start with the title
			doc.DocumentNode.AppendChild(HtmlNode.CreateNode($"<h1>{title}</h1>"));

			// Now add for all keywords and the recipes that have to this keyword
			foreach (var keyword in Keywords)
			{
				// First add the keyword
				doc.DocumentNode.AppendChild(HtmlNode.CreateNode($"<h2>{keyword.Name}</h2>"));

				// Now add links to the recipes
				var list = doc.DocumentNode.AppendChild(HtmlNode.CreateNode("<ul></ul>"));
				foreach (var recipe in keyword.Recipes)
				{
					var li = list.AppendChild(HtmlNode.CreateNode("<li></li>"));
					var a = li.AppendChild(HtmlNode.CreateNode($"<a>{recipe.Name}</a>"));
					a.Attributes.Add("href", recipe.FilenameHtml);
				}
			}

			// Combine the template and the data
			Handlebars.RegisterTemplate("content", doc.DocumentNode.OuterHtml);
			var template = Handlebars.Compile(baseTemplate);
			var result = template(new { title });

			// TODO: Add Exception handling
			var outputFile = Path.Combine(appsettings.Website.Output, filename);
			logger.Debug($"OutputFile: {outputFile}");
			File.WriteAllText(outputFile, result);
		}

		private void WriteStartPage(string filename)
		{
			// It all starts with a document
			var doc = new HtmlDocument();

			// Add the title at the top
			doc.DocumentNode.AppendChild(HtmlNode.CreateNode($"<h1>{appsettings.General.Name}</h1>"));
			doc.DocumentNode.AppendChild(HtmlNode.CreateNode($"<p>{appsettings.General.Author}</p>"));

			// First fill it with a list of all the documents
			if (Documents.Count > 0)
			{
				doc.DocumentNode.AppendChild(HtmlNode.CreateNode("<h2>Algemeen</h2>"));
				var general = doc.DocumentNode.AppendChild(HtmlNode.CreateNode("<ul></ul>"));
				foreach (var document in Documents)
				{
					var li = general.AppendChild(HtmlNode.CreateNode("<li></li>"));
					var a = li.AppendChild(HtmlNode.CreateNode($"<a>{document.Name}</a>"));
					a.Attributes.Add("href", document.FilenameHtml);
				}
			}

			// And then fill with it a list of all the recipes
			doc.DocumentNode.AppendChild(HtmlNode.CreateNode("<h2>Recepten</h2>"));
			var list = doc.DocumentNode.AppendChild(HtmlNode.CreateNode("<ul></ul>"));
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
				a.Attributes.Add("href", keywordsFilename);
				doc.DocumentNode.AppendChild(header);
			}

			// Add the publication date
			var culture = new CultureInfo(appsettings.General.Language);
			var now = DateTime.Now.ToString("d MMMM yyyy", culture);
			doc.DocumentNode.AppendChild(HtmlNode.CreateNode($"<small>{now}</small>"));

			// First read the template
			if (!ReadContentFromFile(appsettings.Website.Templates.Base, out string baseTemplate))
				return;

			// Combine the template and the data
			Handlebars.RegisterTemplate("content", doc.DocumentNode.OuterHtml);
			var template = Handlebars.Compile(baseTemplate);
			var result = template(new { title = appsettings.General.Name });

			// TODO: Add Exception handling
			var outputFile = Path.Combine(appsettings.Website.Output, filename);
			logger.Debug($"OutputFile: {outputFile}");
			File.WriteAllText(outputFile, result);
		}

		public override void Generate()
		{
			WriteRecipes();
			WriteDocuments();
			WriteKeywords("Index", keywordsFilename);
			WriteStartPage("index.html");
			CopyAll(new DirectoryInfo(appsettings.Website.WebFiles), new DirectoryInfo(appsettings.Website.Output));
		}
	}
}
