using System;

namespace Recipes.Models
{
	public class Paths
	{
		public string Input { get; set; }
		public string Output { get; set; }
	}

	public class EPUB
	{
		public string Filename { get; set; }
		public string Name { get; set; }
		public string Author { get; set; }
		public string Language { get; set; }
		public Guid BookId { get; set; }
	}

	public class AppSettings
	{
		public Paths Paths { get; set; }
		public EPUB EPUB { get; set; }
	}
}
