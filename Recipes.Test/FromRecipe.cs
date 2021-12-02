using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Schema.NET;

namespace Recipes.Test
{
	[TestClass]
	public class FromRecipeTests
	{
		[TestMethod]
		public void NameAndDescription()
		{
			var recipe = ParseRecipeFromJSON(GetBasicRecipe());
			var newModel = Program.FromRecipe(recipe);

			Assert.AreEqual("Mom's World Famous Banana Bread", newModel.Name);
			Assert.AreEqual("This classic banana bread recipe comes from my mom -- the walnuts add a nice texture and flavor to the banana bread.", newModel.Description);
		}

		[TestMethod]
		public void InLanguage()
		{
			var recipe = ParseRecipeFromJSON(GetRecipeWithPublisher());
			var newModel = Program.FromRecipe(recipe);

			Assert.AreEqual("en", newModel.InLanguage);
		}

		[TestMethod]
		public void Author()
		{
			var recipe = ParseRecipeFromJSON(GetBasicRecipe());
			var newModel = Program.FromRecipe(recipe);

			Assert.AreEqual("John Smith", newModel.Author);
		}

		[TestMethod]
		public void Publisher()
		{
			var recipe = ParseRecipeFromJSON(GetRecipeWithPublisher());
			var newModel = Program.FromRecipe(recipe);

			Assert.AreEqual("Some Magazine", newModel.Publisher);
		}

		[TestMethod]
		public void Url()
		{
			var recipe = ParseRecipeFromJSON(GetRecipeWithPublisher());
			var newModel = Program.FromRecipe(recipe);

			Assert.AreEqual(new Uri("http://example.com"), newModel.PublishedURL);
		}

		[TestMethod]
		public void PrepTime()
		{
			var recipe = ParseRecipeFromJSON(GetBasicRecipe());
			var newModel = Program.FromRecipe(recipe);

			Assert.AreEqual(new TimeSpan(0, 15, 0), newModel.PrepTime);
		}

		[TestMethod]
		public void CookTime()
		{
			var recipe = ParseRecipeFromJSON(GetBasicRecipe());
			var newModel = Program.FromRecipe(recipe);

			Assert.AreEqual(new TimeSpan(1, 0, 0), newModel.CookTime);
		}

		[TestMethod]
		public void TotalTime()
		{
			var recipe = ParseRecipeFromJSON(GetRecipeWithKeywords());
			var newModel = Program.FromRecipe(recipe);

			Assert.AreEqual(new TimeSpan(0, 30, 0), newModel.TotalTime);
		}

		[TestMethod]
		public void TotalTimeNotSet()
		{
			var recipe = ParseRecipeFromJSON(GetRecipeWithPublisher());
			var newModel = Program.FromRecipe(recipe);

			Assert.AreEqual(new TimeSpan(), newModel.TotalTime);
		}

		[TestMethod]
		public void Ingredients()
		{
			var recipe = ParseRecipeFromJSON(GetBasicRecipe());
			var newModel = Program.FromRecipe(recipe);

			var expected = new List<string>
			{
				"3 or 4 ripe bananas, smashed",
				"1 egg",
				"3/4 cup of sugar"
			};

			CollectionAssert.AreEqual(expected, newModel.Ingredients);
		}

		[TestMethod]
		public void Instructions()
		{
			var recipe = ParseRecipeFromJSON(GetBasicRecipe());
			var newModel = Program.FromRecipe(recipe);

			var expected = new List<string>
			{
				"Preheat the oven to 350 degrees.",
				"Mix in the ingredients in a bowl. Add the flour last.",
				"Pour the mixture into a loaf pan and bake for one hour."
			};

			CollectionAssert.AreEqual(expected, newModel.Instructions);
		}

		[TestMethod]
		public void Keywords()
		{
			var recipe = ParseRecipeFromJSON(GetRecipeWithKeywords());
			var newModel = Program.FromRecipe(recipe);

			var expected = new List<string>
			{
				"rice",
				"burritos"
			};

			CollectionAssert.AreEqual(expected, newModel.Keywords);
		}

