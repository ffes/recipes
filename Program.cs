using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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

		private static void Main(string[] args)
		{
			// First get all the recipes
			var recipes = GetRecipes();

			// Generate all the outputs
			var html = new GenerateHtml();
			html.Generate(recipes);

			var epub = new GenerateEpub();
			epub.Generate(recipes);
		}
	}
}
