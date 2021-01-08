namespace Recipes.Models
{
	public class Paths
	{
		public string Input { get; set; }
		public string Output { get; set; }
	}

	public class AppSettings
	{
		public Paths Paths { get; set; }
	}
}
