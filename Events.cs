using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;

namespace WeaponPaints
{
	public partial class WeaponPaints
	{
		private void OnClientAuthorized(int playerSlot, SteamID steamID)
		{
			int playerIndex = playerSlot + 1;

			CCSPlayerController? player = Utilities.GetPlayerFromIndex(playerIndex);

			PlayerInfo playerInfo = new PlayerInfo
			{
				UserId = player.UserId,
				Index = (int)player.Index,
				SteamId = player?.AuthorizedSteamID?.SteamId64.ToString(),
				Name = player?.PlayerName,
				IpAddress = player?.IpAddress?.Split(":")[0]
			};

			if (player == null || !player.IsValid || player.IsBot || player.IsHLTV || weaponSync == null) return;

			Task.Run(async () =>
			{
				if (Config.Additional.SkinEnabled)
					await weaponSync.GetKnifeFromDatabase(playerInfo);
			});

			//if (Config.Additional.KnifeEnabled && weaponSync != null)
			//_ = weaponSync.GetKnifeFromDatabase(playerIndex);
		}

		private void OnClientDisconnect(int playerSlot)
		{
			CCSPlayerController player = Utilities.GetPlayerFromSlot(playerSlot);

			if (player == null || !player.IsValid || player.IsBot || player.IsHLTV) return;

			if (Config.Additional.KnifeEnabled)
				g_playersKnife.Remove((int)player.Index);
			if (Config.Additional.SkinEnabled)
				gPlayerWeaponsInfo.Remove((int)player.Index);
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
					if (weapon.OwnerEntity.Index <= 0) return;
					int weaponOwner = (int)weapon.OwnerEntity.Index;
					var pawn = new CBasePlayerPawn(NativeAPI.GetEntityFromIndex(weaponOwner));
					if (!pawn.IsValid) return;

					var playerIndex = (int)pawn.Controller.Index;
					var player = Utilities.GetPlayerFromIndex(playerIndex);
					if (!Utility.IsPlayerValid(player)) return;

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

		private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
		{
			if (@event.Defindex == 42 || @event.Defindex == 59)
			{
				CCSPlayerController? player = @event.Userid;
				if (!Utility.IsPlayerValid(player) || !player.PawnIsAlive || g_knifePickupCount[(int)player.Index] >= 1) return HookResult.Continue;

				if (g_playersKnife.ContainsKey((int)player.Index)
					&&
				   g_playersKnife[(int)player.Index] != "weapon_knife")
				{
					g_knifePickupCount[(int)player.Index]++;

					RemovePlayerKnife(player, true);
					AddTimer(0.3f, () => GiveKnifeToPlayer(player));
				}
			}
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

				if (Config.GlobalShare)
					GlobalShareConnect();

				weaponSync = new WeaponSynchronization(DatabaseConnectionString, Config, GlobalShareApi, GlobalShareServerId);
			});

			g_hTimerCheckSkinsData = AddTimer(10.0f, () =>
			{
				List<CCSPlayerController> players = Utilities.GetPlayers();

				foreach (CCSPlayerController player in players)
				{
					if (player == null || !player.IsValid || player.IsBot || player.IsHLTV || player.AuthorizedSteamID == null) continue;
					if (gPlayerWeaponsInfo.ContainsKey((int)player.Index)) continue;

					PlayerInfo playerInfo = new PlayerInfo
					{
						UserId = player.UserId,
						Index = (int)player.Index,
						SteamId = player?.AuthorizedSteamID?.SteamId64.ToString(),
						Name = player?.PlayerName,
						IpAddress = player?.IpAddress?.Split(":")[0]
					};

					if (Config.Additional.SkinEnabled && weaponSync != null)
						_ = weaponSync.GetWeaponPaintsFromDatabase(playerInfo);
					if (Config.Additional.KnifeEnabled && weaponSync != null)
						_ = weaponSync.GetKnifeFromDatabase(playerInfo);
				}
			}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE | CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);

		}

		private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;

			if (player == null || !player.IsValid || player.IsBot || player.IsHLTV || weaponSync == null) return HookResult.Continue;

			PlayerInfo playerInfo = new PlayerInfo
			{
				UserId = player.UserId,
				Index = (int)player.Index,
				SteamId = player?.AuthorizedSteamID?.SteamId64.ToString(),
				Name = player?.PlayerName,
				IpAddress = player?.IpAddress?.Split(":")[0]
			};

			if (!gPlayerWeaponsInfo.ContainsKey((int)player!.Index))
			{
				Console.WriteLine($"[WeaponPaints] Retrying to retrieve player {player.PlayerName} skins");
				Task.Run(async () =>
				{
					if (Config.Additional.SkinEnabled)
						await weaponSync.GetWeaponPaintsFromDatabase(playerInfo);
					if (Config.Additional.KnifeEnabled)
						await weaponSync.GetKnifeFromDatabase(playerInfo);
				});
			}

			return HookResult.Continue;
		}

		private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;
			if (player == null || !player.IsValid)
			{
				return HookResult.Continue;
			}

			if (Config.Additional.KnifeEnabled)
			{
				g_knifePickupCount[(int)player.Index] = 0;
				if (!PlayerHasKnife(player))
					GiveKnifeToPlayer(player);
			}

			if (Config.Additional.SkinVisibilityFix)
			{
				AddTimer(0.3f, () => RefreshSkins(player));
			}

			return HookResult.Continue;
		}


		private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
		{
			g_bCommandsAllowed = false;
			return HookResult.Continue;
		}

		private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
		{
			NativeAPI.IssueServerCommand("mp_t_default_melee \"\"");
			NativeAPI.IssueServerCommand("mp_ct_default_melee \"\"");
			NativeAPI.IssueServerCommand("mp_equipment_reset_rounds 0");

			g_bCommandsAllowed = true;

			return HookResult.Continue;
		}

		private void RegisterListeners()
		{
			RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
			RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorized);
			RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
			RegisterListener<Listeners.OnMapStart>(OnMapStart);

			RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
			RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
			RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Pre);
			RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
			RegisterEventHandler<EventItemPurchase>(OnEventItemPurchasePost);
			RegisterEventHandler<EventItemPickup>(OnItemPickup);
		}

		/* WORKAROUND FOR CLIENTS WITHOUT STEAMID ON AUTHORIZATION */
		/*private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;

			if (player == null || !player.IsValid || !player.EntityIndex.HasValue || player.IsHLTV) return HookResult.Continue;

			int playerIndex = (int)player.EntityIndex.Value.Value;
			if (Config.Additional.SkinEnabled && weaponSync != null)
				_ = weaponSync.GetWeaponPaintsFromDatabase(playerIndex);
			if (Config.Additional.KnifeEnabled && weaponSync != null)
				_ = weaponSync.GetKnifeFromDatabase(playerIndex);

			Task.Run(async () =>
			{
				if (Config.Additional.SkinEnabled && weaponSync != null)
				if (Config.Additional.KnifeEnabled && weaponSync != null)
			});

			return HookResult.Continue;
		}
	*/
	}
}