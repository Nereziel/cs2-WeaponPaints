using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace WeaponPaints;

[MinimumApiVersion(215)]
public partial class WeaponPaints : BasePlugin, IPluginConfig<WeaponPaintsConfig>
{
	internal static WeaponPaints Instance { get; private set; } = new();

	private static readonly Dictionary<string, string> WeaponList = new()
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
		{"weapon_taser", "Zeus x27"},
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
		{ "weapon_knife_skeleton", "Skeleton Knife" },
		{ "weapon_knife_kukri", "Kukri Knife" }
	};

	private static WeaponPaintsConfig _config = new();
	internal static IStringLocalizer? _localizer;
	private static Dictionary<int, int> g_knifePickupCount = new();
	internal static ConcurrentDictionary<int, string> g_playersKnife = new();
	internal static ConcurrentDictionary<int, ushort> g_playersGlove = new();
	internal static ConcurrentDictionary<int, ushort> g_playersMusic = new();
	internal static ConcurrentDictionary<int, (string? CT, string? T)> g_playersAgent = new();
	internal static ConcurrentDictionary<int, ConcurrentDictionary<int, WeaponInfo>> gPlayerWeaponsInfo = new();
	internal static List<JObject> skinsList = new();
	internal static List<JObject> glovesList = new();
	internal static List<JObject> agentsList = new();
	internal static List<JObject> musicList = new();
	internal static WeaponSynchronization? weaponSync;
	private static bool g_bCommandsAllowed = true;
	private Dictionary<int, string> PlayerWeaponImage = new();

	private static Dictionary<int, DateTime> commandsCooldown = new();
	internal static Database? _database;

	private static readonly MemoryFunctionVoid<nint, string, float> CAttributeListSetOrAddAttributeValueByName = new(GameData.GetSignature("CAttributeList_SetOrAddAttributeValueByName"));

	private static readonly MemoryFunctionVoid<CBaseModelEntity, string, ulong> CBaseModelEntitySetBodygroup =
		new(GameData.GetSignature("CBaseModelEntity_SetBodygroup"));

	private static Dictionary<int, string> WeaponDefindex { get; } = new Dictionary<int, string>
	{
		{ 1, "weapon_deagle" },
		{ 2, "weapon_elite" },
		{ 3, "weapon_fiveseven" },
		{ 4, "weapon_glock" },
		{ 7, "weapon_ak47" },
		{ 8, "weapon_aug" },
		{ 9, "weapon_awp" },
		{ 10, "weapon_famas" },
		{ 11, "weapon_g3sg1" },
		{ 13, "weapon_galilar" },
		{ 14, "weapon_m249" },
		{ 16, "weapon_m4a1" },
		{ 17, "weapon_mac10" },
		{ 19, "weapon_p90" },
		{ 23, "weapon_mp5sd" },
		{ 24, "weapon_ump45" },
		{ 25, "weapon_xm1014" },
		{ 26, "weapon_bizon" },
		{ 27, "weapon_mag7" },
		{ 28, "weapon_negev" },
		{ 29, "weapon_sawedoff" },
		{ 30, "weapon_tec9" },
		{ 31, "weapon_taser" },
		{ 32, "weapon_hkp2000" },
		{ 33, "weapon_mp7" },
		{ 34, "weapon_mp9" },
		{ 35, "weapon_nova" },
		{ 36, "weapon_p250" },
		{ 38, "weapon_scar20" },
		{ 39, "weapon_sg556" },
		{ 40, "weapon_ssg08" },
		{ 60, "weapon_m4a1_silencer" },
		{ 61, "weapon_usp_silencer" },
		{ 63, "weapon_cz75a" },
		{ 64, "weapon_revolver" },
		{ 500, "weapon_bayonet" },
		{ 503, "weapon_knife_css" },
		{ 505, "weapon_knife_flip" },
		{ 506, "weapon_knife_gut" },
		{ 507, "weapon_knife_karambit" },
		{ 508, "weapon_knife_m9_bayonet" },
		{ 509, "weapon_knife_tactical" },
		{ 512, "weapon_knife_falchion" },
		{ 514, "weapon_knife_survival_bowie" },
		{ 515, "weapon_knife_butterfly" },
		{ 516, "weapon_knife_push" },
		{ 517, "weapon_knife_cord" },
		{ 518, "weapon_knife_canis" },
		{ 519, "weapon_knife_ursus" },
		{ 520, "weapon_knife_gypsy_jackknife" },
		{ 521, "weapon_knife_outdoor" },
		{ 522, "weapon_knife_stiletto" },
		{ 523, "weapon_knife_widowmaker" },
		{ 525, "weapon_knife_skeleton" },
		{ 526, "weapon_knife_kukri" }
	};

	public WeaponPaintsConfig Config { get; set; } = new();
	public override string ModuleAuthor => "Nereziel & daffyy";
	public override string ModuleDescription => "Skin, gloves, agents and knife selector, standalone and web-based";
	public override string ModuleName => "WeaponPaints";
	public override string ModuleVersion => "2.4e";

	public static WeaponPaintsConfig GetWeaponPaintsConfig()
	{
		return _config;
	}

	public override void Load(bool hotReload)
	{
		Instance = this;

		if (hotReload)
		{
			OnMapStart(string.Empty);

			foreach (var player in Enumerable.OfType<CCSPlayerController>(Utilities.GetPlayers().TakeWhile(player => weaponSync != null)).Where(player => player.IsValid && player.SteamID.ToString().Length == 17 && !string.IsNullOrEmpty(player.IpAddress) && player is { IsBot: false, IsHLTV: false, Connected: PlayerConnectedState.PlayerConnected }))
			{
				g_knifePickupCount[player.Slot] = 0;
				gPlayerWeaponsInfo.TryRemove(player.Slot, out _);
				g_playersKnife.TryRemove(player.Slot, out _);
				g_playersGlove.TryRemove(player.Slot, out _);
				g_playersAgent.TryRemove(player.Slot, out _);

				PlayerInfo? playerInfo = new PlayerInfo
				{
					UserId = player.UserId,
					Slot = player.Slot,
					Index = (int)player.Index,
					SteamId = player?.SteamID.ToString(),
					Name = player?.PlayerName,
					IpAddress = player?.IpAddress?.Split(":")[0]
				};

				_ = Task.Run(async () =>
				{
					if (weaponSync != null) await weaponSync.GetPlayerData(playerInfo);
				});
			}
		}

		Utility.LoadSkinsFromFile(ModuleDirectory + "/skins.json", Logger);
		Utility.LoadGlovesFromFile(ModuleDirectory + "/gloves.json", Logger);
		Utility.LoadAgentsFromFile(ModuleDirectory + "/agents.json", Logger);
		Utility.LoadMusicFromFile(ModuleDirectory + "/music.json", Logger);

		if (Config.Additional.KnifeEnabled)
			SetupKnifeMenu();
		if (Config.Additional.SkinEnabled)
			SetupSkinsMenu();
		if (Config.Additional.GloveEnabled)
			SetupGlovesMenu();
		if (Config.Additional.AgentEnabled)
			SetupAgentsMenu();
		if (Config.Additional.MusicEnabled)
			SetupMusicMenu();

		RegisterListeners();
		RegisterCommands();
	}

	public void OnConfigParsed(WeaponPaintsConfig config)
	{
		if (config.DatabaseHost.Length < 1 || config.DatabaseName.Length < 1 || config.DatabaseUser.Length < 1)
		{
			Logger.LogError("You need to setup Database credentials in config!");
			Unload(false);
			//throw new Exception("[WeaponPaints] You need to setup Database credentials in config!");
		}

		var builder = new MySqlConnectionStringBuilder
		{
			Server = config.DatabaseHost,
			UserID = config.DatabaseUser,
			Password = config.DatabasePassword,
			Database = config.DatabaseName,
			Port = (uint)config.DatabasePort,
			Pooling = true,
			MaximumPoolSize = 640,
			ConnectionReset = false
		};

		_database = new Database(builder.ConnectionString);

		_ = Utility.CheckDatabaseTables();

		Config = config;
		_config = config;
		_localizer = Localizer;

		Utility.Config = config;
		Utility.ShowAd(ModuleVersion);
		Task.Run(async () => await Utility.CheckVersion(ModuleVersion, Logger));
	}

	public override void Unload(bool hotReload)
	{
		base.Unload(hotReload);
	}
}