using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Entities;

namespace WeaponPaints
{
	public partial class WeaponPaints
	{
		[GameEventHandler]
		public HookResult OnClientFullConnect(EventPlayerConnectFull @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;

			if (player is null || !player.IsValid || player.IsBot || player.IsHLTV || player.SteamID.ToString().Length != 17 ||
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
			}
			catch (AggregateException)
			{
			}

			return HookResult.Continue;
		}

		[GameEventHandler]
		public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
		{
			CCSPlayerController player = @event.Userid;

			if (player is null || !player.IsValid || player.IsBot ||
				player.IsHLTV || player.SteamID.ToString().Length != 17) return HookResult.Continue;

			PlayerInfo playerInfo = new PlayerInfo
			{
				UserId = player.UserId,
				Slot = player.Slot,
				Index = (int)player.Index,
				SteamId = player.SteamID.ToString(),
				Name = player.PlayerName,
				IpAddress = player.IpAddress?.Split(":")[0]
			};

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

			commandsCooldown.Remove(player.Slot);

			return HookResult.Continue;
		}

		private void GivePlayerWeaponSkin(CCSPlayerController player, CBasePlayerWeapon weapon)
		{
			if (!Config.Additional.SkinEnabled) return;
			if (player is null || weapon is null || !weapon.IsValid || !Utility.IsPlayerValid(player)) return;
			if (!gPlayerWeaponsInfo.ContainsKey(player.Slot)) return;

			bool isKnife = weapon.DesignerName.Contains("knife") || weapon.DesignerName.Contains("bayonet");

			if (isKnife && !g_playersKnife.ContainsKey(player.Slot) || isKnife && g_playersKnife[player.Slot] == "weapon_knife") return;

			int[] newPaints = { 1171, 1170, 1169, 1164, 1162, 1161, 1159, 1175, 1174, 1167, 1165, 1168, 1163, 1160, 1166, 1173 };

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

				if (!isKnife)
				{
					if (newPaints.Contains(fallbackPaintKit))
					{
						UpdatePlayerWeaponMeshGroupMask(player, weapon, false);
					}
					else
					{
						UpdatePlayerWeaponMeshGroupMask(player, weapon, true);
					}
				}

				return;
			}

			if (!gPlayerWeaponsInfo[player.Slot].ContainsKey(weaponDefIndex) || gPlayerWeaponsInfo[player.Slot][weaponDefIndex].Paint == 0) return;

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

			if (!isKnife)
			{
				if (newPaints.Contains(fallbackPaintKit))
				{
					UpdatePlayerWeaponMeshGroupMask(player, weapon, false);
				}
				else
				{
					UpdatePlayerWeaponMeshGroupMask(player, weapon, true);
				}
			}
		}

		private void OnMapStart(string mapName)
		{
			if (!Config.Additional.KnifeEnabled && !Config.Additional.SkinEnabled && !Config.Additional.GloveEnabled) return;

			if (_database != null)
				weaponSync = new WeaponSynchronization(_database, Config);
		}

		private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;

			if (player is null || !player.IsValid || !Config.Additional.KnifeEnabled && !Config.Additional.GloveEnabled)
				return HookResult.Continue;

			CCSPlayerPawn? pawn = player.PlayerPawn.Value;

			if (pawn == null || !pawn.IsValid)
				return HookResult.Continue;

			g_knifePickupCount[player.Slot] = 0;

			GivePlayerAgent(player);
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
			/*
			NativeAPI.IssueServerCommand("mp_t_default_melee \"\"");
			NativeAPI.IssueServerCommand("mp_ct_default_melee \"\"");
			NativeAPI.IssueServerCommand("mp_equipment_reset_rounds 0");
			*/
			g_bCommandsAllowed = true;

			return HookResult.Continue;
		}

		/*
		public HookResult OnGiveNamedItemPost(DynamicHook hook)
		{
			var itemServices = hook.GetParam<CCSPlayer_ItemServices>(0);
			var weapon = hook.GetReturn<CBasePlayerWeapon>(0);
			if (!weapon.DesignerName.Contains("weapon"))
				return HookResult.Continue;

			var player = GetPlayerFromItemServices(itemServices);
			if (player != null)
				GivePlayerWeaponSkin(player, weapon);

			return HookResult.Continue;
		}
		*/

		public void OnEntityCreated(CEntityInstance entity)
		{
			var designerName = entity.DesignerName;

			if (designerName.Contains("weapon"))
			{
				Server.NextFrame(() =>
				{
					var weapon = new CBasePlayerWeapon(entity.Handle);
					if (weapon == null || !weapon.IsValid || weapon.OwnerEntity.Value == null) return;

					SteamID? _steamid = (SteamID)weapon.OriginalOwnerXuidLow;
					CCSWeaponBaseGun gun = weapon.As<CCSWeaponBaseGun>();
					CCSPlayerController? player = null;

					try
					{

						if (_steamid != null && _steamid.IsValid())
						{
							player = Utilities.GetPlayers().Where(p => p is not null && p.IsValid && p.SteamID == _steamid.SteamId64).FirstOrDefault();

							if (player == null)
								player = Utilities.GetPlayerFromSteamId(weapon.OriginalOwnerXuidLow);
						}
						else
							player = Utilities.GetPlayerFromIndex((int)weapon.OwnerEntity.Index) ?? Utilities.GetPlayerFromIndex((int)gun.OwnerEntity.Value!.Index);

						if (player == null || string.IsNullOrEmpty(player?.PlayerName)) return;
					}
					catch (Exception)
					{
						return;
					}

					if (player is null || !player.IsValid || !Utility.IsPlayerValid(player)) return;

					GivePlayerWeaponSkin(player, weapon);
				});
			}
		}

		private void OnTick()
		{
			foreach (var player in Utilities.GetPlayers().Where(p =>
							p is not null && p.IsValid && p.PlayerPawn != null && p.PlayerPawn.IsValid &&
							(LifeState_t)p.LifeState == LifeState_t.LIFE_ALIVE && p.SteamID.ToString().Length == 17
							&& !p.IsBot && !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected
							)
				)
			{
				if (Config.Additional.ShowSkinImage && PlayerWeaponImage.TryGetValue(player.Slot, out string? value) && !string.IsNullOrEmpty(value))
				{
					player.PrintToCenterHtml("<img src='{PATH}'</img>".Replace("{PATH}", value));
				}
			}
		}

		private void RegisterListeners()
		{
			RegisterListener<Listeners.OnMapStart>(OnMapStart);

			RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
			RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Pre);
			RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
			RegisterListener<Listeners.OnEntityCreated>(OnEntityCreated);
			RegisterListener<Listeners.OnTick>(OnTick);
			//VirtualFunctions.GiveNamedItemFunc.Hook(OnGiveNamedItemPost, HookMode.Post);
		}
	}
}