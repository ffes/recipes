using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Schema.NET;

namespace Recipes.Models
{
	public class MyRecipe: Recipe, IComparable<MyRecipe>
	{
		public FileInfo SourceFile { get; set; }
		public string FilenameHtml { get; set; }
		public string EpubID { get; set; }

		public int CompareTo([AllowNull] MyRecipe other)
		{
			string name = Name;
			return name.CompareTo(other.Name);
		}
	}

	public class Document
	{
		public string EpubID { get; set; }
		public string Name { get; set; }
		public FileInfo SourceFile { get; set; }
		public string FilenameHtml { get; set; }
		public string Html { get; set; }
	}

	public class Keyword: IComparable<Keyword>
	{
		public string Name { get; set; }
		public List<MyRecipe> Recipes { get; set; }

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is Keyword kw)
				return Name == kw.Name;

			return false;
		}

		public int CompareTo([AllowNull] Keyword other)
		{
			return Name.CompareTo(other.Name);
		}
	}
}
