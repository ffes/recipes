using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Recipes.Models;

namespace Recipes
{
	public abstract class GenerateBase
	{
		protected readonly List<MyRecipe> Recipes;
		protected readonly List<Document> Documents;
		protected readonly List<Keyword> Keywords;
		protected readonly AppSettings appsettings;

		public virtual bool Enabled => false;

		public GenerateBase(List<MyRecipe> recipes, List<Keyword> keywords, List<Document> documents)
		{
			Recipes = recipes;
			Keywords = keywords;
			Documents = documents;
			appsettings = Program.config.Get<AppSettings>();
		}

		public abstract void Generate();
	}
}
