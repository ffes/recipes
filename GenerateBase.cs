using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Recipes.Models;

namespace Recipes
{
	public class GenerateBase
	{
		protected readonly List<Recipe> Recipes;
		protected readonly List<Document> Documents;
		protected readonly AppSettings appsettings;

		public GenerateBase(List<Recipe> recipes, List<Document> documents)
		{
			Recipes = recipes;
			Documents = documents;
			appsettings = Program.config.Get<AppSettings>();
		}
	}
}
