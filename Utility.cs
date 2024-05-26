using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using Dapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WeaponPaints
{
	internal static class Utility
	{
		internal static WeaponPaintsConfig? Config { get; set; }

		internal static async Task CheckDatabaseTables()
		{
			if (WeaponPaints._database is null) return;

			try
			{
				await using var connection = await WeaponPaints._database.GetConnectionAsync();

				await using var transaction = await connection.BeginTransactionAsync();

				try
                {
                    var createTableQueries = new[]
                    {
                        "CREATE TABLE IF NOT EXISTS `wp_users` (`id` INT UNSIGNED NOT NULL AUTO_INCREMENT, `steamid` BIGINT UNSIGNED NOT NULL, `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, PRIMARY KEY (`id`), UNIQUE KEY `unique_steamid` (`steamid`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                        "CREATE TABLE IF NOT EXISTS `wp_users_items` (`user_id` INT UNSIGNED NOT NULL, `weapon` SMALLINT UNSIGNED NOT NULL, `paint` SMALLINT UNSIGNED NOT NULL, `wear` FLOAT NOT NULL DEFAULT 0.001, `seed` SMALLINT UNSIGNED NOT NULL DEFAULT 0, `nametag` VARCHAR(20) DEFAULT NULL, `stattrack` INT UNSIGNED NOT NULL DEFAULT 0, `stattrack_enabled` SMALLINT NOT NULL DEFAULT 0, `quality` SMALLINT UNSIGNED NOT NULL DEFAULT 0, PRIMARY KEY (`user_id`,`weapon`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                        "CREATE TABLE IF NOT EXISTS `wp_users_knife` (`user_id` INT UNSIGNED NOT NULL, `knife` VARCHAR(32) DEFAULT NULL, PRIMARY KEY (`user_id`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                        "CREATE TABLE IF NOT EXISTS `wp_users_gloves` (`user_id` INT UNSIGNED NOT NULL, `weapon_defindex` SMALLINT UNSIGNED DEFAULT NULL, PRIMARY KEY (`user_id`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                        "CREATE TABLE IF NOT EXISTS `wp_users_music` (`user_id` INT UNSIGNED NOT NULL, `music` SMALLINT UNSIGNED DEFAULT NULL, PRIMARY KEY (`user_id`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",
                        "CREATE TABLE IF NOT EXISTS `wp_users_agents` (`user_id` INT UNSIGNED NOT NULL,`agent_ct` varchar(64) DEFAULT NULL,`agent_t` varchar(64) DEFAULT NULL, PRIMARY KEY (`user_id`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;"
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
			if (player is null || WeaponPaints.weaponSync is null) return false;

			return player is { IsValid: true, IsBot: false, IsHLTV: false, UserId: not null };
		}

		internal static void LoadSkinsFromFile(string filePath, ILogger logger)
		{
			var json = File.ReadAllText(filePath);
			try
			{
				var deserializedSkins = JsonConvert.DeserializeObject<List<JObject>>(json);
				WeaponPaints.skinsList = deserializedSkins ?? [];
			}
			catch (FileNotFoundException)
			{
				logger?.LogError("Not found \"skins.json\" file");
			}
		}

		internal static void LoadGlovesFromFile(string filePath, ILogger logger)
		{
			try
			{
				var json = File.ReadAllText(filePath);
				var deserializedSkins = JsonConvert.DeserializeObject<List<JObject>>(json);
				WeaponPaints.glovesList = deserializedSkins ?? [];
			}
			catch (FileNotFoundException)
			{
				logger?.LogError("Not found \"gloves.json\" file");
			}
		}

		internal static void LoadAgentsFromFile(string filePath, ILogger logger)
		{
			try
			{
				var json = File.ReadAllText(filePath);
				var deserializedSkins = JsonConvert.DeserializeObject<List<JObject>>(json);
				WeaponPaints.agentsList = deserializedSkins ?? [];
			}
			catch (FileNotFoundException)
			{
				logger?.LogError("Not found \"agents.json\" file");
			}
		}

		internal static void LoadMusicFromFile(string filePath, ILogger logger)
		{
			try
			{
				var json = File.ReadAllText(filePath);
				var deserializedSkins = JsonConvert.DeserializeObject<List<JObject>>(json);
				WeaponPaints.musicList = deserializedSkins ?? [];
			}
			catch (FileNotFoundException)
			{
				logger?.LogError("Not found \"music.json\" file");
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
			return message.ReplaceColorTags();
		}

		internal static async Task CheckVersion(string version, ILogger logger)
		{
			using HttpClient client = new();

			try
			{
				var response = await client.GetAsync("https://raw.githubusercontent.com/Nereziel/cs2-WeaponPaints/main/VERSION").ConfigureAwait(false);

				if (response.IsSuccessStatusCode)
				{
					var remoteVersion = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
					remoteVersion = remoteVersion.Trim();

					var comparisonResult = string.CompareOrdinal(version, remoteVersion);

					switch (comparisonResult)
					{
						case < 0:
							logger.LogWarning("Plugin is outdated! Check https://github.com/Nereziel/cs2-WeaponPaints");
							break;
						case > 0:
							logger.LogInformation("Probably dev version detected");
							break;
						default:
							logger.LogInformation("Plugin is up to date");
							break;
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
	}
}