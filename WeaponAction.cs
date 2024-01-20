using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace WeaponPaints
{
	public partial class WeaponPaints
	{
		internal static void ChangeWeaponAttributes(CBasePlayerWeapon? weapon, CCSPlayerController? player, bool isKnife = false)
		{
			if (player == null || weapon == null || !weapon.IsValid || !Utility.IsPlayerValid(player)) return;

			int playerIndex = (int)player.Index;

			if (!gPlayerWeaponsInfo.ContainsKey(playerIndex)) return;

			if (isKnife && !g_playersKnife.ContainsKey(playerIndex) || isKnife && g_playersKnife[playerIndex] == "weapon_knife") return;

			int weaponDefIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;

			if (isKnife)
			{
				weapon.AttributeManager.Item.EntityQuality = 3;
				if (weapon.CBodyComponent != null && weapon.CBodyComponent.SceneNode != null)
				{
					var skeleton = GetSkeletonInstance(weapon.CBodyComponent.SceneNode);
					skeleton.ForceParentToBeNetworked = true;
				}
			}

			if (_config.Additional.GiveRandomSkin &&
				 !gPlayerWeaponsInfo[playerIndex].ContainsKey(weaponDefIndex))
			{
				// Random skins
				weapon.AttributeManager.Item.ItemID = 16384;
				weapon.AttributeManager.Item.ItemIDLow = 16384 & 0xFFFFFFFF;
				weapon.AttributeManager.Item.ItemIDHigh = weapon.AttributeManager.Item.ItemIDLow >> 32;
				weapon.FallbackPaintKit = GetRandomPaint(weaponDefIndex);
				weapon.FallbackSeed = 0;
				weapon.FallbackWear = 0.000001f;
				if (!isKnife && weapon.CBodyComponent != null && weapon.CBodyComponent.SceneNode != null)
				{
					var skeleton = GetSkeletonInstance(weapon.CBodyComponent.SceneNode);
					//skeleton.ModelState.MeshGroupMask = 2;
					skeleton.ForceParentToBeNetworked = true;
				}
				return;
			}

			if (!gPlayerWeaponsInfo[playerIndex].ContainsKey(weaponDefIndex)) return;
			WeaponInfo weaponInfo = gPlayerWeaponsInfo[playerIndex][weaponDefIndex];
			//Log($"Apply on {weapon.DesignerName}({weapon.AttributeManager.Item.ItemDefinitionIndex}) paint {gPlayerWeaponPaints[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]} seed {gPlayerWeaponSeed[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]} wear {gPlayerWeaponWear[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]}");
			weapon.AttributeManager.Item.ItemID = 16384;
			weapon.AttributeManager.Item.ItemIDLow = 16384 & 0xFFFFFFFF;
			weapon.AttributeManager.Item.ItemIDHigh = weapon.AttributeManager.Item.ItemIDLow >> 32;
			weapon.FallbackPaintKit = weaponInfo.Paint;
			weapon.FallbackSeed = weaponInfo.Seed;
			weapon.FallbackWear = weaponInfo.Wear;

			if (!isKnife && weapon.CBodyComponent != null && weapon.CBodyComponent.SceneNode != null)
			{
				var skeleton = GetSkeletonInstance(weapon.CBodyComponent.SceneNode);
				skeleton.ForceParentToBeNetworked = true;
				//skeleton.ModelState.MeshGroupMask = 2;
			}
		}

		internal static void GiveKnifeToPlayer(CCSPlayerController? player)
		{
			if (!_config.Additional.KnifeEnabled || player == null || !player.IsValid) return;

			if (g_playersKnife.TryGetValue((int)player.Index, out var knife))
			{
				player.GiveNamedItem(knife);
			}
			else if (_config.Additional.GiveRandomKnife)
			{
				var knifeTypes = weaponList.Where(pair => pair.Key.StartsWith("weapon_knife") || pair.Key.StartsWith("weapon_bayonet")).ToDictionary(pair => pair.Key, pair => pair.Value);

				Random random = new();
				int index = random.Next(knifeTypes.Count);
				var randomKnifeClass = knifeTypes.Keys.ElementAt(index);

				player.GiveNamedItem(randomKnifeClass);
			}
			else
			{
				var defaultKnife = (CsTeam)player.TeamNum == CsTeam.Terrorist ? "weapon_knife_t" : "weapon_knife";
				player.GiveNamedItem(defaultKnife);
			}
		}

		internal static bool PlayerHasKnife(CCSPlayerController? player)
		{
			if (!_config.Additional.KnifeEnabled) return false;

			if (player == null || !player.IsValid || player.PlayerPawn == null || !player.PlayerPawn.IsValid || !player.PawnIsAlive)
			{
				return false;
			}

			if (player.PlayerPawn?.Value == null || player.PlayerPawn?.Value.WeaponServices == null || player.PlayerPawn?.Value.ItemServices == null)
				return false;

			var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
			if (weapons == null) return false;
			foreach (var weapon in weapons)
			{
				if (weapon != null && weapon.IsValid && weapon.Value != null && weapon.Value.IsValid)
				{
					if (weapon.Value.DesignerName.Contains("knife") || weapon.Value.DesignerName.Contains("bayonet"))
					{
						return true;
					}
				}
			}
			return false;
		}

		internal void RefreshPlayerKnife(CCSPlayerController? player)
		{
			if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PawnIsAlive) return;
			if (player.PlayerPawn.Value.WeaponServices == null || player.PlayerPawn.Value.ItemServices == null) return;

			var weapons = player.PlayerPawn.Value.WeaponServices.MyWeapons;
			if (weapons != null && weapons.Count > 0)
			{
				CCSPlayer_ItemServices service = new(player.PlayerPawn.Value.ItemServices.Handle);
				//var dropWeapon = VirtualFunction.CreateVoid<nint, nint>(service.Handle, GameData.GetOffset("CCSPlayer_ItemServices_DropActivePlayerWeapon"));

				foreach (var weapon in weapons)
				{
					if (weapon != null && weapon.IsValid && weapon.Value != null && weapon.Value.IsValid)
					{
						//if (weapon.Value.AttributeManager.Item.ItemDefinitionIndex == 42 || weapon.Value.AttributeManager.Item.ItemDefinitionIndex == 59)
						if (weapon.Value.DesignerName.Contains("knife") || weapon.Value.DesignerName.Contains("bayonet"))
						{
							if (weapon.Index <= 0) return;
							int weaponEntityIndex = (int)weapon.Index;
							NativeAPI.IssueClientCommand((int)player.Index - 1, "slot3");
							AddTimer(0.22f, () =>
							{
								if (player.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value!.DesignerName.Contains("knife")
								||
								player.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value!.DesignerName.Contains("bayonet")
								)
								{
									if (player.PawnIsAlive)
									{
										NativeAPI.IssueClientCommand((int)player.Index - 1, "slot3");
										service.DropActivePlayerWeapon(weapon.Value);
										GiveKnifeToPlayer(player);
									}
								}
							});

							Task.Delay(TimeSpan.FromSeconds(3.5)).ContinueWith(_ =>
							{
								try
								{
									CEntityInstance? knife = Utilities.GetEntityFromIndex<CEntityInstance>(weaponEntityIndex);

									if (knife != null && knife.IsValid && knife.Handle != -1 && knife.Index > 0)
									{
										knife.Remove();
									}
								}
								catch (Exception) { }
							});

							break;
						}
					}
				}
			}
		}

		internal void RefreshSkins(CCSPlayerController? player)
		{
			return;
			if (!Utility.IsPlayerValid(player) || !player!.PawnIsAlive) return;

			AddTimer(0.18f, () => NativeAPI.IssueClientCommand((int)player.Index - 1, "slot3"));
			AddTimer(0.25f, () => NativeAPI.IssueClientCommand((int)player.Index - 1, "slot2"));
			AddTimer(0.38f, () => NativeAPI.IssueClientCommand((int)player.Index - 1, "slot1"));
		}

		internal void RefreshWeapons(CCSPlayerController? player)
		{
			if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PawnIsAlive) return;
			if (player.PlayerPawn.Value.WeaponServices == null || player.PlayerPawn.Value.ItemServices == null) return;

			var weapons = player.PlayerPawn.Value.WeaponServices.MyWeapons;
			if (weapons != null && weapons.Count > 0)
			{
				CCSPlayer_ItemServices service = new(player.PlayerPawn.Value.ItemServices.Handle);

				foreach (var weapon in weapons)
				{
					if (weapon != null && weapon.IsValid && weapon.Value != null && weapon.Value.IsValid)
					{
						if (weapon.Index <= 0 || !weapon.Value.DesignerName.Contains("weapon_")) continue;
						//if (weapon.Value.AttributeManager.Item.ItemDefinitionIndex == 42 || weapon.Value.AttributeManager.Item.ItemDefinitionIndex == 59)
						try
						{
							if (weapon.Value.DesignerName.Contains("knife") || weapon.Value.DesignerName.Contains("bayonet"))
							{
								player.RemoveItemByDesignerName(weapon.Value.DesignerName, true);
								GiveKnifeToPlayer(player);
							}
							else
							{
								if (!weaponDefindex.ContainsKey(weapon.Value.AttributeManager.Item.ItemDefinitionIndex)) continue;
								int clip1, reservedAmmo;

								clip1 = weapon.Value.Clip1;
								reservedAmmo = weapon.Value.ReserveAmmo[0];

								string weaponByDefindex = weaponDefindex[weapon.Value.AttributeManager.Item.ItemDefinitionIndex];
								player.RemoveItemByDesignerName(weapon.Value.DesignerName, true);
								CBasePlayerWeapon newWeapon = new(player.GiveNamedItem(weaponByDefindex));

								Server.NextFrame(() =>
								{
									if (newWeapon == null) return;
									try
									{
										newWeapon.Clip1 = clip1;
										newWeapon.ReserveAmmo[0] = reservedAmmo;
									}
									catch (Exception)
									{ }
								});
							}
						}
						catch (Exception ex)
						{
							Logger.LogWarning("Refreshing weapons exception");
							Console.WriteLine("[WeaponPaints] Refreshing weapons exception");
							Console.WriteLine(ex.Message);
						}
					}
				}

				/*
				if (Config.Additional.SkinVisibilityFix)
					RefreshSkins(player);
				*/
			}
		}

		internal void RemovePlayerKnife(CCSPlayerController? player, bool force = false)
		{
			if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PawnIsAlive) return;
			if (player.PlayerPawn.Value.WeaponServices == null || player.PlayerPawn.Value.ItemServices == null) return;

			var weapons = player.PlayerPawn.Value.WeaponServices.MyWeapons;
			if (weapons != null && weapons.Count > 0)
			{
				CCSPlayer_ItemServices service = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle);
				//var dropWeapon = VirtualFunction.CreateVoid<nint, nint>(service.Handle, GameData.GetOffset("CCSPlayer_ItemServices_DropActivePlayerWeapon"));

				foreach (var weapon in weapons)
				{
					if (weapon != null && weapon.IsValid && weapon.Value != null && weapon.Value.IsValid)
					{
						//if (weapon.Value.AttributeManager.Item.ItemDefinitionIndex == 42 || weapon.Value.AttributeManager.Item.ItemDefinitionIndex == 59)
						if (weapon.Value.DesignerName.Contains("knife") || weapon.Value.DesignerName.Contains("bayonet"))
						{
							if (!force)
							{
								if ((int)weapon.Index <= 0) return;
								int weaponEntityIndex = (int)weapon.Index;
								NativeAPI.IssueClientCommand((int)player.Index - 1, "slot3");
								AddTimer(0.35f, () => service.DropActivePlayerWeapon(weapon.Value));

								AddTimer(1.0f, () =>
								{
									CEntityInstance? knife = Utilities.GetEntityFromIndex<CEntityInstance>(weaponEntityIndex);
									if (knife != null && knife.IsValid)
									{
										knife.Remove();
									}
								});
							}
							else
							{
								weapon.Value.Remove();
							}

							break;
						}
					}
				}
			}
		}
		private static int GetRandomPaint(int defindex)
		{

			if (skinsList != null)
			{
				Random rnd = new Random();
				// Filter weapons by the provided defindex
				var filteredWeapons = skinsList.FindAll(w => w["weapon_defindex"]?.ToString() == defindex.ToString());

				if (filteredWeapons.Count > 0)
				{
					var randomWeapon = filteredWeapons[rnd.Next(filteredWeapons.Count)];
					if (int.TryParse(randomWeapon["paint"]?.ToString(), out int paintValue))
					{
						return paintValue;
					}
					else
					{
						return 0;
					}
				}
			}
			return 0;
		}

		private static CSkeletonInstance GetSkeletonInstance(CGameSceneNode node)
		{
			Func<nint, nint> GetSkeletonInstance = VirtualFunction.Create<nint, nint>(node.Handle, 8);
			return new CSkeletonInstance(GetSkeletonInstance(node.Handle));
		}
	}
}