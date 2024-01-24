using System;

namespace Recipes.Models
{
	public class General
	{
		public string Name { get; set; }
		public string Author { get; set; }
		public string Language { get; set; }
	}

	public class Partials
	{
		public string Recipes { get; set; }
	}

	public class Templates
	{
		public string Base { get; set; }
		public Partials Partials { get; set; }
	}

	public class Website
	{
		public bool Enabled { get; set; }
		public string Output { get; set; }
		public string WebFiles { get; set; }
		public Templates Templates { get; set; }
	}

	public class EPUB
	{
		public bool Enabled { get; set; }
		public string Filename { get; set; }
		public Guid BookId { get; set; }
	}

	public class InputPaths
	{
		public string Recipes { get; set; }
		public string Documents { get; set; }
	}

	public class AppSettings
	{
		public General General { get; set; }
		public InputPaths InputPaths { get; set; }
		public Website Website { get; set; }
		public EPUB EPUB { get; set; }
	}
}
