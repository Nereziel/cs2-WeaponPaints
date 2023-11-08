using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using Nexd.MySQL;

namespace WeaponPaints;
public class WeaponPaints : BasePlugin
{
    public override string ModuleName => "WeaponPaints";
    public override string ModuleDescription => "Connector for web-based player chosen wepaon paints.";
    public override string ModuleAuthor => "Nereziel";
    public override string ModuleVersion => "0.5";
    MySqlDb? MySql = null;
    public DateTime[] commandCooldown = new DateTime[Server.MaxPlayers];
    private Dictionary<ulong, Dictionary<nint, int>> g_playersSkins = new Dictionary<ulong, Dictionary<nint, int>>();

    public override void Load(bool hotReload)
    {
        new Cfg().CheckConfig(ModuleDirectory);
        MySql = new MySqlDb(Cfg.config.DatabaseHost!, Cfg.config.DatabaseUser!, Cfg.config.DatabasePassword!, Cfg.config.DatabaseName!, (int)Cfg.config.DatabasePort);
        RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        RegisterListener<Listeners.OnClientConnect>(OnClientConnect);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
    }
    private void OnClientConnect(int playerSlot, string name, string ipAddress)
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
        if (!designerName.Contains("weapon_")) return;
        if (designerName.Contains("knife")) return;
        if (designerName.Contains("bayonet")) return;
        var weapon = new CBasePlayerWeapon(entity.Handle);
        Server.NextFrame(() =>
        {
            if (!weapon.IsValid || !weapon.OwnerEntity.IsValid) return;
            var pawn = new CBasePlayerPawn(NativeAPI.GetEntityFromIndex((int)weapon.OwnerEntity.Value.EntityIndex!.Value.Value));
            if (!pawn.IsValid) return;
            var playerIndex = (int)pawn.Controller.Value.EntityIndex!.Value.Value;
            CCSPlayerController player = Utilities.GetPlayerFromIndex(playerIndex);
            if (player == null || !player.IsValid) return;
            var steamId = new SteamID(player.SteamID);
            if (g_playersSkins.TryGetValue(steamId.SteamId64, out var weaponIDs))
            {
                if (weaponIDs.TryGetValue(weapon.AttributeManager.Item.ItemDefinitionIndex, out var weaponPaint))
                {
                    weapon.AttributeManager.Item.ItemIDLow = unchecked((uint)-1);
                    weapon.AttributeManager.Item.ItemIDHigh = unchecked((uint)-1);
                    weapon.FallbackPaintKit = weaponPaint;
                    weapon.FallbackSeed = 0;
                    weapon.FallbackWear = 0.0001f;
                    if (weapon.AttributeManager.Item.AccountID > 0 && weapon.CBodyComponent != null && weapon.CBodyComponent.SceneNode != null)
                    {
                        var skeleton = GetSkeletonInstance(weapon.CBodyComponent.SceneNode);
                        skeleton.ModelState.MeshGroupMask = 2;
                    }
                }
            }
        });
    }
    [ConsoleCommand("css_wp", "refreshskins")]
    public void OnCommandRefresh(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null) return;
        int playerSlot = (int)player.EntityIndex!.Value.Value - 1;
        if (DateTime.UtcNow >= commandCooldown[playerSlot].AddMinutes(2))
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
                int weaponId = result.Get<int>(pair.Key, "weapon_defindex");
                int weaponPaint = result.Get<int>(pair.Key, "weapon_paint_id");

                if (!g_playersSkins.ContainsKey(steamId.SteamId64))
                {
                    g_playersSkins[steamId.SteamId64] = new Dictionary<nint, int>();
                }

                g_playersSkins[steamId.SteamId64][weaponId] = weaponPaint;
            });
        }
        catch (Exception)
        {
            return;
        }
    }
}