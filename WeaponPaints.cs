using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;

namespace WeaponPaints;

[MinimumApiVersion(132)]
public partial class WeaponPaints : BasePlugin, IPluginConfig<WeaponPaintsConfig>
{
	internal static readonly Dictionary<string, string> weaponList = new()
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

	internal static WeaponPaintsConfig _config = new WeaponPaintsConfig();
	internal static IStringLocalizer? _localizer;
	internal static Dictionary<int, int> g_knifePickupCount = new Dictionary<int, int>();
	internal static Dictionary<int, string> g_playersKnife = new();
	internal static Dictionary<int, Dictionary<int, WeaponInfo>> gPlayerWeaponsInfo = new Dictionary<int, Dictionary<int, WeaponInfo>>();
	internal static List<JObject> skinsList = new List<JObject>();
	internal static WeaponSynchronization? weaponSync;
	//internal static List<int> g_changedKnife = new();
	internal bool g_bCommandsAllowed = true;

	internal Uri GlobalShareApi = new("https://weaponpaints.fun/api.php");
	internal int GlobalShareServerId = 0;
	internal static Dictionary<int, DateTime> commandsCooldown = new Dictionary<int, DateTime>();
	private string DatabaseConnectionString = string.Empty;
	private CounterStrikeSharp.API.Modules.Timers.Timer? g_hTimerCheckSkinsData = null;
	public static Dictionary<int, string> weaponDefindex { get; } = new Dictionary<int, string>
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
		{ 525, "weapon_knife_skeleton" }
	};

	public WeaponPaintsConfig Config { get; set; } = new();
	public override string ModuleAuthor => "Nereziel & daffyy";
	public override string ModuleDescription => "Skin and knife selector, standalone and web-based";
	public override string ModuleName => "WeaponPaints";
	public override string ModuleVersion => "1.3h";

	public static WeaponPaintsConfig GetWeaponPaintsConfig()
	{
		return _config;
	}

	public override void Load(bool hotReload)
	{
		if (!Config.GlobalShare)
		{
			DatabaseConnectionString = Utility.BuildDatabaseConnectionString();
			Utility.TestDatabaseConnection();
		}

		if (hotReload)
		{
			OnMapStart(string.Empty);

			List<CCSPlayerController> players = Utilities.GetPlayers();

			foreach (CCSPlayerController player in players)
			{
				if (player == null || !player.IsValid || player.IsBot || player.IsHLTV || player.AuthorizedSteamID == null) continue;
				if (gPlayerWeaponsInfo.ContainsKey((int)player.Index)) continue;

				PlayerInfo playerInfo = new PlayerInfo
				{
					UserId = player.UserId,
					Index = (int)player.Index,
					SteamId = player?.AuthorizedSteamID?.SteamId64.ToString(),
					Name = player?.PlayerName,
					IpAddress = player?.IpAddress?.Split(":")[0]
				};

				if (Config.Additional.SkinEnabled && weaponSync != null)
					_ = weaponSync.GetWeaponPaintsFromDatabase(playerInfo);
				if (Config.Additional.KnifeEnabled && weaponSync != null)
					_ = weaponSync.GetKnifeFromDatabase(playerInfo);

				g_knifePickupCount[(int)player!.Index] = 0;
			}
			/*
			RegisterListeners();
			RegisterCommands();
			*/
		}

		if (Config.Additional.KnifeEnabled)
			SetupKnifeMenu();
		if (Config.Additional.SkinEnabled)
			SetupSkinsMenu();

		RegisterListeners();
		RegisterCommands();

		Utility.LoadSkinsFromFile(ModuleDirectory + "/skins.json");
	}

	public void OnConfigParsed(WeaponPaintsConfig config)
	{
		if (!config.GlobalShare)
		{
			if (config.DatabaseHost.Length < 1 || config.DatabaseName.Length < 1 || config.DatabaseUser.Length < 1)
			{
				Logger.LogError("You need to setup Database credentials in config!");
				throw new Exception("[WeaponPaints] You need to setup Database credentials in config!");
			}
		}

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
				Logger.LogError("Unable to retrieve serverid from GlobalShare!");
				throw new Exception("[WeaponPaints] Unable to retrieve serverid from GlobalShare!");
			}
		}

		Logger.LogInformation("GlobalShare ONLINE!");
		Console.WriteLine("[WeaponPaints] GlobalShare ONLINE");
	}
}