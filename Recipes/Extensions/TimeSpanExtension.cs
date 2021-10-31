using System;

namespace Recipes
{
	public static class TimeSpanExtension
	{
		public static string ToReadableString(this TimeSpan span)
		{
			string readable = "";
			if (span.Hours > 0)
			{
				readable += $"{span.Hours} uur";
			}

			if (span.Minutes > 0)
			{
				if (readable.Length > 0)
					readable += ", ";
				readable += $"{span.Minutes} " + (span.Minutes == 1 ? "minuut" : "minuten");
			}

			return readable;
		}
	}
}
