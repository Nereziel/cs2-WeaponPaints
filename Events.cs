using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace WeaponPaints
{
	public partial class WeaponPaints
	{

		[GameEventHandler]
		public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;

			if (player is null || !player.IsValid || player.IsBot || player.IsHLTV || player.SteamID.ToString().Length != 17 ||
				weaponSync == null || _database == null) return HookResult.Continue;

			PlayerInfo playerInfo = new PlayerInfo
			{
				UserId = player.UserId,
				Index = (int)player.Index,
				SteamId = player.SteamID.ToString(),
				Name = player.PlayerName,
				IpAddress = player.IpAddress?.Split(":")[0]
			};

			Task.Run(async () =>
			{
				// Run skin, knife, and glove tasks asynchronously
				var skinTask = Config.Additional.SkinEnabled ? weaponSync.GetWeaponPaintsFromDatabase(playerInfo) : Task.CompletedTask;
				var knifeTask = Config.Additional.KnifeEnabled ? weaponSync.GetKnifeFromDatabase(playerInfo) : Task.CompletedTask;
				var gloveTask = Config.Additional.GloveEnabled ? weaponSync.GetGloveFromDatabase(playerInfo) : Task.CompletedTask;

				// Await all tasks to complete
				await Task.WhenAll(skinTask, knifeTask, gloveTask);
			});

			return HookResult.Continue;
		}

		[GameEventHandler]
		public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
		{
			CCSPlayerController player = @event.Userid;

			if (player is null || !player.IsValid || !player.UserId.HasValue || player.IsBot ||
				player.IsHLTV || player.SteamID.ToString().Length != 17) return HookResult.Continue;

			PlayerInfo playerInfo = new PlayerInfo
			{
				UserId = player.UserId,
				Index = (int)player.Index,
				SteamId = player.SteamID.ToString(),
				Name = player.PlayerName,
				IpAddress = player.IpAddress?.Split(":")[0]
			};

			if (weaponSync != null)
				Task.Run(async () => await weaponSync.SyncWeaponPaintsToDatabase(playerInfo));

			if (Config.Additional.SkinEnabled)
				gPlayerWeaponsInfo.TryRemove((int)player.Index, out _);
			if (Config.Additional.KnifeEnabled)
				g_playersKnife.TryRemove((int)player.Index, out _);
			if (Config.Additional.GloveEnabled)
				g_playersGlove.TryRemove(player.Index, out _);

			commandsCooldown.Remove((int)player.UserId);

			return HookResult.Continue;
		}

		private void OnEntityCreated(CEntityInstance entity)
		{
			if (!Config.Additional.SkinEnabled) return;
			if (entity == null || !entity.IsValid || string.IsNullOrEmpty(entity.DesignerName)) return;
			string designerName = entity.DesignerName;
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
					CBasePlayerPawn? pawn = Utilities.GetEntityFromIndex<CCSPlayerPawn>((int)weaponOwner);
					//var pawn = new CBasePlayerPawn(NativeAPI.GetEntityFromIndex(weaponOwner));
					if (!pawn.IsValid) return;

					var playerIndex = (int)pawn.Controller.Index;
					var player = Utilities.GetPlayerFromIndex(playerIndex);
					if (!Utility.IsPlayerValid(player)) return;

					ChangeWeaponAttributes(weapon, player, isKnife);
				}
				catch (Exception) { }
			});
		}

		public HookResult OnPickup(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
		{
			CCSPlayerController? player = Utilities.GetEntityFromIndex<CCSPlayerPawn>((int)activator.Index).OriginalController.Value;

			if (player == null || player.IsBot || player.IsHLTV ||
				player.SteamID.ToString().Length != 17 || !g_knifePickupCount.TryGetValue((int)player.Index, out var pickupCount) ||
				!g_playersKnife.ContainsKey((int)player.Index))
			{
				return HookResult.Continue;
			}

			CBasePlayerWeapon weapon = new(caller.Handle);

			if (weapon.AttributeManager.Item.ItemDefinitionIndex != 42 && weapon.AttributeManager.Item.ItemDefinitionIndex != 59)
			{
				return HookResult.Continue;
			}

			if (pickupCount >= 2)
			{
				return HookResult.Continue;
			}

			if (g_playersKnife[(int)player.Index] != "weapon_knife")
			{
				pickupCount++;
				g_knifePickupCount[(int)player.Index] = pickupCount;

				if (Config.Additional.GiveKnifeAfterRemove)
				{
					AddTimer(0.10f, () =>
					{
						RefreshWeapons(player);
					});
				}
			}

			return HookResult.Continue;
		}

		private void OnMapStart(string mapName)
		{
			if (!Config.Additional.KnifeEnabled && !Config.Additional.SkinEnabled && !Config.Additional.GloveEnabled) return;

			if (_database != null)
				weaponSync = new WeaponSynchronization(_database, Config, GlobalShareApi, GlobalShareServerId);

			// TODO
			// needed for now
			AddTimer(2.0f, () =>
			{
				NativeAPI.IssueServerCommand("mp_t_default_melee \"\"");
				NativeAPI.IssueServerCommand("mp_ct_default_melee \"\"");
				NativeAPI.IssueServerCommand("mp_equipment_reset_rounds 0");

				if (Config.GlobalShare)
					GlobalShareConnect();
			});
		}

		private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;

			if (player is null || !player.IsValid || player.PlayerPawn == null ||
			!player.PlayerPawn.IsValid || player.IsHLTV
			|| !Config.Additional.KnifeEnabled && !Config.Additional.GloveEnabled)
				return HookResult.Continue;

			g_knifePickupCount[(int)player.Index] = 0;
			GiveKnifeToPlayer(player);

			Server.NextFrame(() =>
			{
				RefreshGloves(player);
			});

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
		private void OnTick()
		{
			foreach (var player in Utilities.GetPlayers().Where(p =>
							p is not null && p.IsValid &&
							(LifeState_t)p.LifeState == LifeState_t.LIFE_ALIVE && p.SteamID.ToString().Length == 17
							&& !p.IsBot && !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected && p.Team != CounterStrikeSharp.API.Modules.Utils.CsTeam.None
							)
				)
			{
				try
				{
					if (Config.Additional.ShowSkinImage && PlayerWeaponImage.ContainsKey(player.Slot) && !string.IsNullOrEmpty(PlayerWeaponImage[player.Slot]))
					{
						player.PrintToCenterHtml("<img src='{PATH}'</img>".Replace("{PATH}", PlayerWeaponImage[player.Slot]));
					}

					if (player.PlayerPawn?.IsValid != true || player.PlayerPawn?.Value?.IsValid != true)
						continue;

					var viewModels = GetPlayerViewModels(player);
					if (viewModels == null || viewModels.Length == 0)
						continue;

					var viewModel = viewModels[0];
					if (viewModel == null || viewModel.Value == null || viewModel.Value.Weapon == null || viewModel.Value.Weapon.Value == null)
						continue;

					var weapon = viewModel.Value.Weapon.Value;
					if (weapon == null || !weapon.IsValid || weapon.FallbackPaintKit == 0)
						continue;

					if (viewModel.Value.VMName.Contains("knife"))
						continue;

					var sceneNode = viewModel.Value.CBodyComponent?.SceneNode;
					if (sceneNode == null)
						continue;

					var skeleton = GetSkeletonInstance(sceneNode);
					if (skeleton == null)
						continue;

					int[] newPaints = { 1171, 1170, 1169, 1164, 1162, 1161, 1159, 1175, 1174, 1167, 1165, 1168, 1163, 1160, 1166, 1173 };
					if (newPaints.Contains(weapon.FallbackPaintKit))
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

					Utilities.SetStateChanged(viewModel.Value, "CBaseEntity", "m_CBodyComponent");
				}
				catch (Exception)
				{
				}
			}
		}

		private void RegisterListeners()
		{
			RegisterListener<Listeners.OnEntityCreated>(OnEntityCreated);
			//RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
			//RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
			RegisterListener<Listeners.OnMapStart>(OnMapStart);
			RegisterListener<Listeners.OnTick>(OnTick);

			RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
			RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Pre);
			RegisterEventHandler<EventRoundEnd>(OnRoundEnd);

			//RegisterEventHandler<EventItemPickup>(OnItemPickup);

			HookEntityOutput("weapon_knife", "OnPlayerPickup", OnPickup);
		}
	}
}