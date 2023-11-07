using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using Nexd.MySQL;

namespace WeaponPaints;
public class WeaponPaints : BasePlugin
{
    public override string ModuleName => "WeaponPaints";
    public override string ModuleDescription => "Connector for web-based player chosen wepaon paints.";
    public override string ModuleAuthor => "Nereziel";
    public override string ModuleVersion => "0.3";
    MySqlDb? MySql = null;

    public override void Load(bool hotReload)
    {
        new Cfg().CheckConfig(ModuleDirectory);
        MySql = new MySqlDb(Cfg.config.DatabaseHost!, Cfg.config.DatabaseUser!, Cfg.config.DatabasePassword!, Cfg.config.DatabaseName!, (int)Cfg.config.DatabasePort);
        RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        var designerName = entity.DesignerName;

        if (!designerName.Contains("weapon_")) return;
        if (designerName.Contains("knife")) return;

        var weapon = new CBasePlayerWeapon(entity.Handle);
        if (!weapon.IsValid) return;
        var pawn = new CBasePlayerPawn(NativeAPI.GetEntityFromIndex((int)weapon.OwnerEntity.Value.EntityIndex!.Value.Value));
        var playerIndex = (int)pawn.Controller.Value.EntityIndex!.Value.Value;

        int weaponPaint = GetPlayersWeaponPaint(playerIndex, weapon.AttributeManager.Item.ItemDefinitionIndex);
        if (weaponPaint == 0) return;
        weapon.AttributeManager.Item.ItemIDLow = unchecked((uint)-1);
        weapon.AttributeManager.Item.ItemIDHigh = unchecked((uint)-1);
        weapon.FallbackPaintKit = weaponPaint;
        weapon.FallbackSeed = 0;
        weapon.FallbackWear = 0.0001f;
    }
    private static void Log(string message)
    {
        Console.BackgroundColor = ConsoleColor.DarkGray;
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    public int GetPlayersWeaponPaint(int playerSlot, int weaponDefIndex)
    {
        try
        {
            CCSPlayerController player = Utilities.GetPlayerFromSlot(playerSlot);
            if (player == null || !player.IsValid)
                return 0;

            var steamId = new SteamID(player.SteamID);

            MySqlQueryCondition conditions = new MySqlQueryCondition()
                .Add("steamid", "=", steamId.SteamId64.ToString())
                .Add("weapon_defindex", "=", weaponDefIndex);

            MySqlQueryResult result = MySql!.Table("wp_player_skins").Where(conditions).Select();
            int weaponPaint = result.Get<int>(0, "weapon_paint_id");
            return weaponPaint;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}