		[TestMethod]
		public void Category()
		{
			var recipe = ParseRecipeFromJSON(GetRecipeWithKeywords());
			var newModel = Program.FromRecipe(recipe);

			Assert.AreEqual("Main Course", newModel.Category);
		}

		[TestMethod]
		public void Cuisine()
		{
			var recipe = ParseRecipeFromJSON(GetRecipeWithKeywords());
			var newModel = Program.FromRecipe(recipe);

			Assert.AreEqual("Mexican", newModel.Cuisine);
		}

		[TestMethod]
		public void YieldText()
		{
			var recipe = ParseRecipeFromJSON(GetBasicRecipe());
			var newModel = Program.FromRecipe(recipe);

			Assert.AreEqual("1 loaf", newModel.Yield);
		}

		[TestMethod]
		public void YieldQuantity()
		{
			var recipe = ParseRecipeFromJSON(GetRecipeWithKeywords());
			var newModel = Program.FromRecipe(recipe);

			Assert.AreEqual("4 persons", newModel.Yield);
		}

		[TestMethod]
		public void DatePublished()
		{
			var recipe = ParseRecipeFromJSON(GetBasicRecipe());
			var newModel = Program.FromRecipe(recipe);

			Assert.AreEqual(new DateTime(2009, 5, 8), newModel.DatePublished);
		}

		[TestMethod]
		public void DatePublishedNotSet()
		{
			var recipe = ParseRecipeFromJSON(GetRecipeWithPublisher());
			var newModel = Program.FromRecipe(recipe);

			Assert.AreEqual(new DateTime(), newModel.DatePublished);
		}

		// Parse the JSON to Schema.NET.Recipe
		private static Recipe ParseRecipeFromJSON(string JSON)
		{
			try
			{
				return SchemaSerializer.DeserializeObject<Recipe>(JSON);
			}
			catch (JsonReaderException ex)
			{
				Console.WriteLine($"Error parsing JSON: {ex.Message}");
				return null;
			}
		}

		// The (slightly changed) sample recipe from https://schema.org/Recipe
		private static string GetBasicRecipe() =>
		@"{
			""@context"": ""https://schema.org"",
			""@type"": ""Recipe"",
			""author"": {
				""@type"": ""Person"",
				""name"": ""John Smith""
			},
			""cookTime"": ""PT1H"",
			""datePublished"": ""2009-05-08"",
			""description"": ""This classic banana bread recipe comes from my mom -- the walnuts add a nice texture and flavor to the banana bread."",
			""image"": ""bananabread.jpg"",
			""recipeIngredient"": [
				""3 or 4 ripe bananas, smashed"",
				""1 egg"",
				""3/4 cup of sugar""
			],
			""name"": ""Mom's World Famous Banana Bread"",
			""nutrition"": {
				""@type"": ""NutritionInformation"",
				""calories"": ""240 calories"",
				""fatContent"": ""9 grams fat""
			},
			""prepTime"": ""PT15M"",
			""recipeInstructions"": [
				""Preheat the oven to 350 degrees."",
				""Mix in the ingredients in a bowl. Add the flour last."",
				""Pour the mixture into a loaf pan and bake for one hour.""
			],
			""recipeYield"": ""1 loaf"",
			""suitableForDiet"": ""https://schema.org/LowFatDiet""
		}";

		private static string GetRecipeWithKeywords() =>
		@"{
			""@context"": ""https://schema.org"",
			""@type"": ""Recipe"",
			""totalTime"": ""PT30M"",
			""keywords"": ""rice,burritos"",
			""recipeYield"": {
				""value"": 4,
				""unitText"": ""persons""
			},
			""recipeCategory"": ""Main Course"",
			""recipeCuisine"": ""Mexican""
		}";

		private static string GetRecipeWithPublisher() =>
		@"{
			""@context"": ""https://schema.org"",
			""@type"": ""Recipe"",
			""inLanguage"": ""en"",
			""publisher"": {
				""@type"": ""Organization"",
				""name"": ""Some Magazine""
			},
			""url"": ""http://example.com""
		}";
	}
}
