using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Menu;
using Dapper;
using MenuManager;
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
			if (WeaponPaints.Database is null) return;

			try
			{
				await using var connection = await WeaponPaints.Database.GetConnectionAsync();
				await using var transaction = await connection.BeginTransactionAsync();

				try
				{
					string[] createTableQueries =
					[
						@"
					    CREATE TABLE IF NOT EXISTS `wp_player_skins` (
					        `steamid` varchar(18) NOT NULL,
					        `weapon_team` int(1) NOT NULL,
					        `weapon_defindex` int(6) NOT NULL,
					        `weapon_paint_id` int(6) NOT NULL,
					        `weapon_wear` float NOT NULL DEFAULT 0.000001,
					        `weapon_seed` int(16) NOT NULL DEFAULT 0,
					        `weapon_nametag` VARCHAR(128) DEFAULT NULL,
					        `weapon_stattrak` tinyint(1) NOT NULL DEFAULT 0,
					        `weapon_stattrak_count` int(10) NOT NULL DEFAULT 0,
					        `weapon_sticker_0` VARCHAR(128) NOT NULL DEFAULT '0;0;0;0;0;0;0' COMMENT 'id;schema;x;y;wear;scale;rotation',
					        `weapon_sticker_1` VARCHAR(128) NOT NULL DEFAULT '0;0;0;0;0;0;0' COMMENT 'id;schema;x;y;wear;scale;rotation',
					        `weapon_sticker_2` VARCHAR(128) NOT NULL DEFAULT '0;0;0;0;0;0;0' COMMENT 'id;schema;x;y;wear;scale;rotation',
					        `weapon_sticker_3` VARCHAR(128) NOT NULL DEFAULT '0;0;0;0;0;0;0' COMMENT 'id;schema;x;y;wear;scale;rotation',
					        `weapon_sticker_4` VARCHAR(128) NOT NULL DEFAULT '0;0;0;0;0;0;0' COMMENT 'id;schema;x;y;wear;scale;rotation',
					        `weapon_keychain` VARCHAR(128) NOT NULL DEFAULT '0;0;0;0;0' COMMENT 'id;x;y;z;seed',
					        UNIQUE (`steamid`, `weapon_team`, `weapon_defindex`) -- Add unique constraint here
					    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;",

					    @"
					    CREATE TABLE IF NOT EXISTS `wp_player_knife` (
					        `steamid` varchar(18) NOT NULL,
					        `weapon_team` int(1) NOT NULL,
					        `knife` varchar(64) NOT NULL,
					        UNIQUE (`steamid`, `weapon_team`) -- Unique constraint
					    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;",

					    @"
					    CREATE TABLE IF NOT EXISTS `wp_player_gloves` (
					        `steamid` varchar(18) NOT NULL,
					        `weapon_team` int(1) NOT NULL,
					        `weapon_defindex` int(11) NOT NULL,
					        UNIQUE (`steamid`, `weapon_team`) -- Unique constraint
					    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;",

					    @"
					    CREATE TABLE IF NOT EXISTS `wp_player_agents` (
					        `steamid` varchar(18) NOT NULL,
					        `agent_ct` varchar(64) DEFAULT NULL,
					        `agent_t` varchar(64) DEFAULT NULL,
					        UNIQUE (`steamid`) -- Unique constraint
					    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;",

					    @"
					    CREATE TABLE IF NOT EXISTS `wp_player_music` (
					        `steamid` varchar(64) NOT NULL,
					        `weapon_team` int(1) NOT NULL,
					        `music_id` int(11) NOT NULL,
					        UNIQUE (`steamid`, `weapon_team`) -- Unique constraint
					    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;",

					    @"
					    CREATE TABLE IF NOT EXISTS `wp_player_pins` (
					        `steamid` varchar(64) NOT NULL,
					        `weapon_team` int(1) NOT NULL,
					        `id` int(11) NOT NULL,
					        UNIQUE (`steamid`, `weapon_team`) -- Unique constraint
					    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;"
					];

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
			if (player is null || WeaponPaints.WeaponSync is null) return false;

			return player is { IsValid: true, IsBot: false, IsHLTV: false, UserId: not null };
		}

		internal static void LoadSkinsFromFile(string filePath, ILogger logger)
		{
			var json = File.ReadAllText(filePath);
			try
			{
				var deserializedSkins = JsonConvert.DeserializeObject<List<JObject>>(json);
				WeaponPaints.SkinsList = deserializedSkins ?? [];
			}
			catch (FileNotFoundException)
			{
				logger?.LogError("Not found \"skins.json\" file");
			}
		}
		
		internal static void LoadPinsFromFile(string filePath, ILogger logger)
		{
			var json = File.ReadAllText(filePath);
			try
			{
				var deserializedPins = JsonConvert.DeserializeObject<List<JObject>>(json);
				WeaponPaints.PinsList = deserializedPins ?? [];
			}
			catch (FileNotFoundException)
			{
				logger?.LogError("Not found \"pins.json\" file");
			}
		}

		internal static void LoadGlovesFromFile(string filePath, ILogger logger)
		{
			try
			{
				var json = File.ReadAllText(filePath);
				var deserializedSkins = JsonConvert.DeserializeObject<List<JObject>>(json);
				WeaponPaints.GlovesList = deserializedSkins ?? [];
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
				WeaponPaints.AgentsList = deserializedSkins ?? [];
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
				WeaponPaints.MusicList = deserializedSkins ?? [];
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
		
		internal static IMenu? CreateMenu(string title)
		{
			var menuType = WeaponPaints.Instance.Config.MenuType.ToLower();
        
			var menu = menuType switch
			{
				_ when menuType.Equals("selectable", StringComparison.CurrentCultureIgnoreCase) =>
					WeaponPaints.MenuApi?.NewMenu(title),

				_ when menuType.Equals("dynamic", StringComparison.CurrentCultureIgnoreCase) =>
					WeaponPaints.MenuApi?.NewMenuForcetype(title, MenuType.ButtonMenu),

				_ when menuType.Equals("center", StringComparison.CurrentCultureIgnoreCase) =>
					WeaponPaints.MenuApi?.NewMenuForcetype(title, MenuType.CenterMenu),

				_ when menuType.Equals("chat", StringComparison.CurrentCultureIgnoreCase) =>
					WeaponPaints.MenuApi?.NewMenuForcetype(title, MenuType.ChatMenu),

				_ when menuType.Equals("console", StringComparison.CurrentCultureIgnoreCase) =>
					WeaponPaints.MenuApi?.NewMenuForcetype(title, MenuType.ConsoleMenu),

				_ => WeaponPaints.MenuApi?.NewMenu(title)
			};

			return menu;
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