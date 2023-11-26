using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;
using Dapper;
using System.Runtime.ExceptionServices;
using System.Reflection;
using CounterStrikeSharp.API.Modules.Cvars;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WeaponPaints;
[MinimumApiVersion(61)]
public class WeaponPaints : BasePlugin, IPluginConfig<WeaponPaintsConfig>
{
	public override string ModuleName => "WeaponPaints";
	public override string ModuleDescription => "Skin and knife selector, standalone and web-based";
	public override string ModuleAuthor => "Nereziel & daffyy";
	public override string ModuleVersion => "1.2a";
	public WeaponPaintsConfig Config { get; set; } = new();

	private string DatabaseConnectionString = string.Empty;
	private Uri GlobalShareApi = new Uri("https://weaponpaints.fun/api.php");

	public bool IsMatchZy = false;
	public int GlobalShareServerId = 0;

	private DateTime[] commandCooldown = new DateTime[Server.MaxPlayers];
	private Dictionary<int, Dictionary<int, int>> gPlayerWeaponPaints = new();
	private Dictionary<int, Dictionary<int, int>> gPlayerWeaponSeed = new();
	private Dictionary<int, Dictionary<int, float>> gPlayerWeaponWear = new();
	private Dictionary<int, string> g_playersKnife = new();
	private List<int> g_changedKnife = new();

	private static List<JObject> skinsList = new List<JObject>();
	private static readonly Dictionary<string, string> weaponList = new()
	{
		{"weapon_deagle", "Desert Eagle"},
		{"weapon_elite", "Dual Berettas"},
		{"weapon_fiveseven", "Five-SeveN"},
		{"weapon_glock", "Glock-18"},
		{"weapon_ak47", "AK-47"},
		{"weapon_aug", "AUG"},
		{"weapon_awp", "AWP"},
		{"weapon_famas", "FAMAS"},
		{"weapon_g3sg1", "G3SG1"},
		{"weapon_galilar", "Galil AR"},
		{"weapon_m249", "M249"},
		{"weapon_m4a1", "M4A1"},
		{"weapon_mac10", "MAC-10"},
		{"weapon_p90", "P90"},
		{"weapon_mp5sd", "MP5-SD"},
		{"weapon_ump45", "UMP-45"},
		{"weapon_xm1014", "XM1014"},
		{"weapon_bizon", "PP-Bizon"},
		{"weapon_mag7", "MAG-7"},
		{"weapon_negev", "Negev"},
		{"weapon_sawedoff", "Sawed-Off"},
		{"weapon_tec9", "Tec-9"},
		{"weapon_hkp2000", "P2000"},
		{"weapon_mp7", "MP7"},
		{"weapon_mp9", "MP9"},
		{"weapon_nova", "Nova"},
		{"weapon_p250", "P250"},
		{"weapon_scar20", "SCAR-20"},
		{"weapon_sg556", "SG 553"},
		{"weapon_ssg08", "SSG 08"},
		{"weapon_m4a1_silencer", "M4A1-S"},
		{"weapon_usp_silencer", "USP-S"},
		{"weapon_cz75a", "CZ75-Auto"},
		{"weapon_revolver", "R8 Revolver"},
		{ "weapon_knife", "Default Knife" },
		{ "weapon_knife_m9_bayonet", "M9 Bayonet" },
		{ "weapon_knife_karambit", "Karambit" },
		{ "weapon_bayonet", "Bayonet" },
		{ "weapon_knife_survival_bowie", "Bowie Knife" },
		{ "weapon_knife_butterfly", "Butterfly Knife" },
		{ "weapon_knife_falchion", "Falchion Knife" },
		{ "weapon_knife_flip", "Flip Knife" },
		{ "weapon_knife_gut", "Gut Knife" },
		{ "weapon_knife_tactical", "Huntsman Knife" },
		{ "weapon_knife_push", "Shadow Daggers" },
		{ "weapon_knife_gypsy_jackknife", "Navaja Knife" },
		{ "weapon_knife_stiletto", "Stiletto Knife" },
		{ "weapon_knife_widowmaker", "Talon Knife" },
		{ "weapon_knife_ursus", "Ursus Knife" },
		{ "weapon_knife_css", "Classic Knife" },
		{ "weapon_knife_cord", "Paracord Knife" },
		{ "weapon_knife_canis", "Survival Knife" },
		{ "weapon_knife_outdoor", "Nomad Knife" },
		{ "weapon_knife_skeleton", "Skeleton Knife" }
	};

