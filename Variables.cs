using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using MenuManager;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;

namespace WeaponPaints;

public partial class WeaponPaints
{
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

	public static IStringLocalizer? _localizer;
	internal static readonly ConcurrentDictionary<int, ConcurrentDictionary<CsTeam, string>> GPlayersKnife = new();
	internal static readonly ConcurrentDictionary<int, ConcurrentDictionary<CsTeam, ushort>> GPlayersGlove = new();
	internal static readonly ConcurrentDictionary<int, ConcurrentDictionary<CsTeam, ushort>> GPlayersMusic = new();
	internal static readonly ConcurrentDictionary<int, ConcurrentDictionary<CsTeam, ushort>> GPlayersPin = new();
	internal static readonly ConcurrentDictionary<int, (string? CT, string? T)> GPlayersAgent = new();
	internal static readonly ConcurrentDictionary<int, ConcurrentDictionary<CsTeam, ConcurrentDictionary<int, WeaponInfo>>> GPlayerWeaponsInfo = new();
	internal static List<JObject> SkinsList = [];
	internal static List<JObject> PinsList = [];
	internal static List<JObject> GlovesList = [];
	internal static List<JObject> AgentsList = [];
	internal static List<JObject> MusicList = [];
	internal static WeaponSynchronization? WeaponSync;
	private static bool _gBCommandsAllowed = true;
	private readonly Dictionary<int, string> _playerWeaponImage = new();

	private static readonly Dictionary<int, DateTime> CommandsCooldown = new();
	internal static Database? Database;

	private static readonly MemoryFunctionVoid<nint, string, float> CAttributeListSetOrAddAttributeValueByName = new(GameData.GetSignature("CAttributeList_SetOrAddAttributeValueByName"));
	
	//we dont need anymore because we use AcceptInput
	//private static readonly MemoryFunctionWithReturn<nint, string, int, int> SetBodygroupFunc = new(
	//	GameData.GetSignature("CBaseModelEntity_SetBodygroup"));

	//private static readonly Func<nint, string, int, int> SetBodygroup = SetBodygroupFunc.Invoke;

	private static Dictionary<int, string> WeaponDefindex { get; } = new()
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

	private const ulong MinimumCustomItemId = 65578;
	private ulong _nextItemId = MinimumCustomItemId;
	private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

	private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, float>> _temporaryPlayerWeaponWear = new();
	
	internal static IMenuApi? MenuApi;
	private static readonly PluginCapability<IMenuApi> MenuCapability = new("menu:nfcore");
	
	private int _fadeSeed;
}