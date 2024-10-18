using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Runtime.InteropServices;

namespace WeaponPaints
{
	public partial class WeaponPaints
	{
		[GameEventHandler]
		public HookResult OnClientFullConnect(EventPlayerConnectFull @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;

			if (player is null || !player.IsValid || player.IsBot ||
				weaponSync == null || _database == null) return HookResult.Continue;

			PlayerInfo playerInfo = new PlayerInfo
			{
				UserId = player.UserId,
				Slot = player.Slot,
				Index = (int)player.Index,
				SteamId = player.SteamID.ToString(),
				Name = player.PlayerName,
				IpAddress = player.IpAddress?.Split(":")[0]
			};

			try
			{
				_ = Task.Run(async () => await weaponSync.GetPlayerData(playerInfo));
				/*
				if (Config.Additional.SkinEnabled)
				{
					_ = Task.Run(async () => await weaponSync.GetWeaponPaintsFromDatabase(playerInfo));
				}
				if (Config.Additional.KnifeEnabled)
				{
					_ = Task.Run(async () => await weaponSync.GetKnifeFromDatabase(playerInfo));
				}
				if (Config.Additional.GloveEnabled)
				{
					_ = Task.Run(async () => await weaponSync.GetGloveFromDatabase(playerInfo));
				}
				if (Config.Additional.AgentEnabled)
				{
					_ = Task.Run(async () => await weaponSync.GetAgentFromDatabase(playerInfo));
				}
				if (Config.Additional.MusicEnabled)
				{
					_ = Task.Run(async () => await weaponSync.GetMusicFromDatabase(playerInfo));
				}
				*/
			}
			catch
			{
			}

			return HookResult.Continue;
		}

		[GameEventHandler]
		public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;

			if (player is null || !player.IsValid || player.IsBot) return HookResult.Continue;

			if (Config.Additional.SkinEnabled)
			{
				gPlayerWeaponsInfo.TryRemove(player.Slot, out _);
			}
			if (Config.Additional.KnifeEnabled)
			{
				g_playersKnife.TryRemove(player.Slot, out _);
			}
			if (Config.Additional.GloveEnabled)
			{
				g_playersGlove.TryRemove(player.Slot, out _);
			}
			if (Config.Additional.AgentEnabled)
			{
				g_playersAgent.TryRemove(player.Slot, out _);
			}
			if (Config.Additional.MusicEnabled)
			{
				g_playersMusic.TryRemove(player.Slot, out _);
			}

			temporaryPlayerWeaponWear.TryRemove(player.Slot, out _);

			commandsCooldown.Remove(player.Slot);

			return HookResult.Continue;
		}
		
		private void OnMapStart(string mapName)
		{
			if (Config.Additional is { KnifeEnabled: false, SkinEnabled: false, GloveEnabled: false }) return;

			if (_database != null)
				weaponSync = new WeaponSynchronization(_database, Config);
		}

		private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;

			if (player is null || !player.IsValid || Config.Additional is { KnifeEnabled: false, GloveEnabled: false })
				return HookResult.Continue;

			CCSPlayerPawn? pawn = player.PlayerPawn.Value;

			if (pawn == null || !pawn.IsValid)
				return HookResult.Continue;

			g_knifePickupCount[player.Slot] = 0;

			GivePlayerMusicKit(player);
			GivePlayerAgent(player);
			GivePlayerGloves(player);

			return HookResult.Continue;
		}

		private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
		{
			g_bCommandsAllowed = false;

			return HookResult.Continue;
		}

		private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
		{
			g_bCommandsAllowed = true;
			return HookResult.Continue;
		}

		public HookResult OnGiveNamedItemPost(DynamicHook hook)
		{
			try
			{
				var itemServices = hook.GetParam<CCSPlayer_ItemServices>(0);
				var weapon = hook.GetReturn<CBasePlayerWeapon>();
				if (!weapon.DesignerName.Contains("weapon"))
					return HookResult.Continue;

				var player = GetPlayerFromItemServices(itemServices);
				if (player != null)
					GivePlayerWeaponSkin(player, weapon);
			}
			catch { }

			return HookResult.Continue;
		}

		public void OnEntityCreated(CEntityInstance entity)
		{
			var designerName = entity.DesignerName;

			if (designerName.Contains("weapon"))
			{
				Server.NextFrame(() =>
				{
					var weapon = new CBasePlayerWeapon(entity.Handle);
					if (!weapon.IsValid) return;

					try
					{
						SteamID? _steamid = null;

						if (weapon.OriginalOwnerXuidLow > 0)
							_steamid = new(weapon.OriginalOwnerXuidLow);

						CCSPlayerController? player = null;

						if (_steamid != null && _steamid.IsValid())
						{
							player = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == _steamid.SteamId64);

							if (player == null)
								player = Utilities.GetPlayerFromSteamId(weapon.OriginalOwnerXuidLow);
						}
						else
						{
							CCSWeaponBaseGun gun = weapon.As<CCSWeaponBaseGun>();
							player = Utilities.GetPlayerFromIndex((int)weapon.OwnerEntity.Index) ?? Utilities.GetPlayerFromIndex((int)gun.OwnerEntity.Value!.Index);
						}

						if (string.IsNullOrEmpty(player?.PlayerName)) return;
						if (!Utility.IsPlayerValid(player)) return;

						GivePlayerWeaponSkin(player, weapon);
					}
					catch (Exception)
					{
						return;
					}
				});
			}
		}

		private void OnTick()
		{
			if (!Config.Additional.ShowSkinImage) return;

			foreach (var player in Utilities.GetPlayers().Where(p =>
							p is { IsValid: true, PlayerPawn.IsValid: true } &&
							(LifeState_t)p.LifeState == LifeState_t.LIFE_ALIVE
							&& !p.IsBot && p is {  Connected: PlayerConnectedState.PlayerConnected }
							)
				)
			{
				if (PlayerWeaponImage.TryGetValue(player.Slot, out var value) && !string.IsNullOrEmpty(value))
				{
					player.PrintToCenterHtml("<img src='{PATH}'</img>".Replace("{PATH}", value));
				}
			}
		}
		
		[GameEventHandler]
		public HookResult OnItemPickup(EventItemPickup @event, GameEventInfo _)
		{
			if (!IsWindows) return HookResult.Continue;
			
			var player = @event.Userid;
			if (player != null && player is { IsValid: true, Connected: PlayerConnectedState.PlayerConnected, PawnIsAlive: true, PlayerPawn.IsValid: true })
			{
				GiveOnItemPickup(player);
			}

			return HookResult.Continue;
		}

		private void RegisterListeners()
		{
			RegisterListener<Listeners.OnMapStart>(OnMapStart);

			RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
			RegisterEventHandler<EventRoundStart>(OnRoundStart);
			RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
			RegisterListener<Listeners.OnEntityCreated>(OnEntityCreated);

			if (Config.Additional.ShowSkinImage)
				RegisterListener<Listeners.OnTick>(OnTick);

			if (!IsWindows)
				VirtualFunctions.GiveNamedItemFunc.Hook(OnGiveNamedItemPost, HookMode.Post);
		}
	}
}