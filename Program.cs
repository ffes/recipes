using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Configuration;
using Recipes.Models;

namespace Recipes
{
	internal class Program
	{
		public static readonly IConfigurationRoot config = null;

#if DEBUG
		private static readonly string environment = "Development";
#else
		private static readonly string environment = "Production";
#endif

		static Program()
		{
			config = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{environment}.json", optional: true)
				.Build();
		}

		private static List<Recipe> GetRecipes()
		{
			var appsettings = config.Get<AppSettings>();

			// Set the JSON options
			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};

			// Find all the JSON files in the input directory
			var recipes = new List<Recipe>();
			var files = Directory.EnumerateFiles(appsettings.InputPath, "*.json", SearchOption.AllDirectories);
			foreach (var filepath in files)
			{
				// Read the JSON and deserialize it
				var document = File.ReadAllText(filepath);

				Recipe recipe = null;
				try
				{
					recipe = JsonSerializer.Deserialize<Recipe>(document, options);
				}
				catch (JsonException ex)
				{
					Console.WriteLine($"Error parsing {filepath}: {ex}");
				}

				if (recipe != null)
				{
					recipes.Add(recipe);

					// Get the filename from the path and create the html filename
					recipe.SourceFile = new FileInfo(filepath);
					int len = recipe.SourceFile.Extension.Length;
					recipe.FilenameHtml = recipe.SourceFile.Name[0..^len] + ".html";

					recipe.Id = $"recipe{recipes.Count}";
				}
			}

			// Sort the recipes using Recipe.CompareTo()
			recipes.Sort();

			return recipes;
		}

		private static List<Document> GetDocuments()
		{
			// To store all the documents
			var documents = new List<Document>();

			// Go through the InputPath and see if there are any Markdown documents there
			var appsettings = config.Get<AppSettings>();
			var files = Directory.EnumerateFiles(appsettings.InputPath, "*.md", SearchOption.AllDirectories);
			foreach (var filepath in files)
			{
				var doc = new Document
				{
					SourceFile = new FileInfo(filepath)
				};

				// If it doesn't start with a 0 is is not part of the texts we want to include
				if (!doc.SourceFile.Name.StartsWith("0"))
					continue;

				var pipeline = new MarkdownPipelineBuilder()
							.UsePipeTables()
							.Build();

				// Read the Markdown
				var markdown = File.ReadAllText(filepath);

				// Convert to a MarkdownDocument
				var mddoc = Markdown.Parse(markdown, pipeline);

				// Do smart things with the markdown, like getting the name from the first H1
				foreach (var block in mddoc.ToList())
				{
					if (block is HeadingBlock headingBlock)
					{
						if (headingBlock.Level == 1)
						{
							if (headingBlock.Inline.FirstChild != null)
							{
								if (headingBlock.Inline.FirstChild is LiteralInline literalInline)
								{
									doc.Name = literalInline.Content.ToString();
								}
							}
							break;
						}
					}
				}

				// If there is no H1 found, use the filename as the name...
				if (string.IsNullOrWhiteSpace(doc.Name))
				{
					doc.Name = doc.SourceFile.Name;
				}

				// Convert the MarkdownDocument to a HTML string
				var writer = new StringWriter();
				var renderer = new HtmlRenderer(writer);
				pipeline.Setup(renderer);
				renderer.Render(mddoc);
				writer.Flush();

				// And store it
				doc.Html = writer.ToString();

				// Create the HTML filename
				int len = doc.SourceFile.Extension.Length;
				doc.FilenameHtml = doc.SourceFile.Name[0..^len] + ".html";

				// Add the document to the list
				documents.Add(doc);
			}

			return documents;
		}

		private static void Main(string[] args)
		{
			// First get all the recipes
			var recipes = GetRecipes();

			if (recipes == null)
				return;

			// Get all the documents
			var docs = GetDocuments();

			// Generate all the outputs
			var html = new GenerateHtml(recipes);
			html.Generate();

			var epub = new GenerateEpub(recipes);
			epub.Generate();
		}
	}
}
