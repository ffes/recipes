using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Recipes.Models;

namespace Recipes
{
	public abstract class GenerateBase(List<RecipeModel> recipes, List<Keyword> keywords, List<Document> documents)
	{
		protected readonly List<RecipeModel> Recipes = recipes;
		protected readonly List<Document> Documents = documents;
		protected readonly List<Keyword> Keywords = keywords;
		protected readonly AppSettings appsettings = Program.config.Get<AppSettings>();

		public virtual bool Enabled => false;

		public abstract void Generate();
	}
}
