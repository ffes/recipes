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
using NLog;
using Recipes.Models;
using Schema.NET;

namespace Recipes
{
	public class Program
	{
		public static readonly IConfigurationRoot config = null;

#if DEBUG
		private static readonly string environment = "Development";
#else
		private static readonly string environment = "Production";
#endif

		// NLog
		public static readonly Logger logger = LogManager.GetCurrentClassLogger();

		static Program()
		{
			config = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{environment}.json", optional: true)
				.Build();
		}

		public static RecipeModel FromRecipe(Recipe recipe)
		{
			var newModel = new RecipeModel
			{
				Name = recipe.Name,
				Description = recipe.Description,
				InLanguage = recipe.InLanguage
			};

			// Is there an author
			if (recipe.Author.HasValue)
			{
				var (org, person) = recipe.Author;
				newModel.Author = person.Any() ? person.First().Name : org.First().Name;
			}

			// Is there a publisher
			if (recipe.Publisher.HasValue)
			{
				var (org, person) = recipe.Publisher;
				newModel.Publisher = (org.Any() ? org.First().Name : person.First().Name);
			}

			// Add URL where this recipe was originally published
			if (recipe.Url.Count > 0)
			{
				newModel.PublishedURL = recipe.Url.First();
			}

			// Add the date
			if (recipe.DatePublished.HasValue)
			{
				var (_, date, _) = recipe.DatePublished;

				if (date.Any())
					newModel.DatePublished = date.First() ?? new DateTime();
			}

			// Add the category
			if (recipe.RecipeCategory.Count > 0)
			{
				newModel.Category = recipe.RecipeCategory.First();
			}

			// Add the cuisine
			if (recipe.RecipeCuisine.Count > 0)
			{
				newModel.Cuisine = recipe.RecipeCuisine.First();
			}

			// Add the cooking method
			if (recipe.CookingMethod.Count > 0)
			{
				newModel.CookingMethod = recipe.CookingMethod.First();
			}

			// Add the preparation time
			if (recipe.PrepTime.Count > 0)
			{
				newModel.PrepTime = recipe.PrepTime.First() ?? new TimeSpan();
			}

			// Add the cook time
			if (recipe.CookTime.Count > 0)
			{
				newModel.CookTime = recipe.CookTime.First() ?? new TimeSpan();
			}

			// Add the total time
			if (recipe.TotalTime.Count > 0)
			{
				newModel.TotalTime = recipe.TotalTime.First() ?? new TimeSpan();
			}

			// Add the Yield
			if (recipe.RecipeYield.HasValue)
			{
				var (quantity, str) = recipe.RecipeYield;
				if (quantity.Any())
				{
					foreach (var q in quantity)
					{
						var (_, d, _, _) = q.Value;
						newModel.Yield += $"{d.First()} {q.UnitText.First()}";
					}
				}
				else
					newModel.Yield = string.Join(", ", str.ToArray());
			}

			// Add the ingredients
			foreach (var ingredient in recipe.RecipeIngredient)
			{
				if (newModel.Ingredients == null)
					newModel.Ingredients = new List<string>();

				newModel.Ingredients.Add(ingredient);
			}

			// Add the instructions
			foreach (var instruction in recipe.RecipeInstructions)
			{
				if (newModel.Instructions == null)
					newModel.Instructions = new List<string>();

				newModel.Instructions.Add(instruction.ToString());
			}

			// Add the keywords
			if (recipe.Keywords.Count > 0)
			{
				var (str, uris) = recipe.Keywords;

				foreach (var keywrds in str)
				{
					foreach (var w in keywrds.Split(","))
					{
						newModel.Keywords ??= new List<string>();
						newModel.Keywords.Add(w.Trim().ToLower());
					}
				}

				foreach (var keywrds in uris)
				{
					string kws = keywrds.ToString();
					if (!string.IsNullOrWhiteSpace(kws))
					{
						foreach (var w in kws.Split(","))
						{
							newModel.Keywords ??= new List<string>();
							newModel.Keywords.Add(w.Trim().ToLower());
						}
					}
				}
			}

			// Add the image
			if (recipe.Image.HasValue)
			{
				var (_, uris) = recipe.Image;
				newModel.Image = (uris.Any() ? uris.First().ToString() : null);
			}

			return newModel;
		}

		private static List<RecipeModel> GetRecipes()
		{
			var appsettings = config.Get<AppSettings>();

			// Find all the JSON files in the input directory
			logger.Info($"Reading recipes from {appsettings.InputPaths.Recipes}");
			var recipes = new List<RecipeModel>();
			var files = Directory.EnumerateFiles(appsettings.InputPaths.Recipes, "*.json", SearchOption.AllDirectories);
			foreach (var filepath in files)
			{
				// Read the JSON and deserialize it
				var document = File.ReadAllText(filepath);

				Recipe recipe = null;
				try
				{
					recipe = SchemaSerializer.DeserializeObject<Recipe>(document);
				}
				catch (JsonReaderException ex)
				{
					logger.Error(ex, $"Error parsing {filepath}");
				}

				if (recipe == null)
					continue;

				if (recipe.Name.Count == 0)
					continue;

				var r = FromRecipe(recipe);
				recipes.Add(r);

				// Get the filename from the path and create the html filename
				r.SourceFile = new FileInfo(filepath);
				int len = r.SourceFile.Extension.Length;
				r.FilenameHtml = r.SourceFile.Name[0..^len] + ".html";

				r.EpubID = $"recipe{recipes.Count}";
			}

			// Sort the recipes using Recipe.CompareTo()
			recipes.Sort();

			return recipes;
		}

		private static List<Keyword> GetKeywordsFromRecipes(List<RecipeModel> recipes)
		{
			var keywords = new HashSet<Keyword>();

			foreach (var recipe in recipes)
			{
				var words = new HashSet<string>();

				// Add the category
				if (!string.IsNullOrWhiteSpace(recipe.Category))
					words.Add(recipe.Category.ToLower());

				// Add the cuisine
				if (!string.IsNullOrWhiteSpace(recipe.Cuisine))
					words.Add(recipe.Cuisine.ToLower());

				// Add the cooking method
				if (!string.IsNullOrWhiteSpace(recipe.CookingMethod))
					words.Add(recipe.CookingMethod.ToLower());

				// Go through the keywords
				if (recipe.Keywords != null)
				{
					foreach (var keyword in recipe.Keywords)
					{
						words.Add(keyword);
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
							keyword.Recipes = new List<RecipeModel>();

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
			var files = Directory.EnumerateFiles(appsettings.InputPaths.Documents, "*.md", SearchOption.AllDirectories);
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

		private static void MainWorker()
		{
			// First get all the recipes
			var recipes = GetRecipes();

			// Did we find any recipes?
			if (recipes.Count == 0)
				return;

			// Get all the keywords from the recipes
			var keywords = GetKeywordsFromRecipes(recipes);

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

		private static void Main(string[] args)
		{
			try
			{
				MainWorker();
			}
			catch (Exception e)
			{
				logger.Error(e, "Fatal uncatched exception in Main()");
			}
			finally
			{
				LogManager.Shutdown();
			}
		}
	}
}
