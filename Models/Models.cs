using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json.Serialization;

namespace Recipes.Models
{
	public class TypeOfGood
	{
		[JsonPropertyName("@type")]
		public string Type { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
	}

	public class RecipeIngredient
	{
		[JsonPropertyName("@type")]
		public string Type { get; set; }
		public int AmountOfThisGood { get; set; }
		public string UnitText { get; set; }
		public TypeOfGood TypeOfGood { get; set; }
	}

	public class Author
	{
		[JsonPropertyName("@type")]
		public string Type { get; set; }
		public string Name { get; set; }
	}

	public class RecipeYield
	{
		public int Value { get; set; }
		public string UnitText { get; set; }
	}

	public class Recipe: IComparable<Recipe>
	{
		[JsonPropertyName("@context")]
		public string Context { get; set; }
		[JsonPropertyName("@type")]
		public string Type { get; set; }
		public string Id { get; set; }
		public string Name { get; set; }
		public FileInfo SourceFile { get; set; }
		public string FilenameHtml { get; set; }
		public string Description { get; set; }
		public string InLanguage { get; set; }
		public string Image { get; set; }
		public string Publisher { get; set; }
		public string Url { get; set; }
		public DateTime DatePublished { get; set; }
		public Author Author { get; set; }
		public string TotalTime { get; set; }
		public string PrepTime { get; set; }
		public string CookTime { get; set; }
		public string Keywords { get; set; }
		public RecipeYield RecipeYield { get; set; }
		public string RecipeCategory { get; set; }
		public string RecipeCuisine { get; set; }
		public List<string> RecipeIngredient { get; set; }
		//public List<RecipeIngredient> RecipeIngredient { get; set; }
		public List<string> RecipeInstructions { get; set; }

		public int CompareTo([AllowNull] Recipe other)
		{
			return Name.CompareTo(other.Name);
		}
	}

	public class Document
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public FileInfo SourceFile { get; set; }
		public string FilenameHtml { get; set; }
		public string Html { get; set; }
	}
}
