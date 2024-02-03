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

                // Minimal version for MySQL 5.6.5
                string[] sqlCommands = new string[]
                {
					"CREATE TABLE IF NOT EXISTS `wp_users` (`id` INT UNSIGNED NOT NULL AUTO_INCREMENT, `steamid` BIGINT UNSIGNED NOT NULL, `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, PRIMARY KEY (`id`), UNIQUE KEY `unique_steamid` (`steamid`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
					"CREATE TABLE IF NOT EXISTS `wp_users_items` (`user_id` INT UNSIGNED NOT NULL, `weapon` SMALLINT UNSIGNED NOT NULL, `paint` SMALLINT UNSIGNED NOT NULL, `wear` FLOAT NOT NULL DEFAULT 0.00001, `seed` SMALLINT UNSIGNED NOT NULL DEFAULT 0, `nametag` VARCHAR(20) DEFAULT NULL, `stattrack` INT UNSIGNED NOT NULL DEFAULT 0, `stattrack_enabled` SMALLINT NOT NULL DEFAULT 0, `quality` SMALLINT UNSIGNED NOT NULL DEFAULT 0, PRIMARY KEY (`user_id`,`weapon`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
					"CREATE TABLE IF NOT EXISTS `wp_users_knife` (`user_id` INT UNSIGNED NOT NULL, `knife` VARCHAR(32) DEFAULT NULL, PRIMARY KEY (`user_id`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
					"CREATE TABLE IF NOT EXISTS `wp_users_music` (`user_id` INT UNSIGNED NOT NULL, `music` SMALLINT UNSIGNED DEFAULT NULL, PRIMARY KEY (`user_id`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;"
				};
                try
				{
                    foreach (string command in sqlCommands)
                    {
                        await connection.ExecuteAsync(command, transaction: transaction);
                    }

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

		internal static async Task CheckVersion(string version, ILogger logger)
		{
			using (HttpClient client = new HttpClient())
			{
				try
				{
					HttpResponseMessage response = await client.GetAsync("https://raw.githubusercontent.com/Nereziel/cs2-WeaponPaints/main/VERSION");

					if (response.IsSuccessStatusCode)
					{
						string remoteVersion = await response.Content.ReadAsStringAsync();
						remoteVersion = remoteVersion.Trim();

						int comparisonResult = string.Compare(version, remoteVersion);

						if (comparisonResult < 0)
						{
							logger.LogWarning("Plugin is outdated! Check https://github.com/Nereziel/cs2-WeaponPaints");
						}
						else if (comparisonResult > 0)
						{
							logger.LogInformation("Probably dev version detected");
						}
						else
						{
							logger.LogInformation("Plugin is up to date");
						}
					}
					else
					{
						logger.LogWarning("Failed to check version");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
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