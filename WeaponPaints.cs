using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;
using Newtonsoft.Json.Linq;

namespace WeaponPaints;
[MinimumApiVersion(90)]
public partial class WeaponPaints : BasePlugin, IPluginConfig<WeaponPaintsConfig>
{
	public override string ModuleName => "WeaponPaints";
	public override string ModuleDescription => "Skin and knife selector, standalone and web-based";
	public override string ModuleAuthor => "Nereziel & daffyy";
	public override string ModuleVersion => "1.3b";
	public WeaponPaintsConfig Config { get; set; } = new();
	internal static WeaponPaintsConfig _config = new WeaponPaintsConfig();

	internal static WeaponSynchronization? weaponSync;

	private CounterStrikeSharp.API.Modules.Timers.Timer? g_hTimerCheckSkinsData = null;

	/*
	private Dictionary<int, Dictionary<int, int>> gPlayerWeaponPaints = new();
	private Dictionary<int, Dictionary<int, int>> gPlayerWeaponSeed = new();
	private Dictionary<int, Dictionary<int, float>> gPlayerWeaponWear = new();
	*/
	private string DatabaseConnectionString = string.Empty;

	internal Uri GlobalShareApi = new Uri("https://weaponpaints.fun/api.php");
	internal int GlobalShareServerId = 0;

	private DateTime[] commandCooldown = new DateTime[Server.MaxPlayers];
	internal static Dictionary<int, Dictionary<int, WeaponInfo>> gPlayerWeaponsInfo = new Dictionary<int, Dictionary<int, WeaponInfo>>();
	internal static Dictionary<int, int> g_knifePickupCount = new Dictionary<int, int>();
	internal static Dictionary<int, string> g_playersKnife = new();
	//internal static List<int> g_changedKnife = new();
	internal bool g_bCommandsAllowed = true;

	internal static List<JObject> skinsList = new List<JObject>();
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

	public override void Load(bool hotReload)
	{
		if (!Config.GlobalShare)
		{
			DatabaseConnectionString = Utility.BuildDatabaseConnectionString();
			Utility.TestDatabaseConnection();
		}

		weaponSync = new WeaponSynchronization(DatabaseConnectionString, Config, GlobalShareApi, GlobalShareServerId);

		if (hotReload)
		{
			OnMapStart(string.Empty);
			Task.Run(async () =>
			{
				for (int i = 1; i <= Server.MaxPlayers; i++)
				{
					if (Config.Additional.SkinEnabled && weaponSync != null)
						await weaponSync.GetWeaponPaintsFromDatabase(i);

					if (Config.Additional.KnifeEnabled && weaponSync != null)
						await weaponSync.GetKnifeFromDatabase(i);
				}
			});
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
				throw new Exception("[WeaponPaints] You need to setup Database credentials in config!");
			}
		}

		Config = config;
		_config = config;
		Utility.Config = config;
		Utility.ShowAd(ModuleVersion);
	}

	public static WeaponPaintsConfig GetWeaponPaintsConfig()
	{
		return _config;
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
}