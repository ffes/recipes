using System;

namespace Recipes.Models
{
	public class Website
	{
		public bool Enabled { get; set; }
		public string Output { get; set; }
		public string Stylesheet { get; set; }
	}

	public class EPUB
	{
		public bool Enabled { get; set; }
		public string Filename { get; set; }
		public string Name { get; set; }
		public string Author { get; set; }
		public string Language { get; set; }
		public Guid BookId { get; set; }
	}

	public class AppSettings
	{
		public string InputPath { get; set; }
		public Website Website { get; set; }
		public EPUB EPUB { get; set; }
	}
}
