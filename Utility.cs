using CounterStrikeSharp.API.Modules.Utils;
using System.Reflection;

namespace WeaponPaints
{
	public static class Utility
	{
		public static WeaponPaintsConfig? Config { get; set; }

		public static string ReplaceTags(string message)
		{
			if (message.Contains('{'))
			{
				string modifiedValue = message;
				if (Config != null)
				{
					modifiedValue = modifiedValue.Replace("{WEBSITE}", Config.Website);
				}
				foreach (FieldInfo field in typeof(ChatColors).GetFields())
				{
					string pattern = $"{{{field.Name}}}";
					if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
					{
						modifiedValue = modifiedValue.Replace(pattern, field.GetValue(null)!.ToString(), StringComparison.OrdinalIgnoreCase);
					}
				}
				return modifiedValue;
			}

			return message;
		}

		public static void Log(string message)
		{
			Console.BackgroundColor = ConsoleColor.DarkGray;
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("[WeaponPaints] " + message);
			Console.ResetColor();
		}
		public static void ShowAd(string moduleVersion)
		{
			Console.WriteLine(" ");
			Console.WriteLine(" _     _  _______  _______  _______  _______  __    _  _______  _______  ___   __    _  _______  _______ ");
			Console.WriteLine("| | _ | ||       ||   _   ||       ||       ||  |  | ||       ||   _   ||   | |  |  | ||       ||       |");
			Console.WriteLine("| || || ||    ___||  |_|  ||    _  ||   _   ||   |_| ||    _  ||  |_|  ||   | |   |_| ||_     _||  _____|");
			Console.WriteLine("|       ||   |___ |       ||   |_| ||  | |  ||       ||   |_| ||       ||   | |       |  |   |  | |_____ ");
			Console.WriteLine("|       ||    ___||       ||    ___||  |_|  ||  _    ||    ___||       ||   | |  _    |  |   |  |_____  |");
			Console.WriteLine("|   _   ||   |___ |   _   ||   |    |       || | |   ||   |    |   _   ||   | | | |   |  |   |   _____| |");
			Console.WriteLine("|__| |__||_______||__| |__||___|    |_______||_|  |__||___|    |__| |__||___| |_|  |__|  |___|  |_______|");
			Console.WriteLine("						>> Version: " + moduleVersion);
			Console.WriteLine("			>> GitHub: https://github.com/Nereziel/cs2-WeaponPaints");
			Console.WriteLine(" ");
		}
	}
}