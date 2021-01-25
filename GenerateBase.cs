using System.Collections.Generic;
using Recipes.Models;

namespace Recipes
{
	public class GenerateBase
	{
		protected readonly List<Recipe> Recipes;
		protected readonly List<Document> Documents;

		public GenerateBase(List<Recipe> recipes, List<Document> documents)
		{
			Recipes = recipes;
			Documents = documents;
		}
	}
}
