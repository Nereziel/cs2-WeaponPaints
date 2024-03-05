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
		internal static void GiveKnifeToPlayer(CCSPlayerController? player)
		{
			if (!_config.Additional.KnifeEnabled || player == null || !player.IsValid) return;

			if (PlayerHasKnife(player)) return;

			string knifeToGive = (CsTeam)player.TeamNum == CsTeam.Terrorist ? "weapon_knife_t" : "weapon_knife";
			player.GiveNamedItem(CsItem.Knife);
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

			Dictionary<string, List<(int, int)>> weaponsWithAmmo = new Dictionary<string, List<(int, int)>>();

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

		public static void UpdateWeaponMeshGroupMask(CBaseEntity weapon, bool isLegacy = false)
		{
			if (weapon.CBodyComponent != null && weapon.CBodyComponent.SceneNode != null)
			{
				var skeleton = weapon.CBodyComponent.SceneNode.GetSkeletonInstance();
				if (skeleton != null)
				{
					var value = (ulong)(isLegacy ? 2 : 1);

					if (skeleton.ModelState.MeshGroupMask != value)
					{
						skeleton.ModelState.MeshGroupMask = value;
					}
				}
			}
		}

		public static void UpdatePlayerWeaponMeshGroupMask(CCSPlayerController player, CBasePlayerWeapon weapon, bool isLegacy)
		{
			UpdateWeaponMeshGroupMask(weapon, isLegacy);

			var viewModel = GetPlayerViewModel(player);
			if (viewModel != null && viewModel.Weapon.Value != null && viewModel.Weapon.Value.Index == weapon.Index)
			{
				UpdateWeaponMeshGroupMask(viewModel, isLegacy);
				Utilities.SetStateChanged(viewModel, "CBaseEntity", "m_CBodyComponent");
			}
		}

		public void GivePlayerAgent(CCSPlayerController player)
		{
			try
			{
				Server.NextFrame(() =>
				{
					string? model = player.TeamNum == 3 ? g_playersAgent[player.Slot].CT : g_playersAgent[player.Slot].T;
					if (model == null) return;

					player.PlayerPawn.Value!.SetModel(
						$"characters/models/{model}.vmdl"
					);
				});
			}
			catch (Exception)
			{
			}
		}

		public static CCSPlayerController? GetPlayerFromItemServices(CCSPlayer_ItemServices itemServices)
		{
			var pawn = itemServices.Pawn.Value;
			if (pawn == null || !pawn.IsValid || !pawn.Controller.IsValid || pawn.Controller.Value == null) return null;
			var player = new CCSPlayerController(pawn.Controller.Value.Handle);
			if (!Utility.IsPlayerValid(player)) return null;
			return player;
		}

		private static unsafe CBaseViewModel? GetPlayerViewModel(CCSPlayerController player)
		{
			if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.ViewModelServices == null) return null;
			CCSPlayer_ViewModelServices viewModelServices = new(player.PlayerPawn.Value.ViewModelServices!.Handle);
			nint ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
			var references = MemoryMarshal.CreateSpan(ref ptr, 3);
			var viewModel = (CHandle<CBaseViewModel>)Activator.CreateInstance(typeof(CHandle<CBaseViewModel>), references[0])!;
			if (viewModel == null || viewModel.Value == null) return null;
			return viewModel.Value;
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