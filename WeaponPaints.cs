using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Nexd.MySQL;
using static CounterStrikeSharp.API.Core.Listeners;

namespace WeaponPaints;
public class WeaponPaints : BasePlugin, IPluginConfig<WeaponPaintsConfig>
{
    public override string ModuleName => "WeaponPaints";
    public override string ModuleDescription => "Connector for web-based player chosen wepaon paints.";
    public override string ModuleAuthor => "Nereziel";
    public override string ModuleVersion => "0.7";
    public WeaponPaintsConfig Config { get; set; } = new();

    MySqlDb? MySql = null;
    public DateTime[] commandCooldown = new DateTime[Server.MaxPlayers];
    private Dictionary<ulong, Dictionary<nint, int>> gPlayerWeaponPaints = new Dictionary<ulong, Dictionary<nint, int>>();
    private Dictionary<ulong, Dictionary<nint, int>> gPlayerWeaponSeed = new Dictionary<ulong, Dictionary<nint, int>>();
    private Dictionary<ulong, Dictionary<nint, float>> gPlayerWeaponWear = new Dictionary<ulong, Dictionary<nint, float>>();
    private static Dictionary<string, string> knifeTypes = new Dictionary<string, string>()
    {
        { "m9", "weapon_knife_m9_bayonet" },
        { "karambit", "weapon_knife_karambit" },
        { "bayonet", "weapon_bayonet" },
        { "bowie", "weapon_knife_survival_bowie" },
        { "butterfly", "weapon_knife_butterfly" },
        { "falchion", "weapon_knife_falchion" },
        { "flip", "weapon_knife_flip" },
        { "gut", "weapon_knife_gut" },
        { "tactical", "weapon_knife_tactical" },
        { "shadow", "weapon_knife_push" },
        { "navaja", "weapon_knife_gypsy_jackknife" },
        { "stiletto", "weapon_knife_stiletto" },
        { "talon", "weapon_knife_widowmaker" },
        { "ursus", "weapon_knife_ursus" },
        { "css", "weapon_knife_css" },
        { "paracord", "weapon_knife_cord" },
        { "survival", "weapon_knife_canis" },
        { "nomad", "weapon_knife_outdoor" },
        { "skeleton", "weapon_knife_skeleton" },
        { "default", "weapon_knife" }
    };
    private static List<string> weaponList = new List<string>()
    {
        "weapon_deagle",        "weapon_elite",        "weapon_fiveseven",        "weapon_glock",
        "weapon_ak47",        "weapon_aug",        "weapon_awp",        "weapon_famas",
        "weapon_g3sg1",        "weapon_galilar",        "weapon_m249",        "weapon_m4a1",
        "weapon_mac10",        "weapon_p90",        "weapon_mp5sd",        "weapon_ump45",
        "weapon_xm1014",        "weapon_bizon",        "weapon_mag7",        "weapon_negev",
        "weapon_sawedoff",        "weapon_tec9",        "weapon_hkp2000",        "weapon_mp7",
        "weapon_mp9",        "weapon_nova",        "weapon_p250",        "weapon_scar20",
        "weapon_sg556",        "weapon_ssg08",        "weapon_m4a1_silencer",        "weapon_usp_silencer",
        "weapon_cz75a",        "weapon_revolver",        "weapon_bayonet",        "weapon_knife"
    };