	public override void Load(bool hotReload)
	{
		if (!Config.GlobalShare)
		{
			BuildDatabaseConnectionString();
			TestDatabaseConnection();
		}
		RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
		RegisterEventHandler<EventItemPurchase>(OnEventItemPurchasePost);
		RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
		RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
		RegisterListener<Listeners.OnMapStart>(OnMapStart);
		RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
		RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Pre);
		RegisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Pre);
		RegisterEventHandler<EventItemRemove>(OnItemRemove);

		if (hotReload)
		{
			OnMapStart(string.Empty);
			Task.Run(async () =>
			{
				for (int i = 1; i <= Server.MaxPlayers; i++)
				{
					if (Config.Additional.SkinEnabled)
						await GetWeaponPaintsFromDatabase(i);

					if (Config.Additional.KnifeEnabled)
						await GetKnifeFromDatabase(i);
				}
			});
		}

		if (Config.Additional.KnifeEnabled)
			SetupKnifeMenu();
		if (Config.Additional.SkinEnabled)
			SetupSkinsMenu();

		RegisterCommands();

		LoadSkinsFromFile(ModuleDirectory + "/skins.json");
	}

	private HookResult OnItemRemove(EventItemRemove @event, GameEventInfo info)
	{
		Console.WriteLine(@event.Defindex);
		return HookResult.Continue;
	}

	public void OnConfigParsed(WeaponPaintsConfig config)
	{
		if (!config.GlobalShare)
		{
			if (config.DatabaseHost.Length < 1 || config.DatabaseName.Length < 1 || config.DatabaseUser.Length < 1)
			{
				throw new Exception("[WeaponPaints] You need to setup Database credentials in config!");
			}
		}

		Config = config;
		Utility.Config = config;
		Utility.ShowAd(ModuleVersion);
	}
	private void BuildDatabaseConnectionString()
	{
		var builder = new MySqlConnectionStringBuilder
		{
			Server = Config.DatabaseHost,
			UserID = Config.DatabaseUser,
			Password = Config.DatabasePassword,
			Database = Config.DatabaseName,
			Port = (uint)Config.DatabasePort,
		};

		DatabaseConnectionString = builder.ConnectionString;
	}

	private void TestDatabaseConnection()
	{
		try
		{
			using var connection = new MySqlConnection(DatabaseConnectionString);
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

	async private void CheckDatabaseTables()
	{
		try
		{
			using var connection = new MySqlConnection(DatabaseConnectionString);
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
	// TODO: fix for map which change mp_t_default_melee
	/*private HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info)
	{
		NativeAPI.IssueServerCommand("mp_t_default_melee \"\"");
		NativeAPI.IssueServerCommand("mp_ct_default_melee \"\"");
		return HookResult.Continue;
	}
	*/
	public override void Unload(bool hotReload)
	{
		base.Unload(hotReload);
	}
	private void RegisterCommands()
	{
		AddCommand($"css_{Config.Additional.CommandSkin}", "Skins info", (player, info) =>
		{
			if (!Utility.IsPlayerValid(player)) return;
			OnCommandWS(player, info);
		});
		AddCommand($"css_{Config.Additional.CommandRefresh}", "Skins refresh", (player, info) =>
		{
			if (!Utility.IsPlayerValid(player)) return;
			OnCommandRefresh(player, info);
		});
		if (Config.Additional.CommandKillEnabled)
		{
			AddCommand($"css_{Config.Additional.CommandKill}", "kill yourself", (player, info) =>
			{
				if (!Utility.IsPlayerValid(player) || !player!.PlayerPawn.IsValid) return;

				player.PlayerPawn.Value.CommitSuicide(true, false);
			});
		}
	}
	private void IncompatibilityCheck()
	{
		// MatchZy
		if (Directory.Exists(Path.GetDirectoryName(ModuleDirectory) + "/MatchZy"))
		{
			Console.WriteLine("[WeaponPaints] Incompatibility found: MatchZy");
			IsMatchZy = true;
		}
	}

	private void OnMapStart(string mapName)
	{
		if (!Config.Additional.KnifeEnabled) return;
		// TODO
		// needed for now
		AddTimer(2.0f, () =>
		{
			NativeAPI.IssueServerCommand("mp_t_default_melee \"\"");
			NativeAPI.IssueServerCommand("mp_ct_default_melee \"\"");
			IncompatibilityCheck();
		});
		if (Config.GlobalShare)
			GlobalShareConnect();
	}

	private void GlobalShareConnect()
	{
		if (!Config.GlobalShare) return;

		var values = new Dictionary<string, string>
			{
			   { "server_address", $"{ConVar.Find("ip")!.StringValue}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>().ToString()}" },
			   { "server_hostname", ConVar.Find("hostname")!.StringValue }
			};

		using (var httpClient = new HttpClient())
		{
			httpClient.BaseAddress = GlobalShareApi;
			var formContent = new FormUrlEncodedContent(values);

			Task<HttpResponseMessage> responseTask = httpClient.PostAsync("", formContent);
			responseTask.Wait();
			HttpResponseMessage response = responseTask.Result;

			if (response.IsSuccessStatusCode)
			{
				Task<string> responseBodyTask = response.Content.ReadAsStringAsync();
				responseBodyTask.Wait();
				string responseBody = responseBodyTask.Result;
				GlobalShareServerId = Int32.Parse(responseBody);
			}
			else
			{
				throw new Exception("[WeaponPaints] Unable to retrieve serverid from GlobalShare!");
			}
		}
		Console.WriteLine("[WeaponPaints] GlobalShare ONLINE");
	}

	private void OnClientPutInServer(int playerSlot)
	{
		int playerIndex = playerSlot + 1;
		Task.Run(async () =>
		{
			if (Config.Additional.KnifeEnabled)
				await GetKnifeFromDatabase(playerIndex);
			if (Config.Additional.SkinEnabled)
				await GetWeaponPaintsFromDatabase(playerIndex);
		});
	}
	private void OnClientDisconnect(int playerSlot)
	{
		CCSPlayerController player = Utilities.GetPlayerFromSlot(playerSlot);
		if (!Utility.IsPlayerValid(player)) return;
		if (Config.Additional.KnifeEnabled)
			g_playersKnife.Remove((int)player.EntityIndex!.Value.Value);
		if (Config.Additional.SkinEnabled)
			gPlayerWeaponPaints.Remove((int)player.EntityIndex!.Value.Value);
	}

	private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;
		if (player == null || !player.IsValid || !player.PlayerPawn.IsValid)
		{
			return HookResult.Continue;
		}

		if (Config.Additional.KnifeEnabled)
		{
			if (!PlayerHasKnife(player))
				GiveKnifeToPlayer(player);
		}

		if (Config.Additional.SkinVisibilityFix)
		{
			AddTimer(0.3f, () => RefreshSkins(player));
		}

		return HookResult.Continue;
	}
	private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
	{
		/*
		if (!IsMatchZy) return HookResult.Continue;
		*/

		NativeAPI.IssueServerCommand("mp_t_default_melee \"\"");
		NativeAPI.IssueServerCommand("mp_ct_default_melee \"\"");

		return HookResult.Continue;
	}
	private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
	{
		if (@event.Defindex == 42 || @event.Defindex == 59)
		{
			CCSPlayerController? player = @event.Userid;
			if (!Utility.IsPlayerValid(player) || !player.PawnIsAlive) return HookResult.Continue;

			if (g_playersKnife.ContainsKey((int)player.EntityIndex!.Value.Value)
				&&
			   g_playersKnife[(int)player.EntityIndex!.Value.Value] != "weapon_knife")
			{
				if (PlayerHasKnife(player))
					RemoveKnifeFromPlayer(player);

				AddTimer(0.3f, () =>
				{
					if (!PlayerHasKnife(player))
						GiveKnifeToPlayer(player);
				});

				if (Config.Additional.SkinVisibilityFix)
				{
					AddTimer(0.25f, () => RefreshSkins(player));
				}
			}
		}
		return HookResult.Continue;
	}

	private void OnEntitySpawned(CEntityInstance entity)
	{
		if (!Config.Additional.SkinEnabled) return;
		var designerName = entity.DesignerName;
		if (!weaponList.ContainsKey(designerName)) return;
		bool isKnife = false;
		var weapon = new CBasePlayerWeapon(entity.Handle);

		if (designerName.Contains("knife") || designerName.Contains("bayonet"))
		{
			isKnife = true;
		}
		Server.NextFrame(() =>
		{
			try
			{
				if (!weapon.IsValid) return;
				if (weapon.OwnerEntity.Value == null) return;
				if (!weapon.OwnerEntity.Value.EntityIndex.HasValue)
				{
					for (int i = 1; i <= Server.MaxPlayers; i++)
					{
						CCSPlayerController? ghostPlayer = Utilities.GetPlayerFromIndex(i);
						if (!Utility.IsPlayerValid(ghostPlayer)) continue;
						if (g_changedKnife.Contains((int)ghostPlayer.EntityIndex!.Value.Value))
						{
							ChangeWeaponAttributes(weapon, ghostPlayer, isKnife);
							g_changedKnife.Remove((int)ghostPlayer.EntityIndex!.Value.Value);
							break;
						}
					}
					return;
				}

				if (!weapon.OwnerEntity.Value.EntityIndex.HasValue) return;
				int weaponOwner = (int)weapon.OwnerEntity.Value.EntityIndex.Value.Value;
				var pawn = new CBasePlayerPawn(NativeAPI.GetEntityFromIndex(weaponOwner));
				if (!pawn.IsValid) return;
				var playerIndex = (int)pawn.Controller.Value.EntityIndex!.Value.Value;
				var player = Utilities.GetPlayerFromIndex(playerIndex);
				if (!Utility.IsPlayerValid(player)) return;

				// TODO: Remove knife crashes here, needs another solution
				/*if (isKnife && g_playersKnife[(int)player.EntityIndex!.Value.Value] != "weapon_knife" && (weapon.AttributeManager.Item.ItemDefinitionIndex == 42 || weapon.AttributeManager.Item.ItemDefinitionIndex == 59))
                {
                    RemoveKnifeFromPlayer(player);
                    return;
                }*/
				ChangeWeaponAttributes(weapon, player, isKnife);
			}
			catch (Exception) { }
		});
	}
	private void ChangeWeaponAttributes(CBasePlayerWeapon? weapon, CCSPlayerController? player, bool isKnife = false)
	{
		if (weapon == null || !weapon.IsValid || !Utility.IsPlayerValid(player)) return;

		int playerIndex = (int)player!.EntityIndex!.Value.Value;
		if (!gPlayerWeaponPaints.ContainsKey(playerIndex)) return;

		if (isKnife && !g_playersKnife.ContainsKey(playerIndex) || isKnife && g_playersKnife[playerIndex] == "weapon_knife") return;

		if (Config.Additional.GiveRandomSkin &&
			 !gPlayerWeaponPaints[playerIndex].ContainsKey(weapon.AttributeManager.Item.ItemDefinitionIndex))
		{
			// Random skins
			weapon.AttributeManager.Item.ItemID = 16384;
			weapon.AttributeManager.Item.ItemIDLow = 16384 & 0xFFFFFFFF;
			weapon.AttributeManager.Item.ItemIDHigh = weapon.AttributeManager.Item.ItemIDLow >> 32;
			weapon.FallbackPaintKit = GetRandomPaint(weapon.AttributeManager.Item.ItemDefinitionIndex);
			weapon.FallbackSeed = 0;
			weapon.FallbackWear = 0.0f;
			if (!isKnife && weapon.CBodyComponent != null && weapon.CBodyComponent.SceneNode != null)
			{
				var skeleton = GetSkeletonInstance(weapon.CBodyComponent.SceneNode);
				skeleton.ModelState.MeshGroupMask = 2;
			}
			return;
		}

		if (!gPlayerWeaponPaints[playerIndex].ContainsKey(weapon.AttributeManager.Item.ItemDefinitionIndex)) return;
		//Log($"Apply on {weapon.DesignerName}({weapon.AttributeManager.Item.ItemDefinitionIndex}) paint {gPlayerWeaponPaints[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]} seed {gPlayerWeaponSeed[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]} wear {gPlayerWeaponWear[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]}");
		weapon.AttributeManager.Item.ItemID = 16384;
		weapon.AttributeManager.Item.ItemIDLow = 16384 & 0xFFFFFFFF;
		weapon.AttributeManager.Item.ItemIDHigh = weapon.AttributeManager.Item.ItemIDLow >> 32;
		weapon.FallbackPaintKit = gPlayerWeaponPaints[playerIndex][weapon.AttributeManager.Item.ItemDefinitionIndex];
		weapon.FallbackSeed = gPlayerWeaponSeed[playerIndex][weapon.AttributeManager.Item.ItemDefinitionIndex];
		weapon.FallbackWear = gPlayerWeaponWear[playerIndex][weapon.AttributeManager.Item.ItemDefinitionIndex];
		if (!isKnife && weapon.CBodyComponent != null && weapon.CBodyComponent.SceneNode != null)
		{
			var skeleton = GetSkeletonInstance(weapon.CBodyComponent.SceneNode);
			skeleton.ModelState.MeshGroupMask = 2;
		}
	}

	private HookResult OnEventItemPurchasePost(EventItemPurchase @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;

		if (player == null || !player.IsValid) return HookResult.Continue;

		if (Config.Additional.SkinVisibilityFix)
			AddTimer(0.2f, () => RefreshSkins(player));

		return HookResult.Continue;
	}
	private void GiveKnifeToPlayer(CCSPlayerController? player)
	{
		if (!Config.Additional.KnifeEnabled || player == null || !player.IsValid) return;

		if (g_playersKnife.TryGetValue((int)player.EntityIndex!.Value.Value, out var knife))
		{
			player.GiveNamedItem(knife);
		}
		else if (Config.Additional.GiveRandomKnife)
		{
			var knifeTypes = weaponList.Where(pair => pair.Key.StartsWith("weapon_knife") || pair.Key.StartsWith("weapon_bayonet")).ToDictionary(pair => pair.Key, pair => pair.Value);

			Random random = new();
			int index = random.Next(knifeTypes.Count);
			var randomKnifeClass = knifeTypes.Keys.ElementAt(index);

			player.GiveNamedItem(randomKnifeClass);
		}
		else
		{
			var defaultKnife = (CsTeam)player.TeamNum == CsTeam.Terrorist ? "weapon_knife_t" : "weapon_knife";
			player.GiveNamedItem(defaultKnife);
		}
	}
	private void RemoveKnifeFromPlayer(CCSPlayerController? player)
	{
		if (player == null || !player.IsValid || !player.PawnIsAlive) return;
		if (player.PlayerPawn.Value.WeaponServices == null || player.PlayerPawn.Value.ItemServices == null) return;

		var weapons = player.PlayerPawn.Value.WeaponServices.MyWeapons;
		if (weapons != null && weapons.Count > 0)
		{
			CCSPlayer_ItemServices service = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle);
			//var dropWeapon = VirtualFunction.CreateVoid<nint, nint>(service.Handle, GameData.GetOffset("CCSPlayer_ItemServices_DropActivePlayerWeapon"));

			foreach (var weapon in weapons)
			{
				if (weapon != null && weapon.IsValid && weapon.Value.IsValid)
				{
					//if (weapon.Value.AttributeManager.Item.ItemDefinitionIndex == 42 || weapon.Value.AttributeManager.Item.ItemDefinitionIndex == 59)
					if (weapon.Value.DesignerName.Contains("knife") || weapon.Value.DesignerName.Contains("bayonet"))
					{
						NativeAPI.IssueClientCommand((int)player.EntityIndex!.Value.Value - 1, "slot3");
						AddTimer(0.5f, () => service.DropActivePlayerWeapon(weapon.Value));

						/*
						CEntityInstance knife = new(weapon.Value.Handle);
						AddTimer(1.0f, () => {
								knife.Remove();
							if (knife != null && knife.IsValid && player.PawnIsAlive)
						});
						*/

						break;
					}
				}
			}
		}
	}
	/* Causing crashes
	private void RefreshPlayerKnife(CCSPlayerController? player, bool remove = false)
	{
		if (player == null || !player.IsValid || player.IsBot || !player.PawnIsAlive) return;

		AddTimer(0.1f, () =>
		{
			if (remove == true)
			{
				if (PlayerHasKnife(player))
					RemoveKnifeFromPlayer(player);
			}

			GiveKnifeToPlayer(player);
		});

		if (Config.Additional.SkinVisibilityFix)
		{
			AddTimer(0.25f, () => RefreshSkins(player));
		}
	}
	*/
	private void RefreshSkins(CCSPlayerController? player)
	{
		if (!Utility.IsPlayerValid(player) || !player!.PawnIsAlive) return;

		AddTimer(0.18f, () => NativeAPI.IssueClientCommand((int)player.EntityIndex!.Value.Value - 1, "slot3"));
		AddTimer(0.25f, () => NativeAPI.IssueClientCommand((int)player.EntityIndex!.Value.Value - 1, "slot2"));
		AddTimer(0.38f, () => NativeAPI.IssueClientCommand((int)player.EntityIndex!.Value.Value - 1, "slot1"));
	}
	private bool PlayerHasKnife(CCSPlayerController? player)
	{
		if (!Config.Additional.KnifeEnabled) return false;

		if (player == null || !player.IsValid || !player.PawnIsAlive)
		{
			return false;
		}

		var weapons = player.PlayerPawn.Value.WeaponServices!.MyWeapons;
		if (weapons == null || weapons.Count <= 0) return false;
		foreach (var weapon in weapons)
		{
			if (weapon != null && weapon.IsValid && weapon.Value.IsValid)
			{
				if (weapon.Value.DesignerName.Contains("knife") || weapon.Value.DesignerName.Contains("bayonet"))
				{
					return true;
				}
			}
		}
		return false;
	}
	private void SetupKnifeMenu()
	{
		if (!Config.Additional.KnifeEnabled) return;

		var knivesOnly = weaponList
			.Where(pair => pair.Key.StartsWith("weapon_knife") || pair.Key.StartsWith("weapon_bayonet"))
			.ToDictionary(pair => pair.Key, pair => pair.Value);

		var giveItemMenu = new ChatMenu(Utility.ReplaceTags($" {Config.Messages.KnifeMenuTitle}"));
		var handleGive = (CCSPlayerController? player, ChatMenuOption option) =>
		{
			if (Utility.IsPlayerValid(player))
			{
				var knifeName = option.Text;
				var knifeKey = knivesOnly.FirstOrDefault(x => x.Value == knifeName).Key;
				if (!string.IsNullOrEmpty(knifeKey))
				{
					string temp = "";

					if (!string.IsNullOrEmpty(Config.Messages.ChosenKnifeMenu))
					{
						temp = $" {Config.Prefix} {Config.Messages.ChosenKnifeMenu}".Replace("{KNIFE}", knifeName);
						player!.PrintToChat(Utility.ReplaceTags(temp));
					}

					if (!string.IsNullOrEmpty(Config.Messages.ChosenKnifeMenuKill) && Config.Additional.CommandKillEnabled)
					{
						temp = $" {Config.Prefix} {Config.Messages.ChosenKnifeMenuKill}";
						player!.PrintToChat(Utility.ReplaceTags(temp));
					}

					g_playersKnife[(int)player!.EntityIndex!.Value.Value] = knifeKey;

					if (player!.PawnIsAlive)
					{
						if (PlayerHasKnife(player))
						{
							RemoveKnifeFromPlayer(player);
						}

						g_changedKnife.Add((int)player.EntityIndex!.Value.Value);
						GiveKnifeToPlayer(player);
					}

					Task.Run(() => SyncKnifeToDatabase((int)player.EntityIndex!.Value.Value, knifeKey));

				}
			}
		};
		foreach (var knifePair in knivesOnly)
		{
			giveItemMenu.AddMenuOption(knifePair.Value, handleGive);
		}
		AddCommand($"css_{Config.Additional.CommandKnife}", "Knife Menu", (player, info) =>
		{
			if (!Utility.IsPlayerValid(player)) return;
			int playerIndex = (int)player!.EntityIndex!.Value.Value;

			if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CmdRefreshCooldownSeconds) && playerIndex > 0 && playerIndex < commandCooldown.Length)
			{
				commandCooldown[playerIndex] = DateTime.UtcNow;
				ChatMenus.OpenMenu(player, giveItemMenu);
				return;
			}
			if (!string.IsNullOrEmpty(Config.Messages.CooldownRefreshCommand))
			{
				string temp = $" {Config.Prefix} {Config.Messages.CooldownRefreshCommand}";
				player.PrintToChat(Utility.ReplaceTags(temp));
			}
		});
	}

	private void SetupSkinsMenu()
	{
		var classNamesByWeapon = weaponList.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
		var weaponSelectionMenu = new ChatMenu(Utility.ReplaceTags($" {Config.Messages.WeaponMenuTitle}"));

		// Function to handle skin selection for a specific weapon
		var handleWeaponSelection = (CCSPlayerController? player, ChatMenuOption option) =>
		{
			if (!Utility.IsPlayerValid(player)) return;

			int playerIndex = (int)player!.EntityIndex!.Value.Value;
			string selectedWeapon = option.Text;
			if (classNamesByWeapon.TryGetValue(selectedWeapon, out string? selectedWeaponClassname))
			{
				if (selectedWeaponClassname == null) return;
				var skinsForSelectedWeapon = skinsList?.Where(skin =>
				skin != null &&
				skin.TryGetValue("weapon_name", out var weaponName) &&
				weaponName?.ToString() == selectedWeaponClassname
			)?.ToList();

				var skinSubMenu = new ChatMenu(Utility.ReplaceTags($" {Config.Messages.SkinMenuTitle}").Replace("{WEAPON}", selectedWeapon));

				// Function to handle skin selection for the chosen weapon
				var handleSkinSelection = (CCSPlayerController? p, ChatMenuOption opt) =>
				{
					if (p == null || !p.IsValid) return;

					var steamId = new SteamID(player.SteamID);
					var firstSkin = skinsList?.FirstOrDefault(skin =>
					{
						if (skin != null && skin.TryGetValue("weapon_name", out var weaponName))
						{
							return weaponName?.ToString() == selectedWeaponClassname;
						}
						return false;
					});
					string selectedSkin = opt.Text;
					string selectedPaintID = selectedSkin.Split('(')[1].Trim(')').Trim();

					if (firstSkin != null &&
						firstSkin.TryGetValue("weapon_defindex", out var weaponDefIndexObj) &&
						weaponDefIndexObj != null &&
						int.TryParse(weaponDefIndexObj.ToString(), out var weaponDefIndex) &&
						int.TryParse(selectedPaintID, out var paintID))
					{
						string temp = $" {Config.Prefix} {Config.Messages.ChosenSkinMenu}".Replace("{SKIN}", selectedSkin);
						p.PrintToChat(Utility.ReplaceTags(temp));
						gPlayerWeaponPaints[playerIndex][weaponDefIndex] = paintID;
						gPlayerWeaponWear[playerIndex][weaponDefIndex] = 0.0f;
						gPlayerWeaponSeed[playerIndex][weaponDefIndex] = 0;

						Task.Run(async () =>
						{
							await SyncWeaponPaintsToDatabase(player);
						});
					}
				};

				// Add skin options to the submenu for the selected weapon
				if (skinsForSelectedWeapon != null)
				{
					foreach (var skin in skinsForSelectedWeapon.Where(s => s != null))
					{
						if (skin.TryGetValue("paint_name", out var paintNameObj) && skin.TryGetValue("paint", out var paintObj))
						{
							var paintName = paintNameObj?.ToString();
							var paint = paintObj?.ToString();

							if (!string.IsNullOrEmpty(paintName) && !string.IsNullOrEmpty(paint))
							{
								skinSubMenu.AddMenuOption($"{paintName} ({paint})", handleSkinSelection);
							}
						}
					}
				}

				// Open the submenu for skin selection of the chosen weapon
				ChatMenus.OpenMenu(player, skinSubMenu);
			}
		};

		// Add weapon options to the weapon selection menu
		foreach (var weaponClass in weaponList.Keys)
		{
			string weaponName = weaponList[weaponClass];
			weaponSelectionMenu.AddMenuOption(weaponName, handleWeaponSelection);
		}
		// Command to open the weapon selection menu for players
		AddCommand($"css_{Config.Additional.CommandSkinSelection}", "Skins selection menu", (player, info) =>
		{
			if (!Utility.IsPlayerValid(player)) return;
			int playerIndex = (int)player!.EntityIndex!.Value.Value;

			if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CmdRefreshCooldownSeconds) && playerIndex > 0 && playerIndex < commandCooldown.Length)
			{
				commandCooldown[playerIndex] = DateTime.UtcNow;
				ChatMenus.OpenMenu(player, weaponSelectionMenu);
				return;
			}
			if (!string.IsNullOrEmpty(Config.Messages.CooldownRefreshCommand))
			{
				string temp = $"{Config.Prefix} {Config.Messages.CooldownRefreshCommand}";
				player.PrintToChat(Utility.ReplaceTags(temp));
			}

		});
	}

	// [ConsoleCommand($"css_{Config.Additional.CommandRefresh}", "refreshskins")]
	private void OnCommandRefresh(CCSPlayerController? player, CommandInfo command)
	{
		if (!Config.Additional.CommandWpEnabled || !Config.Additional.SkinEnabled) return;
		if (!Utility.IsPlayerValid(player)) return;

		string temp = "";
		int playerIndex = (int)player!.EntityIndex!.Value.Value;
		if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CmdRefreshCooldownSeconds))
		{
			commandCooldown[playerIndex] = DateTime.UtcNow;
			Task.Run(async () => await GetWeaponPaintsFromDatabase(playerIndex));
			if (Config.Additional.KnifeEnabled)
			{
				if (PlayerHasKnife(player))
					RemoveKnifeFromPlayer(player);
				AddTimer(0.3f, () =>
				{
					GiveKnifeToPlayer(player);
				});

				Task.Run(async () => await GetKnifeFromDatabase(playerIndex));
				/*
				RemoveKnifeFromPlayer(player);
				AddTimer(0.2f, () => GiveKnifeToPlayer(player));
				*/
			}
			if (!string.IsNullOrEmpty(Config.Messages.SuccessRefreshCommand))
			{
				temp = $" {Config.Prefix} {Config.Messages.SuccessRefreshCommand}";
				player.PrintToChat(Utility.ReplaceTags(temp));
			}
			return;
		}
		if (!string.IsNullOrEmpty(Config.Messages.CooldownRefreshCommand))
		{
			temp = $" {Config.Prefix} {Config.Messages.CooldownRefreshCommand}";
			player.PrintToChat(Utility.ReplaceTags(temp));
		}
	}
	// [ConsoleCommand($"css_{Config.Additional.CommandSkin}", "weaponskins")]
	private void OnCommandWS(CCSPlayerController? player, CommandInfo command)
	{
		if (!Config.Additional.SkinEnabled) return;
		if (!Utility.IsPlayerValid(player)) return;

		string temp = "";

		if (!string.IsNullOrEmpty(Config.Messages.WebsiteMessageCommand))
		{
			temp = $" {Config.Prefix} {Config.Messages.WebsiteMessageCommand}";
			player!.PrintToChat(Utility.ReplaceTags(temp));
		}
		if (!string.IsNullOrEmpty(Config.Messages.SynchronizeMessageCommand))
		{
			temp = $" {Config.Prefix} {Config.Messages.SynchronizeMessageCommand}";
			player!.PrintToChat(Utility.ReplaceTags(temp));
		}
		if (!Config.Additional.KnifeEnabled) return;
		if (!string.IsNullOrEmpty(Config.Messages.KnifeMessageCommand))
		{
			temp = $" {Config.Prefix} {Config.Messages.KnifeMessageCommand}";
			player!.PrintToChat(Utility.ReplaceTags(temp));
		}
	}
	private static CSkeletonInstance GetSkeletonInstance(CGameSceneNode node)
	{
		Func<nint, nint> GetSkeletonInstance = VirtualFunction.Create<nint, nint>(node.Handle, 8);
		return new CSkeletonInstance(GetSkeletonInstance(node.Handle));
	}
	private async Task GetWeaponPaintsFromDatabase(int playerIndex)
	{
		if (!Config.Additional.SkinEnabled) return;

		CCSPlayerController player = Utilities.GetPlayerFromIndex(playerIndex);
		if (!Utility.IsPlayerValid(player)) return;

		var steamId = new SteamID(player.SteamID);

		gPlayerWeaponPaints[playerIndex] = new Dictionary<int, int>();
		gPlayerWeaponWear[playerIndex] = new Dictionary<int, float>();
		gPlayerWeaponSeed[playerIndex] = new Dictionary<int, int>();

		try
		{
			if (Config.GlobalShare)
			{
				var values = new Dictionary<string, string>
				{
				   { "server_id", GlobalShareServerId.ToString() },
				   { "steamid", steamId.SteamId64.ToString() },
				   { "skins", "1" }
				};
				UriBuilder builder = new UriBuilder(GlobalShareApi);
				builder.Query = string.Join("&", values.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

				using (var httpClient = new HttpClient())
				{
					httpClient.BaseAddress = GlobalShareApi;
					var formContent = new FormUrlEncodedContent(values);
					HttpResponseMessage response = await httpClient.GetAsync(builder.Uri);

					if (response.IsSuccessStatusCode)
					{
						string responseBody = await response.Content.ReadAsStringAsync();
						JArray jsonArray = JArray.Parse(responseBody);
						if (jsonArray != null && jsonArray.Count > 0)
						{
							foreach (var weapon in jsonArray)
							{
								int? weaponDefIndex = weapon["weapon_defindex"]?.Value<int>();
								int? weaponPaintId = weapon["weapon_paint_id"]?.Value<int>();
								float? weaponWear = weapon["weapon_wear"]?.Value<float>();
								int? weaponSeed = weapon["weapon_seed"]?.Value<int>();

								if (weaponDefIndex != null && weaponPaintId != null && weaponWear != null && weaponSeed != null)
								{
									gPlayerWeaponPaints[playerIndex][weaponDefIndex.Value] = weaponPaintId.Value;
									gPlayerWeaponWear[playerIndex][weaponDefIndex.Value] = weaponWear.Value;
									gPlayerWeaponSeed[playerIndex][weaponDefIndex.Value] = weaponSeed.Value;
								}
							}
						}
						return;
					}
					else
					{
						return;
					}
				}
			}

			using (var connection = new MySqlConnection(DatabaseConnectionString))
			{
				await connection.OpenAsync();

				string query = "SELECT * FROM `wp_player_skins` WHERE `steamid` = @steamid";

				IEnumerable<dynamic> PlayerSkins = await connection.QueryAsync<dynamic>(query, new { steamid = steamId.SteamId64.ToString() });

				if (PlayerSkins != null && PlayerSkins.AsList().Count > 0)
				{
					PlayerSkins.ToList().ForEach(row =>
					{
						int weaponDefIndex = row.weapon_defindex ?? default(int);
						int weaponPaintId = row.weapon_paint_id ?? default(int);
						float weaponWear = row.weapon_wear ?? default(float);
						int weaponSeed = row.weapon_seed ?? default(int);

						gPlayerWeaponPaints[playerIndex][weaponDefIndex] = weaponPaintId;
						gPlayerWeaponWear[playerIndex][weaponDefIndex] = weaponWear;
						gPlayerWeaponSeed[playerIndex][weaponDefIndex] = weaponSeed;
					});
				}
				else
				{
					return;
				}
				await connection.CloseAsync();
			}
		}
		catch (Exception e)
		{
			Utility.Log(e.Message);
			return;
		}
	}
	private async Task GetKnifeFromDatabase(int playerIndex)
	{
		if (!Config.Additional.KnifeEnabled) return;
		try
		{
			CCSPlayerController player = Utilities.GetPlayerFromIndex(playerIndex);
			if (!Utility.IsPlayerValid(player)) return;
			var steamId = new SteamID(player.SteamID);

			if (Config.GlobalShare)
			{
				var values = new Dictionary<string, string>
				{
				   { "server_id", GlobalShareServerId.ToString() },
				   { "steamid", steamId.SteamId64.ToString() },
				   { "knife", "1" }
				};
				UriBuilder builder = new UriBuilder(GlobalShareApi);
				builder.Query = string.Join("&", values.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

				using (var httpClient = new HttpClient())
				{
					httpClient.BaseAddress = GlobalShareApi;
					var formContent = new FormUrlEncodedContent(values);
					HttpResponseMessage response = await httpClient.GetAsync(builder.Uri);

					if (response.IsSuccessStatusCode)
					{
						string result = await response.Content.ReadAsStringAsync();
						if (!string.IsNullOrEmpty(result))
						{
							g_playersKnife[playerIndex] = result;
						}
						else
						{
							return;
						}

					}
					else
					{
						return;
					}
				}
				return;
			}

			using (var connection = new MySqlConnection(DatabaseConnectionString))
			{
				await connection.OpenAsync();
				string query = "SELECT `knife` FROM `wp_player_knife` WHERE `steamid` = @steamid";
				string? PlayerKnife = await connection.QueryFirstOrDefaultAsync<string>(query, new { steamid = steamId.SteamId64.ToString() });

				if (PlayerKnife != null)
				{
					g_playersKnife[playerIndex] = PlayerKnife;
				}
				else
				{
					return;
				}
				await connection.CloseAsync();
			}
			//Log($"{player.PlayerName} has this knife -> {g_playersKnife[playerIndex]}");
		}
		catch (Exception e)
		{
			Utility.Log(e.Message);
			return;
		}
	}
	private async Task SyncKnifeToDatabase(int playerIndex, string knife)
	{
		if (!Config.Additional.KnifeEnabled) return;
		try
		{
			CCSPlayerController player = Utilities.GetPlayerFromIndex(playerIndex);
			if (player == null || !player.IsValid) return;
			var steamId = new SteamID(player.SteamID);

			using var connection = new MySqlConnection(DatabaseConnectionString);
			await connection.OpenAsync();
			string query = "INSERT INTO `wp_player_knife` (`steamid`, `knife`) VALUES(@steamid, @newKnife) ON DUPLICATE KEY UPDATE `knife` = @newKnife";
			await connection.ExecuteAsync(query, new { steamid = steamId.SteamId64.ToString(), newKnife = knife });
			await connection.CloseAsync();
		}
		catch (Exception e)
		{
			Utility.Log(e.Message);
			return;
		}
	}

	private async Task SyncWeaponPaintsToDatabase(CCSPlayerController? player)
	{
		if (!Utility.IsPlayerValid(player)) return;

		int playerIndex = (int)player!.EntityIndex!.Value.Value;

		string steamId = new SteamID(player.SteamID).SteamId64.ToString();

		using var connection = new MySqlConnection(DatabaseConnectionString);
		await connection.OpenAsync();

		if (!gPlayerWeaponPaints.ContainsKey(playerIndex))
			return;

		foreach (var weaponDefIndex in gPlayerWeaponPaints[playerIndex].Keys)
		{
			int paintId = gPlayerWeaponPaints[playerIndex][weaponDefIndex];
			float wear = gPlayerWeaponWear.TryGetValue(playerIndex, out var wearDictionary)
				&& wearDictionary.TryGetValue(weaponDefIndex, out var retrievedWear)
				? retrievedWear
				: 0.0f;

			// Assigning values for gPlayerWeaponSeed
			int seed = gPlayerWeaponSeed.TryGetValue(playerIndex, out var seedDictionary)
				&& seedDictionary.TryGetValue(weaponDefIndex, out var retrievedSeed)
				? retrievedSeed
				: 0;

			string updateSql = "UPDATE `wp_player_skins` SET `weapon_paint_id` = @paintId, " +
							   "`weapon_wear` = @wear, `weapon_seed` = @seed WHERE `steamid` = @steamid " +
							   "AND `weapon_defindex` = @weaponDefIndex";

			var updateParams = new { paintId, wear, seed, steamid = steamId, weaponDefIndex };
			int rowsAffected = await connection.ExecuteAsync(updateSql, updateParams);

			if (rowsAffected == 0)
			{
				string insertSql = "INSERT INTO `wp_player_skins` (`steamid`, `weapon_defindex`, " +
								   "`weapon_paint_id`, `weapon_wear`, `weapon_seed`) " +
								   "VALUES (@steamid, @weaponDefIndex, @paintId, @wear, @seed)";

				await connection.ExecuteAsync(insertSql, updateParams);
			}
		}
		await connection.CloseAsync();
	}

	private static int GetRandomPaint(int defindex)
	{
		Random rnd = new Random();

		if (skinsList != null)
		{
			// Filter weapons by the provided defindex
			var filteredWeapons = skinsList.FindAll(w => w["weapon_defindex"]?.ToString() == defindex.ToString());

			if (filteredWeapons.Count > 0)
			{
				var randomWeapon = filteredWeapons[rnd.Next(filteredWeapons.Count)];
				if (int.TryParse(randomWeapon["paint"]?.ToString(), out int paintValue))
				{
					return paintValue;
				}
				else
				{
					return 0;
				}

			}
		}
		return 0;
	}

	private static void LoadSkinsFromFile(string filePath)
	{
		if (File.Exists(filePath))
		{
			string json = File.ReadAllText(filePath);
			var deserializedSkins = JsonConvert.DeserializeObject<List<JObject>>(json);
			skinsList = deserializedSkins ?? new List<JObject>();
		}
		else
		{
			throw new FileNotFoundException("File not found.", filePath);
		}
	}
}
