using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace WeaponPaints
{
	public partial class WeaponPaints
	{
		private void RegisterEvents()
		{
			RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
			/*RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorized);*/
			RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
			RegisterListener<Listeners.OnMapStart>(OnMapStart);
			RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
			RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
			RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Pre);
			RegisterEventHandler<EventItemPurchase>(OnEventItemPurchasePost);
			RegisterEventHandler<EventItemPickup>(OnItemPickup);
		}

		private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;

			if (player == null || !player.IsValid || !player.EntityIndex.HasValue || player.IsHLTV) return HookResult.Continue;

			int playerIndex = (int)player.EntityIndex.Value.Value;
			if (Config.Additional.SkinEnabled && weaponSync != null)
				_ = weaponSync.GetWeaponPaintsFromDatabase(playerIndex);
			if (Config.Additional.KnifeEnabled && weaponSync != null)
				_ = weaponSync.GetKnifeFromDatabase(playerIndex);
			/*
			Task.Run(async () =>
			{
				if (Config.Additional.SkinEnabled && weaponSync != null)
				if (Config.Additional.KnifeEnabled && weaponSync != null)
			});
			*/

			return HookResult.Continue;
		}

		private void OnMapStart(string mapName)
		{
			if (!Config.Additional.KnifeEnabled) return;
			// TODO
			// needed for now
			AddTimer(2.0f, () =>
			{
				NativeAPI.IssueServerCommand("mp_t_default_melee \"\"");
				NativeAPI.IssueServerCommand("mp_ct_default_melee \"\"");
				NativeAPI.IssueServerCommand("mp_equipment_reset_rounds 0");
			});
		}
		/*
		private void OnClientAuthorized(int playerSlot, SteamID steamID)
		{
			int playerIndex = playerSlot + 1;
			Task.Run(async () =>
			{
				if (Config.Additional.SkinEnabled && weaponSync != null)
					await weaponSync.GetWeaponPaintsFromDatabase(playerIndex);
				if (Config.Additional.KnifeEnabled && weaponSync != null)
					await weaponSync.GetKnifeFromDatabase(playerIndex);
			});
		}
		*/
		private void OnClientDisconnect(int playerSlot)
		{
			CCSPlayerController player = Utilities.GetPlayerFromSlot(playerSlot);

			if (player == null || !player.IsValid || player.IsHLTV) return;

			if (Config.Additional.KnifeEnabled)
				g_playersKnife.Remove((int)player.EntityIndex!.Value.Value);
			if (Config.Additional.SkinEnabled)
				gPlayerWeaponsInfo.Remove((int)player.EntityIndex!.Value.Value);
		}

		private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;
			if (player == null || !player.IsValid || !player.PlayerPawn.IsValid)
			{
				return HookResult.Continue;
			}

			if (Config.Additional.KnifeEnabled)
			{
				g_knifePickupCount[(int)player.EntityIndex!.Value.Value] = 0;
				if (!PlayerHasKnife(player))
					GiveKnifeToPlayer(player);
			}

			if (Config.Additional.SkinVisibilityFix)
			{
				AddTimer(0.3f, () => RefreshSkins(player));
			}

			return HookResult.Continue;
		}
		private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
		{
			NativeAPI.IssueServerCommand("mp_t_default_melee \"\"");
			NativeAPI.IssueServerCommand("mp_ct_default_melee \"\"");
			NativeAPI.IssueServerCommand("mp_equipment_reset_rounds 0");

			return HookResult.Continue;
		}
		private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
		{
			if (@event.Defindex == 42 || @event.Defindex == 59)
			{
				CCSPlayerController? player = @event.Userid;
				if (!Utility.IsPlayerValid(player) || !player.PawnIsAlive || g_knifePickupCount[(int)player.EntityIndex!.Value.Value] >= 2) return HookResult.Continue;

				if (g_playersKnife.ContainsKey((int)player.EntityIndex!.Value.Value)
					&&
				   g_playersKnife[(int)player.EntityIndex!.Value.Value] != "weapon_knife")
				{
					g_knifePickupCount[(int)player.EntityIndex!.Value.Value]++;

					RemovePlayerKnife(player, true);
					AddTimer(0.3f, ()=> GiveKnifeToPlayer(player));

					//RefreshPlayerKnife(player);
					/*
					if (!PlayerHasKnife(player))
						GiveKnifeToPlayer(player);
					
					if (Config.Additional.SkinVisibilityFix)
					{
						AddTimer(0.25f, () => RefreshSkins(player));
					}
					*/
				}
			}
			return HookResult.Continue;
		}

		private void OnEntitySpawned(CEntityInstance entity)
		{
			if (!Config.Additional.SkinEnabled) return;
			var designerName = entity.DesignerName;
			if (!weaponList.ContainsKey(designerName)) return;
			bool isKnife = false;
			var weapon = new CBasePlayerWeapon(entity.Handle);

			if (designerName.Contains("knife") || designerName.Contains("bayonet"))
			{
				isKnife = true;
			}
			Server.NextFrame(() =>
			{
				try
				{
					if (!weapon.IsValid) return;
					if (weapon.OwnerEntity.Value == null) return;
					if (!weapon.OwnerEntity.Value.EntityIndex.HasValue)
					{
						for (int i = 1; i <= Server.MaxPlayers; i++)
						{
							CCSPlayerController? ghostPlayer = Utilities.GetPlayerFromIndex(i);
							if (!Utility.IsPlayerValid(ghostPlayer)) continue;
							if (g_changedKnife.Contains((int)ghostPlayer.EntityIndex!.Value.Value))
							{
								ChangeWeaponAttributes(weapon, ghostPlayer, isKnife);
								g_changedKnife.Remove((int)ghostPlayer.EntityIndex!.Value.Value);
								break;
							}
						}
						return;
					}

					if (!weapon.OwnerEntity.Value.EntityIndex.HasValue) return;
					int weaponOwner = (int)weapon.OwnerEntity.Value.EntityIndex.Value.Value;
					var pawn = new CBasePlayerPawn(NativeAPI.GetEntityFromIndex(weaponOwner));
					if (!pawn.IsValid) return;
					var playerIndex = (int)pawn.Controller.Value.EntityIndex!.Value.Value;
					var player = Utilities.GetPlayerFromIndex(playerIndex);
					if (!Utility.IsPlayerValid(player)) return;

					// TODO: Remove knife crashes here, needs another solution
					/*if (isKnife && g_playersKnife[(int)player.EntityIndex!.Value.Value] != "weapon_knife" && (weapon.AttributeManager.Item.ItemDefinitionIndex == 42 || weapon.AttributeManager.Item.ItemDefinitionIndex == 59))
					{
						RemoveKnifeFromPlayer(player);
						return;
					}*/
					ChangeWeaponAttributes(weapon, player, isKnife);
				}
				catch (Exception) { }
			});
		}
		private HookResult OnEventItemPurchasePost(EventItemPurchase @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;

			if (player == null || !player.IsValid) return HookResult.Continue;

			if (Config.Additional.SkinVisibilityFix)
				AddTimer(0.2f, () => RefreshSkins(player));

			return HookResult.Continue;
		}
	}
}