    public override void Load(bool hotReload)
    {
        MySql = new MySqlDb(Config.DatabaseHost!, Config.DatabaseUser!, Config.DatabasePassword!, Config.DatabaseName!, Config.DatabasePort);
        RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
    }
    public void OnConfigParsed(WeaponPaintsConfig config)
    {
        Config = config;
    }
    private void OnClientPutInServer(int playerSlot)
    {
        int slot = playerSlot;
        Server.NextFrame(() =>
        {
            Task.Run(() => GetWeaponPaintsFromDatabase(slot));
        });
    }
    private void OnClientDisconnect(int playerSlot)
    {
        // Clean up after player
    }
    private void OnEntitySpawned(CEntityInstance entity)
    {
        var designerName = entity.DesignerName;
        if (!weaponList.Contains(designerName)) return;
        bool isKnife = false;
        var weapon = new CBasePlayerWeapon(entity.Handle);
        if (designerName.Contains("knife") || designerName.Contains("bayonet"))
        {
            isKnife = true;
        }
        Server.NextFrame(() =>
        {
            if (!weapon.IsValid) return;
            if (weapon.OwnerEntity.Value == null) return;
            if (!weapon.OwnerEntity.Value.EntityIndex.HasValue) return;
            int weaponOwner = (int)weapon.OwnerEntity.Value.EntityIndex.Value.Value;
            var pawn = new CBasePlayerPawn(NativeAPI.GetEntityFromIndex(weaponOwner));
            if (!pawn.IsValid) return;
            var playerIndex = (int)pawn.Controller.Value.EntityIndex!.Value.Value;
            var player = Utilities.GetPlayerFromIndex(playerIndex);
            if (player == null || !player.IsValid || player.IsBot) return;
            var steamId = new SteamID(player.SteamID);
            if (!gPlayerWeaponPaints.ContainsKey(steamId.SteamId64)) return;
            if (!gPlayerWeaponPaints[steamId.SteamId64].ContainsKey(weapon.AttributeManager.Item.ItemDefinitionIndex)) return;
            weapon.AttributeManager.Item.ItemIDLow = unchecked((uint)-1);
            weapon.AttributeManager.Item.ItemIDHigh = unchecked((uint)-1);
            weapon.FallbackPaintKit = gPlayerWeaponPaints[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex];
            weapon.FallbackSeed = gPlayerWeaponSeed[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex];
            weapon.FallbackWear = gPlayerWeaponWear[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex];
            if (!isKnife && weapon.CBodyComponent != null && weapon.CBodyComponent.SceneNode != null)
            {
                var skeleton = GetSkeletonInstance(weapon.CBodyComponent.SceneNode);
                skeleton.ModelState.MeshGroupMask = 2;
            }
        });
    }
    [ConsoleCommand("css_ws", "weaponskins")]
    public void OnCommandWS(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null) return;
        player.PrintToChat($"Change weapon skins at {ChatColors.Purple}{Config.WebSite}");
        player.PrintToChat($"To synchronize weapon paints type {ChatColors.Purple}!wp");
    }
    [ConsoleCommand("css_wp", "refreshskins")]
    public void OnCommandRefresh(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null) return;
        int playerSlot = (int)player.EntityIndex!.Value.Value - 1;
        if (DateTime.UtcNow >= commandCooldown[playerSlot].AddSeconds(Config.CmdRefreshCooldownSeconds))
        {
            commandCooldown[playerSlot] = DateTime.UtcNow;
            Task.Run(async () => await GetWeaponPaintsFromDatabase(playerSlot));
            player.PrintToChat("Refreshed weapon paints.");
            return;
        }
        player.PrintToChat("You can't refresh weapon paints right now.");
    }
    public CSkeletonInstance GetSkeletonInstance(CGameSceneNode node)
    {
        Func<nint, nint> GetSkeletonInstance = VirtualFunction.Create<nint, nint>(node.Handle, 8);
        return new CSkeletonInstance(GetSkeletonInstance(node.Handle));
    }
    private async Task GetWeaponPaintsFromDatabase(int playerSlot)
    {
        try
        {
            CCSPlayerController player = Utilities.GetPlayerFromSlot(playerSlot);
            if (player == null || !player.IsValid) return;
            var steamId = new SteamID(player.SteamID);

            MySqlQueryCondition conditions = new MySqlQueryCondition()
                 .Add("steamid", "=", steamId.SteamId64.ToString());

            MySqlQueryResult result = await MySql!.Table("wp_player_skins").Where(conditions).SelectAsync();

            result.ToList().ForEach(pair =>
            {
                int WeaponDefIndex = result.Get<int>(pair.Key, "weapon_defindex");
                int PaintId = result.Get<int>(pair.Key, "weapon_paint_id");
                float Wear = result.Get<float>(pair.Key, "weapon_wear");
                int Seed = result.Get<int>(pair.Key, "weapon_seed");

                if (!gPlayerWeaponPaints.ContainsKey(steamId.SteamId64))
                {
                    gPlayerWeaponPaints[steamId.SteamId64] = new Dictionary<nint, int>();
                }
                if (!gPlayerWeaponWear.ContainsKey(steamId.SteamId64))
                {
                    gPlayerWeaponWear[steamId.SteamId64] = new Dictionary<nint, float>();
                }
                if (!gPlayerWeaponSeed.ContainsKey(steamId.SteamId64))
                {
                    gPlayerWeaponSeed[steamId.SteamId64] = new Dictionary<nint, int>();
                }

                gPlayerWeaponPaints[steamId.SteamId64][WeaponDefIndex] = PaintId;
                gPlayerWeaponWear[steamId.SteamId64][WeaponDefIndex] = Wear;
                gPlayerWeaponSeed[steamId.SteamId64][WeaponDefIndex] = Seed;
            });
        }
        catch (Exception)
        {
            return;
        }
    }
}
