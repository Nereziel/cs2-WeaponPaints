using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace WeaponPaints
{
	public partial class WeaponPaints
	{
		private void OnClientPutInServer(int playerSlot)
		{
			CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

			if (player == null || !player.IsValid || player.IsBot || player.IsHLTV || weaponSync == null || player.Connected == PlayerConnectedState.PlayerDisconnecting) return;

			PlayerInfo playerInfo = new PlayerInfo
			{
				UserId = player.UserId,
				Index = (int)player.Index,
				SteamId = player.SteamID.ToString(),
				Name = player.PlayerName,
				IpAddress = player.IpAddress?.Split(":")[0]
			};

			if (!gPlayerWeaponsInfo.ContainsKey((int)player.Index))
			{
				Task.Run(async () =>
				{
					if (Config.Additional.SkinEnabled)
						await weaponSync.GetWeaponPaintsFromDatabase(playerInfo);
					if (Config.Additional.KnifeEnabled)
						await weaponSync.GetKnifeFromDatabase(playerInfo);
				});
			}
		}
		private void OnClientDisconnect(int playerSlot)
		{
			CCSPlayerController player = Utilities.GetPlayerFromSlot(playerSlot);

			if (player == null || !player.IsValid || player.IsBot || player.IsHLTV || player.UserId == null) return;

			if (Config.Additional.KnifeEnabled)
				g_playersKnife.TryRemove((int)player.Index, out _);
			if (Config.Additional.SkinEnabled)
			{
				if (gPlayerWeaponsInfo.TryRemove((int)player.Index, out var innerDictionary))
				{
					innerDictionary.Clear();
				}
			}
			if (commandsCooldown.ContainsKey((int)player.UserId))
			{
				commandsCooldown.Remove((int)player.UserId);
			}
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

			/*
			if (Config.Additional.SkinVisibilityFix)
				AddTimer(0.2f, () => RefreshSkins(player));
			*/

			return HookResult.Continue;
		}

		/*
		private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
		{
			if (@event.Defindex == 42 || @event.Defindex == 59)
			{
				CCSPlayerController? player = @event.Userid;
				if (player == null || !player.IsValid || !g_knifePickupCount.ContainsKey((int)player.Index) || player.IsBot || !g_playersKnife.ContainsKey((int)player.Index))
					return HookResult.Continue;


				if (g_knifePickupCount[(int)player.Index] >= 2) return HookResult.Continue;

				if (g_playersKnife.ContainsKey((int)player.Index)
					&&
				   g_playersKnife[(int)player.Index] != "weapon_knife")
				{
					g_knifePickupCount[(int)player.Index]++;

					RemovePlayerKnife(player, true);

					if (!PlayerHasKnife(player) && Config.Additional.GiveKnifeAfterRemove)
						AddTimer(0.3f, () => GiveKnifeToPlayer(player));
				}
			}
			return HookResult.Continue;
		}
		*/

		public HookResult OnPickup(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
		{
			CCSPlayerController? player = Utilities.GetEntityFromIndex<CCSPlayerPawn>((int)activator.Index).OriginalController.Value;

			if (player == null || player.IsBot || player.IsHLTV)
				return HookResult.Continue;

			if (player == null || !player.IsValid || player.SteamID.ToString() == "" ||
				!g_knifePickupCount.ContainsKey((int)player.Index) || !g_playersKnife.ContainsKey((int)player.Index))
				return HookResult.Continue;

			CBasePlayerWeapon weapon = new(caller.Handle);

			if (weapon.AttributeManager.Item.ItemDefinitionIndex != 42 && weapon.AttributeManager.Item.ItemDefinitionIndex != 59)
				return HookResult.Continue;

			if (g_knifePickupCount[(int)player.Index] >= 2) return HookResult.Continue;

			if (g_playersKnife[(int)player.Index] != "weapon_knife")
			{
				g_knifePickupCount[(int)player.Index]++;
				player.RemoveItemByDesignerName(weapon.DesignerName);
				if (Config.Additional.GiveKnifeAfterRemove)
					AddTimer(0.2f, () => GiveKnifeToPlayer(player));
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

			/*
			g_hTimerCheckSkinsData = AddTimer(10.0f, () =>
			{
				List<CCSPlayerController> players = Utilities.GetPlayers();

				foreach (CCSPlayerController player in players)
				{
					if (player.IsBot || player.IsHLTV || player.SteamID.ToString() == "") continue;
					if (gPlayerWeaponsInfo.ContainsKey((int)player.Index)) continue;

					PlayerInfo playerInfo = new PlayerInfo
					{
						UserId = player.UserId,
						Index = (int)player.Index,
						SteamId = player?.SteamID.ToString(),
						Name = player?.PlayerName,
						IpAddress = player?.IpAddress?.Split(":")[0]
					};

					if (Config.Additional.SkinEnabled && weaponSync != null)
						_ = weaponSync.GetWeaponPaintsFromDatabase(playerInfo);
					if (Config.Additional.KnifeEnabled && weaponSync != null)
						_ = weaponSync.GetKnifeFromDatabase(playerInfo);
				}
			}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE | CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
			*/
		}

		private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;

			if (player == null || !player.IsValid || player.IsBot || player.IsHLTV || weaponSync == null) return HookResult.Continue;

			PlayerInfo playerInfo = new PlayerInfo
			{
				UserId = player.UserId,
				Index = (int)player.Index,
				SteamId = player?.SteamID.ToString(),
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

			if (Config.Additional.KnifeEnabled && !PlayerHasKnife(player))
			{
				g_knifePickupCount[(int)player.Index] = 0;
				GiveKnifeToPlayer(player);
				//AddTimer(0.1f, () => GiveKnifeToPlayer(player));
			}

			/*
			if (Config.Additional.SkinVisibilityFix)
			{
				AddTimer(0.3f, () => RefreshSkins(player));
			}
			*/

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
			foreach (var player in Utilities.GetPlayers())
			{
				try
				{
					if (player == null || !player.IsValid || !player.PawnIsAlive || player.IsBot || player.IsHLTV || player.Connected == PlayerConnectedState.PlayerDisconnecting) continue;

					var viewModels = GetPlayerViewModels(player);

					if (viewModels == null) continue;

					var viewModel = viewModels[0];
					if (viewModel == null || viewModel.Value == null || viewModel.Value.Weapon == null || viewModel.Value.Weapon.Value == null) continue;
					CBasePlayerWeapon weapon = viewModel.Value.Weapon.Value;

					if (weapon == null || !weapon.IsValid) continue;

					var isKnife = viewModel.Value.VMName.Contains("knife");

					if (!isKnife)
					{
						if (
							viewModel.Value.CBodyComponent != null
							&& viewModel.Value.CBodyComponent.SceneNode != null
						)
						{
							var skeleton = GetSkeletonInstance(viewModel.Value.CBodyComponent.SceneNode);
							int[] array = { 1171, 1170, 1169, 1164, 1162, 1161, 1159, 1175, 1174, 1167, 1165, 1168, 1163, 1160, 1166, 1173 };
							int fallbackPaintKit = weapon.FallbackPaintKit;
							if (array.Contains(fallbackPaintKit))
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

						Utilities.SetStateChanged(viewModel.Value, "CBaseEntity", "m_CBodyComponent");
					}
				}
				catch (Exception)
				{ }
			}
		}

		private void RegisterListeners()
		{
			RegisterListener<Listeners.OnEntityCreated>(OnEntityCreated);
			RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
			RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
			RegisterListener<Listeners.OnMapStart>(OnMapStart);
			RegisterListener<Listeners.OnTick>(OnTick);

			//RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
			RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
			RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Pre);
			RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
			//RegisterEventHandler<EventItemPurchase>(OnEventItemPurchasePost);
			//RegisterEventHandler<EventItemPickup>(OnItemPickup);
			HookEntityOutput("weapon_knife", "OnPlayerPickup", OnPickup, HookMode.Pre);
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