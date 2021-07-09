using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Recipes.Models;
using Schema.NET;

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

		private static List<MyRecipe> GetRecipes()
		{
			var appsettings = config.Get<AppSettings>();

			// Find all the JSON files in the input directory
			var recipes = new List<MyRecipe>();
			var files = Directory.EnumerateFiles(appsettings.InputPath, "*.json", SearchOption.AllDirectories);
			foreach (var filepath in files)
			{
				// Read the JSON and deserialize it
				var document = File.ReadAllText(filepath);

				MyRecipe recipe = null;
				try
				{
					recipe = SchemaSerializer.DeserializeObject<MyRecipe>(document);
				}
				catch (JsonReaderException ex)
				{
					Console.WriteLine($"Error parsing {filepath}: {ex.Message}");
				}

				if (recipe == null)
					continue;

				if (recipe.Name.Count == 0)
					continue;

				recipes.Add(recipe);

				// Get the filename from the path and create the html filename
				recipe.SourceFile = new FileInfo(filepath);
				int len = recipe.SourceFile.Extension.Length;
				recipe.FilenameHtml = recipe.SourceFile.Name[0..^len] + ".html";

				recipe.EpubID = $"recipe{recipes.Count}";
			}

			// Sort the recipes using Recipe.CompareTo()
			recipes.Sort();

			return recipes;
		}

		private static List<Keyword> GetKeywordsFromRecipes(List<MyRecipe> recipes)
		{
			var keywords = new HashSet<Keyword>();

			foreach (var recipe in recipes)
			{
				var words = new HashSet<string>();

				// Add the category
				foreach (var cat in recipe.RecipeCategory)
					words.Add(cat.Trim().ToLower());

				// Add the cuisine
				foreach (var cuisine in recipe.RecipeCuisine)
					words.Add(cuisine.Trim().ToLower());

				// Go through the keywords
				if (recipe.Keywords.Count > 0)
				{
					var (str, _) = recipe.Keywords;

					foreach (var keywrds in str)
					{
						foreach (var w in keywrds.Split(","))
						{
							words.Add(w.Trim().ToLower());
						}
					}
				}

				// Go through the collected words
				foreach (var word in words)
				{
					// Add it to the hashset
					var kw = new Keyword {
						Name = word
					};
					keywords.Add(kw);

					// Get it back from the hashset
					if (keywords.TryGetValue(kw, out Keyword keyword))
					{
						// Add the recipe to hashset of recipes in this keyword
						if (keyword.Recipes == null)
							keyword.Recipes = new List<MyRecipe>();

						if (!keyword.Recipes.Contains(recipe))
							keyword.Recipes.Add(recipe);
					}
				}
			}

			// Convert the HashSet<> to a List<> and sort it
			var list = keywords.ToList();
			list.Sort();

			return list;
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

				// And add the ID
				doc.EpubID = $"doc{documents.Count}";
			}

			return documents;
		}

		private static void Main(string[] args)
		{
			// First get all the recipes
			var recipes = GetRecipes();

			// Did we find any recipes?
			if (recipes.Count == 0)
				return;

			// Get all the keywords from the recipes
			var keywords = GetKeywordsFromRecipes(recipes);

			// For now, just print the result
			foreach (var kw in keywords)
			{
				Console.WriteLine(kw.Name);
				foreach (var recipe in kw.Recipes)
				{
					Console.WriteLine($"  {recipe.Name.First()}");
				}
			}

			// Get all the markdown documents
			var docs = GetDocuments();

			// Generate all the outputs
			var html = new GenerateHtml(recipes, keywords, docs);
			if (html.Enabled)
				html.Generate();

			var epub = new GenerateEpub(recipes, keywords, docs);
			if (epub.Enabled)
				epub.Generate();
		}
	}
}
