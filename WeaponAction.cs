using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace WeaponPaints
{
	public partial class WeaponPaints
	{
				private void GivePlayerWeaponSkin(CCSPlayerController player, CBasePlayerWeapon weapon)
		{
			if (!Config.Additional.SkinEnabled) return;
			if (!gPlayerWeaponsInfo.TryGetValue(player.Slot, out _)) return;

			bool isKnife = weapon.DesignerName.Contains("knife") || weapon.DesignerName.Contains("bayonet");

			if (isKnife && !g_playersKnife.ContainsKey(player.Slot) || isKnife && g_playersKnife[player.Slot] == "weapon_knife") return;

			int[] newPaints = { 106, 112, 113, 114, 115, 117, 118, 120, 121, 123, 126, 127, 128, 129, 130, 131, 133, 134, 137, 138, 139, 140, 142, 144, 145, 146, 152, 160, 161, 163, 173, 239, 292, 324, 331, 412, 461, 513, 766, 768, 770, 773, 774, 830, 831, 832, 834, 874, 875, 877, 878, 882, 883, 901, 912, 936, 937, 938, 939, 940, 1054, 1062, 1159, 1160, 1161, 1162, 1163, 1164, 1165, 1166, 1167, 1168, 1169, 1170, 1171, 1172, 1173, 1174, 1175, 1177, 1178, 1179, 1180 };

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
			
			UpdatePlayerEconItemId(weapon.AttributeManager.Item);

			int weaponDefIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;
			int fallbackPaintKit = 0;
			
			weapon.AttributeManager.Item.AccountID = (uint)player.SteamID;

			if (_config.Additional.GiveRandomSkin &&
				 !gPlayerWeaponsInfo[player.Slot].ContainsKey(weaponDefIndex))
			{
				// Random skins
				weapon.FallbackPaintKit = GetRandomPaint(weaponDefIndex);
				weapon.FallbackSeed = 0;
				weapon.FallbackWear = 0.01f;

				weapon.AttributeManager.Item.NetworkedDynamicAttributes.Attributes.RemoveAll();
				CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "set item texture prefab",  GetRandomPaint(weaponDefIndex));
				CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "set item texture seed", 0);
				CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "set item texture wear", 0.01f);

				weapon.AttributeManager.Item.AttributeList.Attributes.RemoveAll();
				CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.AttributeList.Handle, "set item texture prefab",  GetRandomPaint(weaponDefIndex));
				CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.AttributeList.Handle, "set item texture seed", 0);
				CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.AttributeList.Handle, "set item texture wear", 0.01f);

				fallbackPaintKit = weapon.FallbackPaintKit;

				if (fallbackPaintKit == 0)
					return;

				if (isKnife) return;
				UpdatePlayerWeaponMeshGroupMask(player, weapon, !newPaints.Contains(fallbackPaintKit));
				return;
			}

			if (!gPlayerWeaponsInfo[player.Slot].TryGetValue(weaponDefIndex, out var value) || value.Paint == 0) return;

			var weaponInfo = value;
			//Log($"Apply on {weapon.DesignerName}({weapon.AttributeManager.Item.ItemDefinitionIndex}) paint {gPlayerWeaponPaints[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]} seed {gPlayerWeaponSeed[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]} wear {gPlayerWeaponWear[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]}");
			weapon.AttributeManager.Item.ItemID = 16384;
			weapon.AttributeManager.Item.ItemIDLow = 16384 & 0xFFFFFFFF;
			weapon.AttributeManager.Item.ItemIDHigh = weapon.AttributeManager.Item.ItemIDLow >> 32;
			weapon.FallbackPaintKit = weaponInfo.Paint;
			weapon.FallbackSeed = weaponInfo.Seed;
			weapon.FallbackWear = weaponInfo.Wear;
			CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "set item texture prefab", weapon.FallbackPaintKit);

			fallbackPaintKit = weapon.FallbackPaintKit;

			if (fallbackPaintKit == 0)
				return;

			if (isKnife) return;
			UpdatePlayerWeaponMeshGroupMask(player, weapon, !newPaints.Contains(fallbackPaintKit));
		}
		
		private static void GiveKnifeToPlayer(CCSPlayerController? player)
		{
			if (!_config.Additional.KnifeEnabled || player == null || !player.IsValid) return;

			if (PlayerHasKnife(player)) return;

			//string knifeToGive = (CsTeam)player.TeamNum == CsTeam.Terrorist ? "weapon_knife_t" : "weapon_knife";
			player.GiveNamedItem(CsItem.Knife);
		}

		private static bool PlayerHasKnife(CCSPlayerController? player)
		{
			if (!_config.Additional.KnifeEnabled) return false;

			if (player == null || !player.IsValid || !player.PlayerPawn.IsValid)
			{
				return false;
			}

			if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.WeaponServices == null || player.PlayerPawn.Value.ItemServices == null)
				return false;

			var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
			if (weapons == null) return false;
			foreach (var weapon in weapons)
			{
				if (!weapon.IsValid || weapon.Value == null || !weapon.Value.IsValid) continue;
				if (weapon.Value.DesignerName.Contains("knife") || weapon.Value.DesignerName.Contains("bayonet"))
				{
					return true;
				}
			}
			return false;
		}

		private void RefreshWeapons(CCSPlayerController? player)
		{
			if (!g_bCommandsAllowed) return;
			if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || (LifeState_t)player.LifeState != LifeState_t.LIFE_ALIVE)
				return;
			if (player.PlayerPawn.Value.WeaponServices == null || player.PlayerPawn.Value.ItemServices == null)
				return;

			var weapons = player.PlayerPawn.Value.WeaponServices.MyWeapons;

			if (weapons.Count == 0)
				return;
			if (player.Team is CsTeam.None or CsTeam.Spectator)
				return;

			int playerTeam = player.TeamNum;

			Dictionary<string, List<(int, int)>> weaponsWithAmmo = [];

			foreach (var weapon in weapons)
			{
				if (!weapon.IsValid || weapon.Value == null ||
					!weapon.Value.IsValid || !weapon.Value.DesignerName.Contains("weapon_"))
					continue;

				CCSWeaponBaseGun gun = weapon.Value.As<CCSWeaponBaseGun>();

				if (weapon.Value.Entity == null) continue;
				if (!weapon.Value.OwnerEntity.IsValid) continue;
				if (gun.Entity == null) continue;
				if (!gun.IsValid) continue;
				if (!gun.VisibleinPVS) continue;

				try
				{
					CCSWeaponBaseVData? weaponData = weapon.Value.As<CCSWeaponBase>().VData;

					if (weaponData == null) continue;

					if (weaponData.GearSlot == gear_slot_t.GEAR_SLOT_RIFLE || weaponData.GearSlot == gear_slot_t.GEAR_SLOT_PISTOL)
					{
						if (!WeaponDefindex.TryGetValue(weapon.Value.AttributeManager.Item.ItemDefinitionIndex, out var weaponByDefindex))
							continue;

						int clip1 = weapon.Value.Clip1;
						int reservedAmmo = weapon.Value.ReserveAmmo[0];

						if (!weaponsWithAmmo.TryGetValue(weaponByDefindex, out var value))
						{
							value = [];
							weaponsWithAmmo.Add(weaponByDefindex, value);
						}

						value.Add((clip1, reservedAmmo));

						if (gun.VData == null) return;

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
				if (!weapon.IsValid || weapon.Value == null) return;
				CCSWeaponBaseVData? weaponData = weapon.Value.As<CCSWeaponBase>().VData;

				if (weapon.Value.DesignerName.Contains("knife") || weaponData?.GearSlot == gear_slot_t.GEAR_SLOT_KNIFE)
				{
					CCSWeaponBaseGun gun;

					AddTimer(0.3f, () =>
					{
						if (player.TeamNum != playerTeam) return;

						player.ExecuteClientCommand("slot 3");
						gun = weapon.Value.As<CCSWeaponBaseGun>();
						player.DropActiveWeapon();

						AddTimer(0.7f, () =>
						{
							if (player.TeamNum != playerTeam) return;

							if (!gun.IsValid || gun.State != CSWeaponState_t.WEAPON_NOT_CARRIED) return;

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
								newWeapon.Clip1 = ammo.Item1;
								newWeapon.ReserveAmmo[0] = ammo.Item2;
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

		private void GivePlayerGloves(CCSPlayerController player)
		{
			if (!Utility.IsPlayerValid(player) || (LifeState_t)player.LifeState != LifeState_t.LIFE_ALIVE) return;

			CCSPlayerPawn? pawn = player.PlayerPawn.Value;
			if (pawn == null || !pawn.IsValid)
				return;

			var model = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState.ModelName ?? string.Empty;
			if (!string.IsNullOrEmpty(model))
			{
				pawn.SetModel("characters/models/tm_jumpsuit/tm_jumpsuit_varianta.vmdl");
				pawn.SetModel(model);
			}

			Instance.AddTimer(0.06f, () =>
			{
				CEconItemView item = pawn.EconGloves;
				try
				{
					if (!player.IsValid)
						return;

					if (!player.PawnIsAlive)
						return;

					if (!g_playersGlove.TryGetValue(player.Slot, out var gloveInfo) || gloveInfo == 0) return;

					WeaponInfo weaponInfo = gPlayerWeaponsInfo[player.Slot][gloveInfo];

					item.ItemDefinitionIndex = gloveInfo;
					item.ItemIDLow = 16384 & 0xFFFFFFFF;
					item.ItemIDHigh = 16384;

					CAttributeListSetOrAddAttributeValueByName.Invoke(item.NetworkedDynamicAttributes.Handle, "set item texture prefab", weaponInfo.Paint);
					CAttributeListSetOrAddAttributeValueByName.Invoke(item.NetworkedDynamicAttributes.Handle, "set item texture seed", weaponInfo.Seed);
					CAttributeListSetOrAddAttributeValueByName.Invoke(item.NetworkedDynamicAttributes.Handle, "set item texture wear", weaponInfo.Wear);

					item.Initialized = true;

					SetBodygroup(pawn.Handle, "default_gloves", 1);
				}
				catch (Exception) { }
			}, TimerFlags.STOP_ON_MAPCHANGE);
		}

		private static int GetRandomPaint(int defindex)
		{
			if (skinsList.Count == 0)
				return 0;

			Random rnd = new Random();

			// Filter weapons by the provided defindex
			var filteredWeapons = skinsList.Where(w => w["weapon_defindex"]?.ToString() == defindex.ToString()).ToList();

			if (filteredWeapons.Count == 0)
				return 0;

			var randomWeapon = filteredWeapons[rnd.Next(filteredWeapons.Count)];

			return int.TryParse(randomWeapon["paint"]?.ToString(), out var paintValue) ? paintValue : 0;
		}

		private static void SubclassChange(CBasePlayerWeapon weapon, ushort itemD)
		{
			var subclassChangeFunc = VirtualFunction.Create<nint, string, int>(
				GameData.GetSignature("ChangeSubclass")
			);

			subclassChangeFunc(weapon.Handle, itemD.ToString());
		}

		private static void UpdateWeaponMeshGroupMask(CBaseEntity weapon, bool isLegacy = false)
		{
			if (weapon.CBodyComponent?.SceneNode == null) return;
			var skeleton = weapon.CBodyComponent.SceneNode.GetSkeletonInstance();
			var value = (ulong)(isLegacy ? 2 : 1);

			if (skeleton.ModelState.MeshGroupMask != value)
			{
				skeleton.ModelState.MeshGroupMask = value;
			}
		}

		private static void UpdatePlayerWeaponMeshGroupMask(CCSPlayerController player, CBasePlayerWeapon weapon, bool isLegacy)
		{
			UpdateWeaponMeshGroupMask(weapon, isLegacy);

			var viewModel = GetPlayerViewModel(player);
			if (viewModel == null || viewModel.Weapon.Value == null ||
			    viewModel.Weapon.Value.Index != weapon.Index) return;
			UpdateWeaponMeshGroupMask(viewModel, isLegacy);
			Utilities.SetStateChanged(viewModel, "CBaseEntity", "m_CBodyComponent");
		}

		private static void GivePlayerAgent(CCSPlayerController player)
		{
			if (!g_playersAgent.TryGetValue(player.Slot, out var value)) return;

			var model = player.TeamNum == 3 ? value.CT : value.T;
			if (string.IsNullOrEmpty(model)) return;

			if (player.PlayerPawn.Value == null)
				return;

			try
			{
				Server.NextFrame(() =>
				{
					player.PlayerPawn.Value.SetModel(
						$"characters/models/{model}.vmdl"
					);
				});
			}
			catch (Exception)
			{
			}
		}

		private static void GivePlayerMusicKit(CCSPlayerController player)
		{
			if (!g_playersMusic.TryGetValue(player.Slot, out var value)) return;
			if (player.InventoryServices == null) return;
			
			player.InventoryServices.MusicID = value;
			Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInventoryServices");
			player.MusicKitID = value;
			Utilities.SetStateChanged(player, "CCSPlayerController", "m_iMusicKitID");
		}
		
		private void GiveOnItemPickup(CCSPlayerController player)
		{
			var pawn = player.PlayerPawn.Value;
			if (pawn == null) return;
			
			var myWeapons = pawn.WeaponServices?.MyWeapons;
			if (myWeapons == null) return;
			foreach (var handle in myWeapons)
			{
				var weapon = handle.Value;
				if (weapon != null && weapon.DesignerName.Contains("knife"))
				{
					GivePlayerWeaponSkin(player, weapon);
				}
			}
		}
		
		private void UpdatePlayerEconItemId(CEconItemView econItemView)
		{
			var itemId = _nextItemId++;
			econItemView.ItemID = itemId;

			econItemView.ItemIDLow = (uint)itemId & 0xFFFFFFFF;
			econItemView.ItemIDHigh = (uint)itemId >> 32;
		}

		private static CCSPlayerController? GetPlayerFromItemServices(CCSPlayer_ItemServices itemServices)
		{
			var pawn = itemServices.Pawn.Value;
			if (!pawn.IsValid || !pawn.Controller.IsValid || pawn.Controller.Value == null) return null;
			var player = new CCSPlayerController(pawn.Controller.Value.Handle);
			return !Utility.IsPlayerValid(player) ? null : player;
		}

		private static unsafe CBaseViewModel? GetPlayerViewModel(CCSPlayerController player)
		{
			if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.ViewModelServices == null) return null;
			CCSPlayer_ViewModelServices viewModelServices = new(player.PlayerPawn.Value.ViewModelServices!.Handle);
			var ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
			var references = MemoryMarshal.CreateSpan(ref ptr, 3);
			var viewModel = (CHandle<CBaseViewModel>)Activator.CreateInstance(typeof(CHandle<CBaseViewModel>), references[0])!;
			return viewModel.Value == null ? null : viewModel.Value;
		}

		public static unsafe T[] GetFixedArray<T>(nint pointer, string @class, string member, int length) where T : CHandle<CBaseViewModel>
		{
			var ptr = pointer + Schema.GetSchemaOffset(@class, member);
			var references = MemoryMarshal.CreateSpan(ref ptr, length);
			var values = new T[length];

			for (var i = 0; i < length; i++)
			{
				values[i] = (T)Activator.CreateInstance(typeof(T), references[i])!;
			}

			return values;
		}
	}
}