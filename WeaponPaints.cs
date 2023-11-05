using CounterStrikeSharp.API.Core;
using MySqlConnector;
using Nexd.MySQL;

namespace WeaponPaints;
public class WeaponPaints : BasePlugin
{
    public override string ModuleName => "WeaponPaints";
    public override string ModuleDescription => "Connector for web-based player chosen wepaon paints.";
    public override string ModuleAuthor => "Nereziel";
    public override string ModuleVersion => "0.2";
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
        if (weapon.AttributeManager.Item.AccountID < 0) return;
        
        //Log($"AccountID {weapon.AttributeManager.Item.AccountID}");
        //Log($"playerSteam {playerId}");
        var playerId = ConvertToSteam64(weapon.AttributeManager.Item.AccountID);
        int weaponPaint = GetPlayersWeaponPaint(playerId.ToString(), weapon.AttributeManager.Item.ItemDefinitionIndex);
        if (playerId == 0) return;
        if (weaponPaint == 0) return;
        weapon.AttributeManager.Item.AccountID = unchecked((uint)271098320);
        weapon.AttributeManager.Item.ItemIDLow = unchecked((uint)-1);
        weapon.AttributeManager.Item.ItemIDHigh = unchecked((uint)-1);
        weapon.FallbackPaintKit = weaponPaint;
        weapon.FallbackSeed = 0;
        weapon.FallbackWear = 0.0001f;
    }
    private Int64 ConvertToSteam64(uint id)
    {
        uint account_type = id % 2;
        uint account_id = (id - account_type) / 2;
        return 76561197960265728L + (account_id * 2) + account_type;
    }
    private static void Log(string message)
    {
        Console.BackgroundColor = ConsoleColor.DarkGray;
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    public int GetPlayersWeaponPaint(string steamId, int weaponDefIndex)
    {
        try
        {
            MySqlQueryCondition conditions = new MySqlQueryCondition()
                .Add("steamid", "=", steamId)
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