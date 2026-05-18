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

		internal static void LoadStickersFromFile(string filePath, ILogger logger)
		{
			try
			{
				var json = File.ReadAllText(filePath);
				var deserializedStickers = JsonConvert.DeserializeObject<List<JObject>>(json);
				WeaponPaints.StickersList = deserializedStickers ?? [];
			}
			catch (FileNotFoundException)
			{
				logger?.LogError("Not found \"stickers.json\" file");
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


		internal static async Task LoadSkinDataAsync(string moduleDirectory, WeaponPaintsConfig config, ILogger logger)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(config.SkinApiURL))
				{
					await LoadOnlineSkinDataAsync(moduleDirectory, config, logger).ConfigureAwait(false);
					return;
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to load skin data from online API. Falling back to bundled JSON data.");
			}

			LoadLocalSkinData(moduleDirectory, config.SkinsLanguage, logger);
		}

		internal static void LoadLocalSkinData(string moduleDirectory, string language, ILogger logger)
		{
			LoadSkinsFromFile(Path.Combine(moduleDirectory, "data", $"skins_{language}.json"), logger);
			LoadGlovesFromFile(Path.Combine(moduleDirectory, "data", $"gloves_{language}.json"), logger);
			LoadAgentsFromFile(Path.Combine(moduleDirectory, "data", $"agents_{language}.json"), logger);
			LoadMusicFromFile(Path.Combine(moduleDirectory, "data", $"music_{language}.json"), logger);
			LoadPinsFromFile(Path.Combine(moduleDirectory, "data", $"collectibles_{language}.json"), logger);
			LoadStickersFromFile(Path.Combine(moduleDirectory, "data", $"stickers_{language}.json"), logger);
		}

		private static async Task LoadOnlineSkinDataAsync(string moduleDirectory, WeaponPaintsConfig config, ILogger logger)
		{
			using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
			var baseUrl = config.SkinApiURL.TrimEnd('/');
			var apiLanguage = NormalizeApiLanguage(config.SkinsLanguage);

			var skinsJson = await client.GetStringAsync($"{baseUrl}/{apiLanguage}/skins.json").ConfigureAwait(false);
			var agentsJson = await client.GetStringAsync($"{baseUrl}/{apiLanguage}/agents.json").ConfigureAwait(false);
			var musicJson = await client.GetStringAsync($"{baseUrl}/{apiLanguage}/music_kits.json").ConfigureAwait(false);
			var collectiblesJson = await client.GetStringAsync($"{baseUrl}/{apiLanguage}/collectibles.json").ConfigureAwait(false);
			var stickersJson = await client.GetStringAsync($"{baseUrl}/{apiLanguage}/stickers.json").ConfigureAwait(false);

			var skins = LoadDefaultEntriesFromLocalFile(moduleDirectory, config.SkinsLanguage, "skins", token => token["paint"]?.ToString() == "0");
			var gloves = LoadDefaultEntriesFromLocalFile(moduleDirectory, config.SkinsLanguage, "gloves", token => token["paint"]?.ToString() == "0");
			var localSkinLookup = LoadLocalSkinLookup(moduleDirectory, config.SkinsLanguage);

			foreach (var item in JArray.Parse(skinsJson).OfType<JObject>())
			{
				var category = item["category"]?["name"]?.ToString();
				if (string.Equals(category, "Gloves", StringComparison.OrdinalIgnoreCase))
				{
					var glove = ConvertApiSkinToPluginGlove(item);
					if (glove != null) gloves.Add(glove);
					continue;
				}

				var skin = ConvertApiSkinToPluginSkin(item, localSkinLookup);
				if (skin != null) skins.Add(skin);
			}

			WeaponPaints.SkinsList = skins;
			WeaponPaints.GlovesList = gloves;
			WeaponPaints.AgentsList = ConvertApiAgentsToPluginAgents(agentsJson);
			WeaponPaints.MusicList = ConvertApiMusicToPluginMusic(musicJson);
			WeaponPaints.PinsList = ConvertApiCollectiblesToPluginPins(collectiblesJson);
			WeaponPaints.StickersList = ConvertApiStickersToPluginStickers(stickersJson);

			logger.LogInformation("Loaded skin data from online JSON API ({ApiUrl}, language {Language}).", baseUrl, apiLanguage);
		}

		private static string NormalizeApiLanguage(string language)
		{
			return language switch
			{
				"zh-cn" => "zh-CN",
				"zh-hans" => "zh-CN",
				"zh-tw" => "zh-TW",
				"ua" => "uk",
				_ => language
			};
		}

		private static List<JObject> LoadDefaultEntriesFromLocalFile(string moduleDirectory, string language, string filePrefix, Func<JObject, bool> predicate)
		{
			try
			{
				var path = Path.Combine(moduleDirectory, "data", $"{filePrefix}_{language}.json");
				if (!File.Exists(path)) return [];

				return JArray.Parse(File.ReadAllText(path))
					.OfType<JObject>()
					.Where(predicate)
					.Select(token => (JObject)token.DeepClone())
					.ToList();
			}
			catch
			{
				return [];
			}
		}

		private static Dictionary<string, JObject> LoadLocalSkinLookup(string moduleDirectory, string language)
		{
			try
			{
				var path = Path.Combine(moduleDirectory, "data", $"skins_{language}.json");
				if (!File.Exists(path)) return [];

				return JArray.Parse(File.ReadAllText(path))
					.OfType<JObject>()
					.Where(token => TryReadInt(token["weapon_defindex"]) != null && TryReadInt(token["paint"]) != null)
					.GroupBy(token => GetSkinLookupKey(TryReadInt(token["weapon_defindex"])!.Value, TryReadInt(token["paint"])!.Value))
					.ToDictionary(group => group.Key, group => (JObject)group.First().DeepClone());
			}
			catch
			{
				return [];
			}
		}

		private static string GetSkinLookupKey(int weaponDefIndex, int paint)
		{
			return $"{weaponDefIndex}:{paint}";
		}

		private static bool? ReadBool(JToken? token)
		{
			if (token == null) return null;
			if (token.Type == JTokenType.Boolean) return token.Value<bool>();

			var value = token.ToString();
			if (bool.TryParse(value, out var boolValue)) return boolValue;
			if (int.TryParse(value, out var intValue)) return intValue != 0;

			return null;
		}

		private static JObject? ConvertApiSkinToPluginSkin(JObject item, IReadOnlyDictionary<string, JObject> localSkinLookup)
		{
			var weaponDefIndex = TryReadInt(item["weapon_defindex"]) ?? TryReadInt(item["weapon"]?["weapon_id"]);
			var paint = TryReadInt(item["paint"]) ?? TryReadInt(item["paint_index"]);
			var paintName = item["paint_name"]?.ToString() ?? item["name"]?.ToString();

			if (weaponDefIndex == null || paint == null || string.IsNullOrWhiteSpace(paintName))
			{
				return null;
			}

			localSkinLookup.TryGetValue(GetSkinLookupKey(weaponDefIndex.Value, paint.Value), out var localSkin);

			var weaponName = item["weapon_name"]?.ToString()
			                 ?? localSkin?["weapon_name"]?.ToString()
			                 ?? item["original"]?["name"]?.ToString();

			if (string.IsNullOrWhiteSpace(weaponName))
			{
				return null;
			}

			var legacyModel = ReadBool(item["legacy_model"])
			                  ?? ReadBool(localSkin?["legacy_model"])
			                  ?? false;

			return new JObject
			{
				["weapon_defindex"] = weaponDefIndex.Value,
				["weapon_name"] = weaponName,
				["paint"] = paint.Value,
				["image"] = item["image"]?.ToString() ?? localSkin?["image"]?.ToString() ?? string.Empty,
				["paint_name"] = paintName,
				["legacy_model"] = legacyModel
			};
		}

		private static JObject? ConvertApiSkinToPluginGlove(JObject item)
		{
			var weaponDefIndex = TryReadInt(item["weapon"]?["weapon_id"]);
			var paint = TryReadInt(item["paint_index"]);
			var paintName = item["name"]?.ToString();

			if (weaponDefIndex == null || paint == null || string.IsNullOrWhiteSpace(paintName))
			{
				return null;
			}

			return new JObject
			{
				["weapon_defindex"] = weaponDefIndex.Value,
				["paint"] = paint.Value,
				["image"] = item["image"]?.ToString() ?? string.Empty,
				["paint_name"] = paintName
			};
		}

		private static List<JObject> ConvertApiAgentsToPluginAgents(string json)
		{
			var agents = new List<JObject>
			{
				new() { ["team"] = 2, ["image"] = string.Empty, ["model"] = "null", ["agent_name"] = "Agent | Default" },
				new() { ["team"] = 3, ["image"] = string.Empty, ["model"] = "null", ["agent_name"] = "Agent | Default" }
			};

			foreach (var item in JArray.Parse(json).OfType<JObject>())
			{
				var name = item["name"]?.ToString();
				var teamId = item["team"]?["id"]?.ToString();
				var model = NormalizeAgentModelPath(item["model_player"]?.ToString());

				if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(model))
				{
					continue;
				}

				agents.Add(new JObject
				{
					["team"] = string.Equals(teamId, "counter-terrorists", StringComparison.OrdinalIgnoreCase) ? 3 : 2,
					["image"] = item["image"]?.ToString() ?? string.Empty,
					["model"] = model,
					["agent_name"] = name
				});
			}

			return agents;
		}

		private static List<JObject> ConvertApiMusicToPluginMusic(string json)
		{
			return JArray.Parse(json)
				.OfType<JObject>()
				.Select(item => new JObject
				{
					["id"] = ReadDefIndex(item),
					["name"] = item["name"]?.ToString() ?? string.Empty,
					["image"] = item["image"]?.ToString() ?? string.Empty
				})
				.Where(item => !string.IsNullOrWhiteSpace(item["id"]?.ToString()) &&
				               !string.IsNullOrWhiteSpace(item["name"]?.ToString()) &&
				               !item["name"]!.ToString().Contains("StatTrak", StringComparison.OrdinalIgnoreCase))
				.ToList();
		}

		private static List<JObject> ConvertApiCollectiblesToPluginPins(string json)
		{
			return JArray.Parse(json)
				.OfType<JObject>()
				.Select(item => new JObject
				{
					["id"] = ReadDefIndex(item),
					["name"] = item["name"]?.ToString() ?? string.Empty,
					["image"] = item["image"]?.ToString() ?? string.Empty
				})
				.Where(item => !string.IsNullOrWhiteSpace(item["id"]?.ToString()) &&
				               !string.IsNullOrWhiteSpace(item["name"]?.ToString()))
				.ToList();
		}


		private static List<JObject> ConvertApiStickersToPluginStickers(string json)
		{
			return JArray.Parse(json)
				.OfType<JObject>()
				.Select(item =>
				{
					var converted = new JObject
					{
						["id"] = ReadStickerId(item),
						["name"] = item["name"]?.ToString() ?? item["market_hash_name"]?.ToString() ?? string.Empty,
						["image"] = item["image"]?.ToString() ?? string.Empty
					};

					CopyStickerField(item, converted, "tournament_event");
					CopyStickerField(item, converted, "tournament_team");
					CopyStickerField(item, converted, "tournament_player");
					CopyStickerField(item, converted, "team");
					CopyStickerField(item, converted, "team_name");
					CopyStickerField(item, converted, "player");
					CopyStickerField(item, converted, "player_name");
					CopyStickerField(item, converted, "type");

					return converted;
				})
				.Where(item => !string.IsNullOrWhiteSpace(item["id"]?.ToString()) &&
				               !string.IsNullOrWhiteSpace(item["name"]?.ToString()))
				.ToList();
		}

		private static void CopyStickerField(JObject source, JObject target, string fieldName)
		{
			var value = source[fieldName]?.ToString();
			if (!string.IsNullOrWhiteSpace(value))
			{
				target[fieldName] = value;
			}
		}

		private static string ReadStickerId(JObject item)
		{
			var stickerId = item["sticker_id"]?.ToString();
			if (!string.IsNullOrWhiteSpace(stickerId)) return stickerId;

			var defIndex = item["def_index"]?.ToString();
			if (!string.IsNullOrWhiteSpace(defIndex)) return defIndex;

			var id = item["id"]?.ToString();
			if (string.IsNullOrWhiteSpace(id)) return string.Empty;

			var numericPart = new string(id.Where(char.IsDigit).ToArray());
			return string.IsNullOrWhiteSpace(numericPart) ? id : numericPart;
		}

		private static string NormalizeAgentModelPath(string? modelPath)
		{
			if (string.IsNullOrWhiteSpace(modelPath)) return string.Empty;

			var model = modelPath.Trim()
				.Replace("characters/models/", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace("agents/models/", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace(".vmdl", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Trim('/');

			return model;
		}

		private static int? TryReadInt(JToken? token)
		{
			return int.TryParse(token?.ToString(), out var value) ? value : null;
		}

		private static string ReadDefIndex(JObject item)
		{
			var defIndex = item["def_index"]?.ToString();
			if (!string.IsNullOrWhiteSpace(defIndex)) return defIndex;

			var id = item["id"]?.ToString();
			if (string.IsNullOrWhiteSpace(id)) return string.Empty;

			var separatorIndex = id.LastIndexOf('-');
			return separatorIndex >= 0 && separatorIndex + 1 < id.Length ? id[(separatorIndex + 1)..] : id;
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
