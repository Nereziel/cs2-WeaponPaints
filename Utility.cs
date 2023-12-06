using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Dapper;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace WeaponPaints
{
	internal static class Utility
	{
		internal static WeaponPaintsConfig? Config { get; set; }

		internal static string BuildDatabaseConnectionString()
		{
			if (Config == null) return string.Empty;
			var builder = new MySqlConnectionStringBuilder
			{
				Server = Config.DatabaseHost,
				UserID = Config.DatabaseUser,
				Password = Config.DatabasePassword,
				Database = Config.DatabaseName,
				Port = (uint)Config.DatabasePort,
			};

			return builder.ConnectionString;
		}

		internal static async void CheckDatabaseTables()
		{
			try
			{
				using var connection = new MySqlConnection(BuildDatabaseConnectionString());
				await connection.OpenAsync();

				using var transaction = await connection.BeginTransactionAsync();

				try
				{
					string createTable1 = "CREATE TABLE IF NOT EXISTS `wp_player_skins` (`steamid` varchar(64) NOT NULL, `weapon_defindex` int(6) NOT NULL, `weapon_paint_id` int(6) NOT NULL, `weapon_wear` float NOT NULL DEFAULT 0.0001, `weapon_seed` int(16) NOT NULL DEFAULT 0) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci";
					string createTable2 = "CREATE TABLE IF NOT EXISTS `wp_player_knife` (`steamid` varchar(64) NOT NULL, `knife` varchar(64) NOT NULL, UNIQUE (`steamid`)) ENGINE = InnoDB";

					await connection.ExecuteAsync(createTable1, transaction: transaction);
					await connection.ExecuteAsync(createTable2, transaction: transaction);

					await transaction.CommitAsync();
				}
				catch (Exception)
				{
					await transaction.RollbackAsync();
					throw new Exception("[WeaponPaints] Unable to create tables!");
				}
			}
			catch (Exception ex)
			{
				throw new Exception("[WeaponPaints] Unknown mysql exception! " + ex.Message);
			}
		}

		internal static bool IsPlayerValid(CCSPlayerController? player)
		{
			return (player != null && player.IsValid && !player.IsBot && !player.IsHLTV && player.AuthorizedSteamID != null);
		}
		internal static void LoadSkinsFromFile(string filePath)
		{
			if (File.Exists(filePath))
			{
				string json = File.ReadAllText(filePath);
				var deserializedSkins = JsonConvert.DeserializeObject<List<JObject>>(json);
				WeaponPaints.skinsList = deserializedSkins ?? new List<JObject>();
			}
			else
			{
				throw new FileNotFoundException("File not found.", filePath);
			}
		}

		internal static void Log(string message)
		{
			Console.BackgroundColor = ConsoleColor.DarkGray;
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("[WeaponPaints] " + message);
			Console.ResetColor();
		}

		internal static string ReplaceTags(string message)
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

		internal static async Task CheckVersion(string version)
		{
			using (HttpClient client = new HttpClient())
			{
				try
				{
					HttpResponseMessage response = await client.GetAsync("https://github.com/Nereziel/cs2-WeaponPaints/blob/main/VERSION");

					if (response.IsSuccessStatusCode)
					{
						string remoteVersion = await response.Content.ReadAsStringAsync();
						remoteVersion = remoteVersion.Trim();

						int comparisonResult = string.Compare(version, remoteVersion);

						if (comparisonResult < 0)
						{
							WeaponPaints.logger!.LogWarning("Plugin is outdated! Check https://github.com/Nereziel/cs2-WeaponPaints");
						}
						else if (comparisonResult > 0)
						{
							WeaponPaints.logger!.LogInformation("Probably dev version detected");
						}
						else
						{
							WeaponPaints.logger!.LogInformation("Plugin is up to date");
						}
					}
					else
					{
						WeaponPaints.logger!.LogWarning("Failed to check version");
					}
				}
				catch (Exception)
				{
				}
			}
		}

		internal static void ShowAd(string moduleVersion)
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

		internal static void TestDatabaseConnection()
		{
			try
			{
				using var connection = new MySqlConnection(BuildDatabaseConnectionString());
				connection.Open();

				if (connection.State != System.Data.ConnectionState.Open)
				{
					throw new Exception("[WeaponPaints] Unable connect to database!");
				}
			}
			catch (Exception ex)
			{
				throw new Exception("[WeaponPaints] Unknown mysql exception! " + ex.Message);
			}
			CheckDatabaseTables();
		}
	}
}