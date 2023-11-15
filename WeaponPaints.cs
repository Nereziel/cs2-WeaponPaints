using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Nexd.MySQL;
using System.Runtime.ExceptionServices;
using static CounterStrikeSharp.API.Core.Listeners;
using System.Reflection;


namespace WeaponPaints;
[MinimumApiVersion(52)]
public class WeaponPaints : BasePlugin, IPluginConfig<WeaponPaintsConfig>
{
    public override string ModuleName => "WeaponPaints";
    public override string ModuleDescription => "Connector for web-based player chosen wepaon paints.";
    public override string ModuleAuthor => "Nereziel";
    public override string ModuleVersion => "0.8";

    public WeaponPaintsConfig Config { get; set; } = new();

    MySqlDb? MySql = null;
    private DateTime[] commandCooldown = new DateTime[Server.MaxPlayers];
    private Dictionary<ulong, Dictionary<nint, int>> gPlayerWeaponPaints = new();
    private Dictionary<ulong, Dictionary<nint, int>> gPlayerWeaponSeed = new();
    private Dictionary<ulong, Dictionary<nint, float>> gPlayerWeaponWear = new();
    private Dictionary<int, string> g_playersKnife = new();
    private static readonly Dictionary<string, string> knifeTypes = new()
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
    private static readonly List<string> weaponList = new()
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
        base.Load(hotReload);
        SetGlobalExceptionHandler();
        MySql = new MySqlDb(Config.DatabaseHost, Config.DatabaseUser, Config.DatabasePassword, Config.DatabaseName!, Config.DatabasePort);
        RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        //RegisterEventHandler<EventRoundPrestart>(OnRoundPreStart);
        if (Config.Additional.KnifeEnabled)
            SetupMenus();
    }
    public void OnConfigParsed(WeaponPaintsConfig config)
    {
        if (config.DatabaseHost.Length < 1 || config.DatabaseName.Length < 1 || config.DatabaseUser.Length < 1)
        {
            throw new Exception("You need to setup Database credentials in config!");
        }

        Config = config;
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
        RemoveGlobalExceptionHandler();
        base.Unload(hotReload);
    }
    private void GlobalExceptionHandler(object? sender, FirstChanceExceptionEventArgs @event)
    {
        Log(@event.Exception.ToString());
    }
    private void SetGlobalExceptionHandler()
    {
        AppDomain.CurrentDomain.FirstChanceException += this.GlobalExceptionHandler;
    }
    private void RemoveGlobalExceptionHandler()
    {
        AppDomain.CurrentDomain.FirstChanceException -= this.GlobalExceptionHandler;
    }
    private void OnMapStart(string mapName)
    {
        if (!Config.Additional.KnifeEnabled) return;
        // TODO
        // needed for now
        base.AddTimer(2.0f, () => {
            NativeAPI.IssueServerCommand("mp_t_default_melee \"\"");
            NativeAPI.IssueServerCommand("mp_ct_default_melee \"\"");
        });
    }

    private void OnClientPutInServer(int playerSlot)
    {
        int playerIndex = playerSlot + 1;
        Task.Run(async () =>
        {   
            if (Config.Additional.KnifeEnabled)
                await GetKnifeFromDatabase(playerIndex);
            await GetWeaponPaintsFromDatabase(playerIndex);
        });
    }
    private void OnClientDisconnect(int playerSlot)
    {
        // TODO: Clean up after player
        if (Config.Additional.KnifeEnabled)
            g_playersKnife.Remove(playerSlot+1);
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (!player.IsValid || !player.PlayerPawn.IsValid)
        {
            return HookResult.Continue;
        }

        if (Config.Additional.KnifeEnabled)
            GiveKnifeToPlayer(player);

        if (Config.Additional.SkinVisibilityFix)
        {
            // Check the best slot and set it. Weird solution but works xD
            AddTimer(0.1f, () => NativeAPI.IssueClientCommand((int)player.EntityIndex!.Value.Value - 1, "slot3"));
            AddTimer(0.25f, () => NativeAPI.IssueClientCommand((int)player.EntityIndex!.Value.Value - 1, "slot2"));
            AddTimer(0.35f, () => NativeAPI.IssueClientCommand((int)player.EntityIndex!.Value.Value - 1, "slot1"));
        }

        return HookResult.Continue;
    }
    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (!Config.Additional.SkinEnabled) return;
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
            // TODO: Remove knife crashes here, needs another solution
            /*if (isKnife && g_playersKnife[(int)player.EntityIndex!.Value.Value] != "weapon_knife" && (weapon.AttributeManager.Item.ItemDefinitionIndex == 42 || weapon.AttributeManager.Item.ItemDefinitionIndex == 59))
            {
                RemoveKnifeFromPlayer(player);
                return;
            }*/
            var steamId = new SteamID(player.SteamID);
            if (!gPlayerWeaponPaints.ContainsKey(steamId.SteamId64)) return;
            if (!gPlayerWeaponPaints[steamId.SteamId64].ContainsKey(weapon.AttributeManager.Item.ItemDefinitionIndex)) return;
            //Log($"Apply on {weapon.DesignerName}({weapon.AttributeManager.Item.ItemDefinitionIndex}) paint {gPlayerWeaponPaints[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]} seed {gPlayerWeaponSeed[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]} wear {gPlayerWeaponWear[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]}");
            weapon.AttributeManager.Item.ItemID = 16384;
            weapon.AttributeManager.Item.ItemIDLow = 16384 & 0xFFFFFFFF;
            weapon.AttributeManager.Item.ItemIDHigh = weapon.AttributeManager.Item.ItemIDLow >> 32;
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
    public void GiveKnifeToPlayer(CCSPlayerController player)
    {
        if (!Config.Additional.KnifeEnabled) return;
        if (player.IsBot) 
        { 
            player.GiveNamedItem((CsTeam)player.TeamNum == CsTeam.Terrorist ? "weapon_knife_t" : "weapon_knife");
            return;
        }

        if (!PlayerHasKnife(player))
        {
            if (g_playersKnife.TryGetValue((int)player.EntityIndex!.Value.Value, out var knife))
            {
                player.GiveNamedItem(knife);
            }
            else
            {
                player.GiveNamedItem((CsTeam)player.TeamNum == CsTeam.Terrorist ? "weapon_knife_t" : "weapon_knife");
            }
        }
    }
    public void RemoveKnifeFromPlayer(CCSPlayerController player)
    {
        if (!Config.Additional.KnifeEnabled) return;
        if (!g_playersKnife.ContainsKey((int)player.EntityIndex!.Value.Value)) return;
        var weapons = player.PlayerPawn.Value.WeaponServices!.MyWeapons;
        foreach (var weapon in weapons)
        {
            if (weapon.IsValid && weapon.Value.IsValid)
            {
                //if (weapon.Value.AttributeManager.Item.ItemDefinitionIndex == 42 || weapon.Value.AttributeManager.Item.ItemDefinitionIndex == 59)
                if (weapon.Value.DesignerName.Contains("knife"))
                {
                    weapon.Value.Remove();
                    break;
                }
            }
        }
    }
    public bool PlayerHasKnife(CCSPlayerController player)
    {
        if (!Config.Additional.KnifeEnabled) return true;
        var weapons = player.PlayerPawn.Value.WeaponServices!.MyWeapons;
        foreach (var weapon in weapons)
        {
            if (weapon.IsValid && weapon.Value.IsValid)
            {
                if (weapon.Value.DesignerName.Contains("knife"))
                {
                    return true;
                }
            }
        }
        return false;
    }
    private void SetupMenus()
    {
        if (!Config.Additional.KnifeEnabled) return;
        var giveItemMenu = new ChatMenu(ReplaceTags(Config.Messages.KnifeMenuTitle));
        var handleGive = (CCSPlayerController player, ChatMenuOption option) =>
        {
            if (knifeTypes.TryGetValue(option.Text, out var knife))
            {
                Task.Run(() => SyncKnifeToDatabase((int)player.EntityIndex!.Value.Value, knife));
                g_playersKnife[(int)player.EntityIndex!.Value.Value] = knifeTypes[option.Text];
                if (!string.IsNullOrEmpty(Config.Messages.ChosenKnifeMenu)) {
                    string temp = $"{Config.Prefix} {Config.Messages.ChosenKnifeMenu}".Replace("{KNIFE}", option.Text);
                    player.PrintToChat(ReplaceTags(temp));
                }
                RemoveKnifeFromPlayer(player);
                GiveKnifeToPlayer(player);
            }
        };
        foreach (var knife in knifeTypes)
        {
            giveItemMenu.AddMenuOption(knife.Key, handleGive);
        }
        AddCommand("css_knife", "Knife Menu", (player, info) => { if (player == null) return; ChatMenus.OpenMenu(player, giveItemMenu); });
    }
    [ConsoleCommand("css_wp", "refreshskins")]
    public void OnCommandRefresh(CCSPlayerController? player, CommandInfo command)
    {
        if (!Config.Additional.CommandWpEnabled || !Config.Additional.SkinEnabled) return;
        if (player == null) return;
        string temp = "";
        int playerIndex = (int)player.EntityIndex!.Value.Value;
        if (DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CmdRefreshCooldownSeconds))
        {
            commandCooldown[playerIndex] = DateTime.UtcNow;
            Task.Run(async () => await GetWeaponPaintsFromDatabase(playerIndex));
            if (Config.Additional.KnifeEnabled)
                Task.Run(async () => await GetKnifeFromDatabase(playerIndex));
            if (!string.IsNullOrEmpty(Config.Messages.SuccessRefreshCommand)) {
                temp = $"{Config.Prefix} {Config.Messages.SuccessRefreshCommand}";
                player.PrintToChat(ReplaceTags(temp));
            }
            return;
        }
        if (!string.IsNullOrEmpty(Config.Messages.CooldownRefreshCommand)) {
            temp = $"{Config.Prefix} {Config.Messages.CooldownRefreshCommand}";
            player.PrintToChat(ReplaceTags(temp));
        }
    }
    [ConsoleCommand("css_ws", "weaponskins")]
    public void OnCommandWS(CCSPlayerController? player, CommandInfo command)
    {
        if (!Config.Additional.SkinEnabled) return;
        if (player == null) return;

        string temp = "";

        if (!string.IsNullOrEmpty(Config.Messages.WebsiteMessageCommand)) {
            temp = $"{Config.Prefix} {Config.Messages.WebsiteMessageCommand}";
            player.PrintToChat(ReplaceTags(temp));
        }
        if (!string.IsNullOrEmpty(Config.Messages.SynchronizeMessageCommand)) {
            temp = $"{Config.Prefix} {Config.Messages.SynchronizeMessageCommand}";
            player.PrintToChat(ReplaceTags(temp));
        }
        if (!Config.Additional.KnifeEnabled) return;
        if (!string.IsNullOrEmpty(Config.Messages.KnifeMessageCommand)) {
            temp = $"{Config.Prefix} {Config.Messages.KnifeMessageCommand}";
            player.PrintToChat(ReplaceTags(temp));
        }
    }
    public static CSkeletonInstance GetSkeletonInstance(CGameSceneNode node)
    {
        Func<nint, nint> GetSkeletonInstance = VirtualFunction.Create<nint, nint>(node.Handle, 8);
        return new CSkeletonInstance(GetSkeletonInstance(node.Handle));
    }
    private async Task GetWeaponPaintsFromDatabase(int playerIndex)
    {
        if (!Config.Additional.SkinEnabled) return;
        try
        {
            CCSPlayerController player = Utilities.GetPlayerFromIndex(playerIndex);
            if (player == null || !player.IsValid) return;
            var steamId = new SteamID(player.SteamID);

            MySqlQueryCondition conditions = new MySqlQueryCondition()
                 .Add("steamid", "=", steamId.SteamId64.ToString());

            MySqlQueryResult result = await MySql!.Table("wp_player_skins").Where(conditions).SelectAsync();
            if (result.Rows < 1) return;
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
        catch (Exception e)
        {
            Log(e.Message);
            return;
        }
    }
    private async Task GetKnifeFromDatabase(int playerIndex)
    {
        if (!Config.Additional.KnifeEnabled) return;
        try
        {
            CCSPlayerController player = Utilities.GetPlayerFromIndex(playerIndex);
            if (player == null || !player.IsValid) return;
            var steamId = new SteamID(player.SteamID);
            MySqlQueryCondition conditions = new MySqlQueryCondition()
                 .Add("steamid", "=", steamId.SteamId64.ToString());

            MySqlQueryResult result = await MySql!.Table("wp_player_knife").Where(conditions).SelectAsync();

            if (result.Rows < 1)
            {
                //g_playersKnife[playerIndex] = "weapon_knife";
                return;
            }

            string knife = result.Get<string>(0, "knife");
            if (knife != null)
            {
                g_playersKnife[playerIndex] = knife;
            }
            //Log($"{player.PlayerName} has this knife -> {g_playersKnife[playerIndex]}");
        }
        catch (Exception e)
        {
            Log(e.Message);
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
            await MySql!.ExecuteNonQueryAsync($"INSERT INTO `wp_player_knife` (`steamid`, `knife`) VALUES('{steamId.SteamId64}', '{knife}') ON DUPLICATE KEY UPDATE `knife` = '{knife}';");
        }
        catch (Exception e)
        {
            Log(e.Message);
            return;
        }
    }

    private string ReplaceTags(string message)
    {
        if (message.Contains('{'))
        {
            string modifiedValue = message;
            modifiedValue = modifiedValue.Replace("{WEBSITE}", Config.Website);
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

    private static void Log(string message)
    {
        Console.BackgroundColor = ConsoleColor.DarkGray;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
