using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using HtmlAgilityPack;
using Recipes.Models;

namespace Recipes
{
	public class GenerateHtml
	{
		/// <summary>
		/// Add the basic elements to the head.
		/// </summary>
		/// <param name="head"></param>
		private static void AddBasicsToHead(HtmlNode head, string title)
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
			link.Attributes.Add("href", "styles.css");
			link.Attributes.Add("rel", "stylesheet");

			// Add the title of the page
			head.AppendChild(HtmlNode.CreateNode($"<title>{title}</title>"));
		}

		private static void AddRecipeToBody(HtmlNode body, Recipe recipe)
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

			// Add the total time
			if (recipe.TotalTime != null)
			{
				var ts = XmlConvert.ToTimeSpan(recipe.TotalTime);
				body.AppendChild(HtmlNode.CreateNode($"<p>Totale bereidingstijd {ts.ToReadableString()}</p>"));
			}

			// Add the yields
			body.AppendChild(HtmlNode.CreateNode($"<p>Voor {recipe.RecipeYield.Value} {recipe.RecipeYield.UnitText}</p>"));

			// Add the ingredients
			body.AppendChild(HtmlNode.CreateNode($"<h2>Ingrediënten</h2>"));

			var ingredients = body.AppendChild(HtmlNode.CreateNode("<ul></ul>"));
			foreach (var ingredient in recipe.RecipeIngredient)
			{
				ingredients.AppendChild(HtmlNode.CreateNode($"<li>{ingredient}</li>"));
			}

			// Add the instructions
			body.AppendChild(HtmlNode.CreateNode($"<h2>Bereidingswijze</h2>"));

			var instructions = body.AppendChild(HtmlNode.CreateNode("<ol></ol>"));
			foreach (var instruction in recipe.RecipeInstructions)
			{
				instructions.AppendChild(HtmlNode.CreateNode($"<li>{instruction}</li>"));
			}
		}

		private static void WriteRecipe(Recipe recipe, string path)
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
			AddBasicsToHead(head, recipe.Name);

			// Add the body and fill it
			var body = html.AppendChild(HtmlNode.CreateNode("<body></body>"));
			AddRecipeToBody(body, recipe);

			// Save the document
			doc.Save(Path.Combine(path, recipe.FilenameHtml));
		}

		private static void WriteIndex(List<Recipe> recipes, string path)
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

			// And fill it a simple list of all the recipes
			var list = body.AppendChild(HtmlNode.CreateNode("<ul></ul>"));
			foreach (var recipe in recipes)
			{
				var li = list.AppendChild(HtmlNode.CreateNode("<li></li>"));
				var a = li.AppendChild(HtmlNode.CreateNode($"<a>{recipe.Name}</a>"));
				a.Attributes.Add("href", recipe.FilenameHtml);
			}

			// Add the publication date
			body.AppendChild(HtmlNode.CreateNode($"<p>{DateTime.Now:d MMMM yyyy}</p>"));

			// Save the document
			doc.Save(path);
		}

		public static void Generate(List<Recipe> recipes, string path)
		{
			// Generate the Basic HTML website
			foreach (var recipe in recipes)
			{
				// Generate the HTML output
				WriteRecipe(recipe, path);

				// Copy the image file to the output directory
				if (!string.IsNullOrWhiteSpace(recipe.Image))
				{
					var from = Path.Combine(recipe.SourceFile.DirectoryName, recipe.Image);
					if (File.Exists(from))
					{
						var to = Path.Combine(path, recipe.Image);
						File.Copy(from, to, true);
					}
				}
			}

			// Generate the index.html
			WriteIndex(recipes, Path.Combine(path, "index.html"));
		}
	}
}
