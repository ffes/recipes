using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Recepten.Models;

namespace Recepten
{
	internal class Program
	{
#if DEBUG
		private static readonly string environment = "Development";
#else
		private static readonly string environment = "Production";
#endif

		private static void Main(string[] args)
		{
			var config = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{environment}.json", optional: true)
				.Build();

			var appsettings = config.Get<AppSettings>();

			// Set the JSON options
			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};

			// Find all the JSON files in the input directory
			var recipes = new List<Recipe>();
			var files = Directory.EnumerateFiles(appsettings.Paths.Input, "*.json", SearchOption.AllDirectories);
			foreach (var filepath in files)
			{
				// Read the JSON and deserialize it
				var document = File.ReadAllText(filepath);
				var recipe = JsonSerializer.Deserialize<Recipe>(document, options);
				recipes.Add(recipe);

				// Get the filename from the path and create the html filename
				var fileinfo = new FileInfo(filepath);
				int len = fileinfo.Extension.Length;
				recipe.Filename = fileinfo.Name[0..^len] + ".html";

				// Generate the HTML output
				GenerateHtml.Generate(recipe, Path.Combine(appsettings.Paths.Output));

				// Copy the image file to the output directory
				if (!string.IsNullOrWhiteSpace(recipe.Image))
				{
					var from = Path.Combine(fileinfo.DirectoryName, recipe.Image);
					if (File.Exists(from))
					{
						var to = Path.Combine(appsettings.Paths.Output, recipe.Image);
						File.Copy(from, to, true);
					}
				}
			}

			// Generate the index.html
			GenerateHtml.GenerateIndex(recipes, Path.Combine(appsettings.Paths.Output, "index.html"));
		}
	}
}
