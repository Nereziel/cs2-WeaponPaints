using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

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
				Pooling = true
			};

			return builder.ConnectionString;
		}

		internal static async Task CheckDatabaseTables()
		{
			if (WeaponPaints._database is null) return;

			try
			{
				await using var connection = await WeaponPaints._database.GetConnectionAsync();

				await using var transaction = await connection.BeginTransactionAsync();

				try
				{
					string[] createTableQueries = new[]
					{
				@"CREATE TABLE IF NOT EXISTS `wp_player_skins` (
                        `steamid` varchar(64) NOT NULL,
                        `weapon_defindex` int(6) NOT NULL,
                        `weapon_paint_id` int(6) NOT NULL,
                        `weapon_wear` float NOT NULL DEFAULT 0.000001,
                        `weapon_seed` int(16) NOT NULL DEFAULT 0
                    ) ENGINE=InnoDB",
				@"CREATE TABLE IF NOT EXISTS `wp_player_knife` (
                        `steamid` varchar(64) NOT NULL,
                        `knife` varchar(64) NOT NULL,
                        UNIQUE (`steamid`)
                    ) ENGINE = InnoDB",
				@"CREATE TABLE IF NOT EXISTS `wp_player_gloves` (
					 `steamid` varchar(64) NOT NULL,
					 `weapon_defindex` int(11) NOT NULL,
                      UNIQUE (`steamid`)
					) ENGINE=InnoDB"
			};

					foreach (var query in createTableQueries)
					{
						await connection.ExecuteAsync(query, transaction: transaction);
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
				throw new Exception("[WeaponPaints] Unknown MySQL exception! " + ex.Message);
			}
		}

		internal static bool IsPlayerValid(CCSPlayerController? player)
		{
			if (player is null) return false;

			return (player is not null && player.IsValid && !player.IsBot && !player.IsHLTV && player.UserId.HasValue
				&& WeaponPaints.weaponSync != null && player.Connected == PlayerConnectedState.PlayerConnected && player.SteamID.ToString().Length == 17);
		}

		internal static void LoadSkinsFromFile(string filePath)
		{
			try
			{
				string json = File.ReadAllText(filePath);
				var deserializedSkins = JsonConvert.DeserializeObject<List<JObject>>(json);
				WeaponPaints.skinsList = deserializedSkins ?? new List<JObject>();
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}

		internal static void LoadGlovesFromFile(string filePath)
		{
			try
			{
				string json = File.ReadAllText(filePath);
				var deserializedSkins = JsonConvert.DeserializeObject<List<JObject>>(json);
				WeaponPaints.glovesList = deserializedSkins ?? new List<JObject>();
			}
			catch (FileNotFoundException)
			{
				throw;
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
					HttpResponseMessage response = await client.GetAsync("https://raw.githubusercontent.com/Nereziel/cs2-WeaponPaints/main/VERSION").ConfigureAwait(false);

					if (response.IsSuccessStatusCode)
					{
						string remoteVersion = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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
				catch (HttpRequestException ex)
				{
					logger.LogError(ex, "Failed to connect to the version server.");
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "An error occurred while checking version.");
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

		/*(
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
		*/
	}
}