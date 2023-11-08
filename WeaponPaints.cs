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
  public override string ModuleVersion => "0.4";
  MySqlDb? MySql = null;
  private Dictionary<ulong, Dictionary<nint, int>> g_playersSkins = new Dictionary<ulong, Dictionary<nint, int>>();

  public override void Load(bool hotReload)
  {
    new Cfg().CheckConfig(ModuleDirectory);
    MySql = new MySqlDb(Cfg.config.DatabaseHost!, Cfg.config.DatabaseUser!, Cfg.config.DatabasePassword!, Cfg.config.DatabaseName!, (int)Cfg.config.DatabasePort);
    RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
    RegisterListener<Listeners.OnClientPutInServer>(OnClientConnect);

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
      if (!weapon.IsValid) return;
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
        }
      }
    });
  }
  private static void Log(string message)
  {
    Console.BackgroundColor = ConsoleColor.DarkGray;
    Console.ForegroundColor = ConsoleColor.DarkMagenta;
    Console.WriteLine(message);
    Console.ResetColor();
  }
  private void OnClientConnect(int playerSlot)
  {
    try
    {
      CCSPlayerController player = Utilities.GetPlayerFromSlot(playerSlot);

      if (player == null || !player.IsValid) return;

      var steamId = new SteamID(player.SteamID);

      MySqlQueryCondition conditions = new MySqlQueryCondition()
           .Add("steamid", "=", steamId.SteamId64.ToString());

      MySqlQueryResult result = MySql!.Table("wp_player_skins").Where(conditions).Select();

      result.ToList().ForEach(pair =>
        {
          int weponId = result.Get<int>(pair.Key, "weapon_defindex");
          int weponPaint = result.Get<int>(pair.Key, "weapon_paint_id");

          if (!g_playersSkins.ContainsKey(steamId.SteamId64))
          {
            g_playersSkins[steamId.SteamId64] = new Dictionary<nint, int>();
          }

          g_playersSkins[steamId.SteamId64][weponId] = weponPaint;
        }
      );
    }
    catch (Exception)
    {
      return;
    }
  }
}
