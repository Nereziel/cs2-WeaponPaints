﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace WeaponPaints
{
	public partial class WeaponPaints
	{
		internal static void ChangeWeaponAttributes(CBasePlayerWeapon? weapon, CCSPlayerController? player, bool isKnife = false)
		{
			if (player is null || weapon is null || !weapon.IsValid || !Utility.IsPlayerValid(player)) return;

			if (!gPlayerWeaponsInfo.ContainsKey(player.Slot)) return;

			if (isKnife && !g_playersKnife.ContainsKey(player.Slot) || isKnife && g_playersKnife[player.Slot] == "weapon_knife") return;

			if (isKnife)
			{
				var newDefIndex = WeaponDefindex.FirstOrDefault(x => x.Value == g_playersKnife[player.Slot]);
				if (newDefIndex.Key == 0) return;

				if (weapon.AttributeManager.Item.ItemDefinitionIndex != newDefIndex.Key)
				{
					SubclassChange(weapon, (ushort)newDefIndex.Key);
				}

				weapon.AttributeManager.Item.ItemDefinitionIndex = (ushort)newDefIndex.Key;
				weapon.AttributeManager.Item.EntityQuality = 3;
			}

			int weaponDefIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;
			int fallbackPaintKit = 0;

			if (_config.Additional.GiveRandomSkin &&
				 !gPlayerWeaponsInfo[player.Slot].ContainsKey(weaponDefIndex))
			{
				// Random skins
				weapon.AttributeManager.Item.ItemID = 16384;
				weapon.AttributeManager.Item.ItemIDLow = 16384 & 0xFFFFFFFF;
				weapon.AttributeManager.Item.ItemIDHigh = weapon.AttributeManager.Item.ItemIDLow >> 32;
				weapon.FallbackPaintKit = GetRandomPaint(weaponDefIndex);
				weapon.FallbackSeed = 0;
				weapon.FallbackWear = 0.000001f;
				CAttributeList_SetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "set item texture prefab", weapon.FallbackPaintKit);

				fallbackPaintKit = weapon.FallbackPaintKit;

				if (fallbackPaintKit == 0)
					return;

				var foundSkin = skinsList.FirstOrDefault(skin =>
					((int?)skin?["weapon_defindex"] ?? 0) == weaponDefIndex &&
					((int?)skin?["paint"] ?? 0) == fallbackPaintKit &&
					skin?["paint_name"] != null
				);
				/*
								string skinName = foundSkin?["paint_name"]?.ToString() ?? "";
								if (!string.IsNullOrEmpty(skinName))
									new SchemaString<CEconItemView>(weapon.AttributeManager.Item, "m_szCustomName").Set(skinName);
				*/
				if (!isKnife && weapon.CBodyComponent != null && weapon.CBodyComponent.SceneNode != null)
				{
					var skeleton = GetSkeletonInstance(weapon.CBodyComponent.SceneNode);

					int[] newPaints = { 1171, 1170, 1169, 1164, 1162, 1161, 1159, 1175, 1174, 1167, 1165, 1168, 1163, 1160, 1166, 1173 };

					if (newPaints.Contains(fallbackPaintKit))
					{
						skeleton.ModelState.MeshGroupMask = 1;
					}
					else
					{
						if (skeleton.ModelState.MeshGroupMask != 2)
						{
							skeleton.ModelState.MeshGroupMask = 2;
						}
					}
				}

				var viewModels = GetPlayerViewModels(player);
				if (viewModels == null || viewModels.Length == 0)
					return;

				var viewModel = viewModels[0];
				if (viewModel == null || viewModel.Value == null || viewModel.Value.Weapon == null || viewModel.Value.Weapon.Value == null)
					return;

				Utilities.SetStateChanged(viewModel.Value, "CBaseEntity", "m_CBodyComponent");

				return;
			}

			if (!gPlayerWeaponsInfo[player.Slot].ContainsKey(weaponDefIndex)) return;
			WeaponInfo weaponInfo = gPlayerWeaponsInfo[player.Slot][weaponDefIndex];
			//Log($"Apply on {weapon.DesignerName}({weapon.AttributeManager.Item.ItemDefinitionIndex}) paint {gPlayerWeaponPaints[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]} seed {gPlayerWeaponSeed[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]} wear {gPlayerWeaponWear[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]}");
			weapon.AttributeManager.Item.ItemID = 16384;
			weapon.AttributeManager.Item.ItemIDLow = 16384 & 0xFFFFFFFF;
			weapon.AttributeManager.Item.ItemIDHigh = weapon.AttributeManager.Item.ItemIDLow >> 32;
			weapon.FallbackPaintKit = weaponInfo.Paint;
			weapon.FallbackSeed = weaponInfo.Seed;
			weapon.FallbackWear = weaponInfo.Wear;
			CAttributeList_SetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "set item texture prefab", weapon.FallbackPaintKit);

			fallbackPaintKit = weapon.FallbackPaintKit;

			if (fallbackPaintKit == 0)
				return;

			var foundSkin1 = skinsList.FirstOrDefault(skin =>
			   ((int?)skin?["weapon_defindex"] ?? 0) == weaponDefIndex &&
			   ((int?)skin?["paint"] ?? 0) == fallbackPaintKit &&
			   skin?["paint_name"] != null
		   );

			/*
			var skinName1 = foundSkin1?["paint_name"]?.ToString() ?? "";
			if (!string.IsNullOrEmpty(skinName1))
				new SchemaString<CEconItemView>(weapon.AttributeManager.Item, "m_szCustomName").Set(skinName1);
			*/
			if (!isKnife && weapon.CBodyComponent != null && weapon.CBodyComponent.SceneNode != null)
			{
				var skeleton = GetSkeletonInstance(weapon.CBodyComponent.SceneNode);
				int[] newPaints = { 1171, 1170, 1169, 1164, 1162, 1161, 1159, 1175, 1174, 1167, 1165, 1168, 1163, 1160, 1166, 1173 };
				if (newPaints.Contains(fallbackPaintKit))
				{
					skeleton.ModelState.MeshGroupMask = 1;
				}
				else
				{
					if (skeleton.ModelState.MeshGroupMask != 2)
					{
						skeleton.ModelState.MeshGroupMask = 2;
					}
				}
			}

			var viewModels1 = GetPlayerViewModels(player);
			if (viewModels1 == null || viewModels1.Length == 0)
				return;

			var viewModel1 = viewModels1[0];
			if (viewModel1 == null || viewModel1.Value == null || viewModel1.Value.Weapon == null || viewModel1.Value.Weapon.Value == null)
				return;

			Utilities.SetStateChanged(viewModel1.Value, "CBaseEntity", "m_CBodyComponent");
		}

		internal static void GiveKnifeToPlayer(CCSPlayerController? player)
		{
			if (!_config.Additional.KnifeEnabled || player == null || !player.IsValid) return;

			if (PlayerHasKnife(player)) return;

			string knifeToGive;
			if (g_playersKnife.TryGetValue(player.Slot, out var knife))
			{
				knifeToGive = knife;
			}
			else if (_config.Additional.GiveRandomKnife)
			{
				var knifeTypes = weaponList.Where(pair => pair.Key.StartsWith("weapon_knife") || pair.Key.StartsWith("weapon_bayonet")).ToList();

				if (knifeTypes.Count == 0)
				{
					Utility.Log("No valid knife types found.");
					return;
				}

				Random random = new();
				int index = random.Next(knifeTypes.Count);
				knifeToGive = knifeTypes[index].Key;
			}
			else
			{
				knifeToGive = (CsTeam)player.TeamNum == CsTeam.Terrorist ? "weapon_knife_t" : "weapon_knife";
			}

			player.GiveNamedItem(knifeToGive);
		}

		internal static bool PlayerHasKnife(CCSPlayerController? player)
		{
			if (!_config.Additional.KnifeEnabled) return false;

			if (player == null || !player.IsValid || player.PlayerPawn == null || !player.PlayerPawn.IsValid)
			{
				return false;
			}

			if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.WeaponServices == null || player.PlayerPawn.Value.ItemServices == null)
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

		internal void RefreshWeapons(CCSPlayerController? player)
		{
			if (!g_bCommandsAllowed) return;
			if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || (LifeState_t)player.LifeState != LifeState_t.LIFE_ALIVE)
				return;
			if (player.PlayerPawn.Value.WeaponServices == null || player.PlayerPawn.Value.ItemServices == null)
				return;

			var weapons = player.PlayerPawn.Value.WeaponServices.MyWeapons;

			if (weapons == null || weapons.Count == 0)
				return;
			if (player.Team == CsTeam.None || player.Team == CsTeam.Spectator)
				return;

			int playerTeam = player.TeamNum;

			//Dictionary<string, (int, int)> weaponsWithAmmo = new Dictionary<string, (int, int)>();
			Dictionary<string, List<(int, int)>> weaponsWithAmmo = new Dictionary<string, List<(int, int)>>();

			// Iterate through each weapon
			foreach (var weapon in weapons)
			{
				if (weapon == null || !weapon.IsValid || weapon.Value == null ||
					!weapon.Value.IsValid || !weapon.Value.DesignerName.Contains("weapon_"))
					continue;

				CCSWeaponBaseGun gun = weapon.Value.As<CCSWeaponBaseGun>();

				if (weapon.Value.Entity == null) continue;
				if (weapon.Value.OwnerEntity == null) continue;
				if (!weapon.Value.OwnerEntity.IsValid) continue;
				if (gun == null) continue;
				if (gun.Entity == null) continue;
				if (!gun.IsValid) continue;
				if (!gun.VisibleinPVS) continue;

				try
				{
					string? weaponByDefindex = null;

					CCSWeaponBaseVData? weaponData = weapon.Value.As<CCSWeaponBase>().VData;

					if (weaponData == null) continue;

					if (weaponData.GearSlot == gear_slot_t.GEAR_SLOT_RIFLE || weaponData.GearSlot == gear_slot_t.GEAR_SLOT_PISTOL)
					{
						if (!WeaponDefindex.TryGetValue(weapon.Value.AttributeManager.Item.ItemDefinitionIndex, out weaponByDefindex))
							continue;

						int clip1 = weapon.Value.Clip1;
						int reservedAmmo = weapon.Value.ReserveAmmo[0];

						if (!weaponsWithAmmo.ContainsKey(weaponByDefindex))
						{
							weaponsWithAmmo.Add(weaponByDefindex, new List<(int, int)>());
						}

						weaponsWithAmmo[weaponByDefindex].Add((clip1, reservedAmmo));

						if (gun == null || gun.VData == null) return;

						weapon.Value.Remove();
					}
				}
				catch (Exception ex)
				{
					Logger.LogWarning(ex.Message);
					continue;
				}
			}

			try
			{
				player.ExecuteClientCommand("slot 3");
				player.ExecuteClientCommand("slot 3");

				var weapon = player.PlayerPawn.Value.WeaponServices.ActiveWeapon;
				if (weapon is null || !weapon.IsValid || weapon.Value == null) return;
				CCSWeaponBaseVData? weaponData = weapon.Value.As<CCSWeaponBase>().VData;

				if (weapon.Value.DesignerName.Contains("knife") || weaponData?.GearSlot == gear_slot_t.GEAR_SLOT_KNIFE)
				{
					CCSWeaponBaseGun gun = weapon.Value.As<CCSWeaponBaseGun>();

					AddTimer(0.3f, () =>
					{
						if (player.TeamNum != playerTeam) return;

						player.ExecuteClientCommand("slot 3");
						gun = weapon.Value.As<CCSWeaponBaseGun>();
						player.DropActiveWeapon();

						AddTimer(0.7f, () =>
						{
							if (player.TeamNum != playerTeam) return;

							if (gun == null || !gun.IsValid || gun.State != CSWeaponState_t.WEAPON_NOT_CARRIED) return;

							gun.Remove();
						});

						GiveKnifeToPlayer(player);
					});
				}
			}
			catch (Exception ex)
			{
				Logger.LogWarning($"Cannot remove knife: {ex.Message}");
			}

			AddTimer(0.6f, () =>
					{
						if (!g_bCommandsAllowed) return;

						foreach (var entry in weaponsWithAmmo)
						{
							foreach (var ammo in entry.Value)
							{
								var newWeapon = new CBasePlayerWeapon(player.GiveNamedItem(entry.Key));
								Server.NextFrame(() =>
						{
							try
							{
								if (newWeapon != null)
								{
									newWeapon.Clip1 = ammo.Item1;
									newWeapon.ReserveAmmo[0] = ammo.Item2;
								}
							}
							catch (Exception ex)
							{
								Logger.LogWarning("Error setting weapon properties: " + ex.Message);
							}
						});
							}
						}

					}, TimerFlags.STOP_ON_MAPCHANGE);
		}

		/*
		internal void RefreshKnife(CCSPlayerController? player)
		{
			return;
			if (player == null || !player.IsValid || player.PlayerPawn?.Value == null)
				return;

			if (player.PlayerPawn.Value.WeaponServices == null)
				return;

			var weapons = player.PlayerPawn.Value.WeaponServices.MyWeapons;
			if (weapons != null && weapons.Count > 0)
			{
				try
				{
					player.ExecuteClientCommand("slot 3");
					player.ExecuteClientCommand("slot 3");

					var weapon = player.PlayerPawn.Value.WeaponServices.ActiveWeapon;
					if (weapon is null || !weapon.IsValid || weapon.Value == null) return;
					CCSWeaponBaseVData? weaponData = weapon.Value.As<CCSWeaponBase>().VData;

					if (weapon.Value.DesignerName.Contains("knife") || weaponData?.GearSlot == gear_slot_t.GEAR_SLOT_KNIFE)
					{
						AddTimer(0.2f, () =>
						{
							player.ExecuteClientCommand("slot 3");
							player.DropActiveWeapon();

							AddTimer(0.6f, () =>
							{
								if (weapon.IsValid)
									weapon.Value.Remove();
								GiveKnifeToPlayer(player);
							});
						});
					}
				}
				catch (Exception ex)
				{
					Logger.LogWarning($"Cannot remove knife: {ex.Message}");
				}
				return;
				foreach (var weapon in weapons)
				{
					if (weapon != null && weapon.IsValid && weapon.Value != null && weapon.Value.IsValid && weapon.Index > 0)
					{
					}
				}
			}
		}
		*/

		private static void RefreshGloves(CCSPlayerController player)
		{
			if (!Utility.IsPlayerValid(player) || (LifeState_t)player.LifeState != LifeState_t.LIFE_ALIVE) return;

			CCSPlayerPawn? pawn = player.PlayerPawn.Value;
			if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
				return;

			string model = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState.ModelName ?? string.Empty;
			if (!string.IsNullOrEmpty(model))
			{
				pawn.SetModel("characters/models/tm_jumpsuit/tm_jumpsuit_varianta.vmdl");
				pawn.SetModel(model);
			}

			Instance.AddTimer(0.06f, () =>
			{
				try
				{
					if (player == null || !player.IsValid)
						return;

					if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
						return;

					if (g_playersGlove.TryGetValue(player.Slot, out var gloveInfo) && gloveInfo != 0)
					{
						CCSPlayerPawn? pawn = player.PlayerPawn.Value;
						if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
							return;

						WeaponInfo weaponInfo = gPlayerWeaponsInfo[player.Slot][gloveInfo];

						CEconItemView item = pawn.EconGloves;
						item.ItemDefinitionIndex = gloveInfo;
						item.ItemIDLow = 16384 & 0xFFFFFFFF;
						item.ItemIDHigh = 16384;

						CAttributeList_SetOrAddAttributeValueByName.Invoke(item.NetworkedDynamicAttributes.Handle, "set item texture prefab", weaponInfo.Paint);
						CAttributeList_SetOrAddAttributeValueByName.Invoke(item.NetworkedDynamicAttributes.Handle, "set item texture seed", weaponInfo.Seed);
						CAttributeList_SetOrAddAttributeValueByName.Invoke(item.NetworkedDynamicAttributes.Handle, "set item texture wear", weaponInfo.Wear);

						item.Initialized = true;

						CBaseModelEntity_SetBodygroup.Invoke(pawn, "default_gloves", 1);
					}
				}
				catch (Exception) { }
			}, TimerFlags.STOP_ON_MAPCHANGE);
		}

		private static int GetRandomPaint(int defindex)
		{
			if (skinsList == null || skinsList.Count == 0)
				return 0;

			Random rnd = new Random();

			// Filter weapons by the provided defindex
			var filteredWeapons = skinsList.Where(w => w["weapon_defindex"]?.ToString() == defindex.ToString()).ToList();

			if (filteredWeapons.Count == 0)
				return 0;

			var randomWeapon = filteredWeapons[rnd.Next(filteredWeapons.Count)];

			if (int.TryParse(randomWeapon["paint"]?.ToString(), out int paintValue))
				return paintValue;

			return 0;
		}

		public static void SubclassChange(CBasePlayerWeapon weapon, ushort itemD)
		{
			var SubclassChangeFunc = VirtualFunction.Create<nint, string, int>(
				GameData.GetSignature("ChangeSubclass")
			);

			SubclassChangeFunc(weapon.Handle, itemD.ToString());
		}

		public static CCSPlayerController? GetPlayerFromItemServices(CCSPlayer_ItemServices itemServices)
		{
			var pawn = itemServices.Pawn.Value;
			if (pawn == null || !pawn.IsValid || !pawn.Controller.IsValid || pawn.Controller.Value == null) return null;
			var player = new CCSPlayerController(pawn.Controller.Value.Handle);
			if (!Utility.IsPlayerValid(player)) return null;
			return player;
		}

		private static CSkeletonInstance GetSkeletonInstance(CGameSceneNode node)
		{
			Func<nint, nint> GetSkeletonInstance = VirtualFunction.Create<nint, nint>(node.Handle, 8);
			return new CSkeletonInstance(GetSkeletonInstance(node.Handle));
		}

		private static unsafe CHandle<CBaseViewModel>[]? GetPlayerViewModels(CCSPlayerController player)
		{
			if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.ViewModelServices == null) return null;
			CCSPlayer_ViewModelServices viewModelServices = new CCSPlayer_ViewModelServices(player.PlayerPawn.Value.ViewModelServices!.Handle);
			return GetFixedArray<CHandle<CBaseViewModel>>(viewModelServices.Handle, "CCSPlayer_ViewModelServices", "m_hViewModel", 3);
		}

		public static unsafe T[] GetFixedArray<T>(nint pointer, string @class, string member, int length) where T : CHandle<CBaseViewModel>
		{
			nint ptr = pointer + Schema.GetSchemaOffset(@class, member);
			Span<nint> references = MemoryMarshal.CreateSpan(ref ptr, length);
			T[] values = new T[length];

			for (int i = 0; i < length; i++)
			{
				values[i] = (T)Activator.CreateInstance(typeof(T), references[i])!;
			}

			return values;
		}
	}
}