using System.Collections.Concurrent;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Newtonsoft.Json.Linq;

namespace WeaponPaints;

public partial class WeaponPaints
{
	private static readonly ConcurrentDictionary<int, byte> GPlayersForceGloveKnifeRefresh = new();
	private static readonly ConcurrentDictionary<int, PendingSeedWearInput> GPlayersPendingSeedWearInput = new();

	private enum SeedWearInputKind
	{
		Seed,
		Wear
	}

	private enum SeedWearInputTarget
	{
		Knife,
		Gloves
	}

	private sealed class PendingSeedWearInput
	{
		public SeedWearInputKind Kind { get; init; }
		public SeedWearInputTarget Target { get; init; }
	}
	private const string BackMenuLabel = "← Back";

	private void OnCommandRefresh(CCSPlayerController? player, CommandInfo command)
	{
		if (!Config.Additional.CommandWpEnabled || !Config.Additional.SkinEnabled || !_gBCommandsAllowed) return;
		if (!Utility.IsPlayerValid(player)) return;

		if (player == null || !player.IsValid || player.UserId == null || player.IsBot) return;

		PlayerInfo? playerInfo = new PlayerInfo
		{
			UserId = player.UserId,
			Slot = player.Slot,
			Index = (int)player.Index,
			SteamId = player?.SteamID.ToString(),
			Name = player?.PlayerName,
			IpAddress = player?.IpAddress?.Split(":")[0]
		};

		try
		{
			if (player != null && !CommandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
			    player != null && DateTime.UtcNow >= (CommandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
		{
				CommandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);

				if (WeaponSync != null)
				{
					_ = Task.Run(async () => await WeaponSync.GetPlayerData(playerInfo));

					GivePlayerGloves(player);
					RefreshWeapons(player);
					GivePlayerAgent(player);
					GivePlayerMusicKit(player);
					AddTimer(0.15f, () => GivePlayerPin(player));
				}

				if (!string.IsNullOrEmpty(Localizer["wp_command_refresh_done"]))
				{
					player.Print(Localizer["wp_command_refresh_done"]);
				}
			return;
			}
			if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
		{
				player!.Print(Localizer["wp_command_cooldown"]);
			}
		}
		catch (Exception) { }
	}

	private void OnCommandWS(CCSPlayerController? player, CommandInfo command)
	{
		if (!Config.Additional.SkinEnabled) return;
		if (!Utility.IsPlayerValid(player)) return;

		if (!string.IsNullOrEmpty(Localizer["wp_info_website"]))
		{
			player!.Print(Localizer["wp_info_website", Config.Website]);
		}
		if (!string.IsNullOrEmpty(Localizer["wp_info_refresh"]))
		{
			player!.Print(Localizer["wp_info_refresh"]);
		}

		if (Config.Additional.GloveEnabled)
			if (!string.IsNullOrEmpty(Localizer["wp_info_glove"]))
		{
				player!.Print(Localizer["wp_info_glove"]);
			}

		if (Config.Additional.AgentEnabled)
			if (!string.IsNullOrEmpty(Localizer["wp_info_agent"]))
		{
				player!.Print(Localizer["wp_info_agent"]);
			}

		if (Config.Additional.MusicEnabled)
			if (!string.IsNullOrEmpty(Localizer["wp_info_music"]))
		{
				player!.Print(Localizer["wp_info_music"]);
			}
		
		if (Config.Additional.PinsEnabled)
			if (!string.IsNullOrEmpty(Localizer["wp_info_pin"]))
		{
				player!.Print(Localizer["wp_info_pin"]);
			}

		if (!string.IsNullOrEmpty(Localizer["wp_info_seed"]))
		{
			player!.Print(Localizer["wp_info_seed"]);
		}

		if (!string.IsNullOrEmpty(Localizer["wp_info_wear"]))
		{
			player!.Print(Localizer["wp_info_wear"]);
		}

		if (!Config.Additional.KnifeEnabled) return;
		if (!string.IsNullOrEmpty(Localizer["wp_info_knife"]))
		{
			player!.Print(Localizer["wp_info_knife"]);
		}
	}

	private void RegisterCommands()
	{
		_config.Additional.CommandStattrak.ForEach(c =>
		{
			AddCommand($"css_{c}", "Stattrak toggle", (player, info) =>
		{
				if (!Utility.IsPlayerValid(player)) return;

				OnCommandStattrak(player, info);
			});
		});

		_config.Additional.CommandSeed.ForEach(c =>
		{
			AddCommand($"css_{c}", "Set active weapon seed", (player, info) =>
		{
				if (!Utility.IsPlayerValid(player)) return;

				OnCommandSeed(player, info);
			});
		});

		_config.Additional.CommandWear.ForEach(c =>
		{
			AddCommand($"css_{c}", "Set active weapon wear", (player, info) =>
		{
				if (!Utility.IsPlayerValid(player)) return;

				OnCommandWear(player, info);
			});
		});

		AddCommandListener("say", OnPlayerChatSeedWearInput, HookMode.Pre);
		AddCommandListener("say_team", OnPlayerChatSeedWearInput, HookMode.Pre);

		_config.Additional.CommandSkin.ForEach(c =>
		{
			AddCommand($"css_{c}", "Skins info", (player, info) =>
		{
				if (!Utility.IsPlayerValid(player)) return;
				OnCommandWS(player, info);
			});
		});
			
		_config.Additional.CommandRefresh.ForEach(c =>
		{
			AddCommand($"css_{c}", "Skins refresh", (player, info) =>
		{
				if (!Utility.IsPlayerValid(player)) return;
				OnCommandRefresh(player, info);
			});
		});

		if (Config.Additional.CommandKillEnabled)
		{
			_config.Additional.CommandKill.ForEach(c =>
		{
				AddCommand($"css_{c}", "kill yourself", (player, _) =>
				{
					if (player == null || !Utility.IsPlayerValid(player) || player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid) return;

					player.PlayerPawn.Value.CommitSuicide(true, false);
				});
			});
		}

		AddCommand("wp_refresh", "Admin refresh player skins", (player, info) =>
		{
			OnCommandSkinRefresh(player, info);
		});
	}

	private void OnCommandSkinRefresh(CCSPlayerController? player, CommandInfo command)
	{
		if (!Config.Additional.CommandWpEnabled || !Config.Additional.SkinEnabled || !_gBCommandsAllowed) return;
		if (player != null)
		{
			return;
		}

		var args = command.GetArg(1);
			
		if (string.IsNullOrEmpty(args))
		{
			Console.WriteLine("[WeaponPaints] Usage: wp_refresh <steamid64|all>");
			Console.WriteLine("[WeaponPaints] Examples:");
			Console.WriteLine("[WeaponPaints]   wp_refresh all - Refresh skins for all players");
			Console.WriteLine("[WeaponPaints]   wp_refresh 76561198012345678 - Refresh skins by SteamID64");
			return;
		}

		var targetPlayers = new List<CCSPlayerController>();

		if (args.Equals("all", StringComparison.OrdinalIgnoreCase))
		{
			targetPlayers = Utilities.GetPlayers().Where(p => 
				p != null && p.IsValid && !p.IsBot && p.UserId != null).ToList();
			
			if (targetPlayers.Count == 0)
		{
				Console.WriteLine("[WeaponPaints] No players connected to refresh.");
			return;
			}
			
			Console.WriteLine($"[WeaponPaints] Refreshing skins for {targetPlayers.Count} players...");
		}
		else
		{
			var foundPlayer = Utilities.GetPlayers().FirstOrDefault(p => 
				p != null && p.IsValid && !p.IsBot && p.UserId != null && 
				 p.SteamID.ToString() == args);

			if (foundPlayer == null)
		{
				Console.WriteLine($"[WeaponPaints] Player with SteamID64 '{args}' not found.");
			return;
			}

			targetPlayers.Add(foundPlayer);
			Console.WriteLine($"[WeaponPaints] Refreshing skins for {foundPlayer.PlayerName}...");
		}

		foreach (var targetPlayer in targetPlayers)
		{
			try
		{
				PlayerInfo? playerInfo = new PlayerInfo
				{
					UserId = targetPlayer.UserId,
					Slot = targetPlayer.Slot,
					Index = (int)targetPlayer.Index,
					SteamId = targetPlayer.SteamID.ToString(),
					Name = targetPlayer.PlayerName,
					IpAddress = targetPlayer.IpAddress?.Split(":")[0]
				};

				if (WeaponSync != null)
				{
					_ = Task.Run(async () => await WeaponSync.GetPlayerData(playerInfo));
				}

				GivePlayerGloves(targetPlayer);
				RefreshWeapons(targetPlayer);
				GivePlayerAgent(targetPlayer);
				GivePlayerMusicKit(targetPlayer);
				AddTimer(0.15f, () => GivePlayerPin(targetPlayer));

				if (!string.IsNullOrEmpty(Localizer["wp_command_refresh_done"]))
				{
					targetPlayer.Print(Localizer["wp_command_refresh_done"]);
				}

				Console.WriteLine($"[WeaponPaints] Skins refreshed for {targetPlayer.PlayerName}");
			}
			catch (Exception ex)
		{
				Console.WriteLine($"[WeaponPaints] Error refreshing skins for {targetPlayer.PlayerName}: {ex.Message}");
			}
		}

		Console.WriteLine("[WeaponPaints] Refresh process completed.");
	}


	private void OnCommandStattrak(CCSPlayerController? player, CommandInfo commandInfo)
	{
		if (player == null || !player.IsValid) return;
		
		var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
		
		if (weapon == null || !weapon.IsValid)
			return;

		if (!HasChangedPaint(player, weapon.AttributeManager.Item.ItemDefinitionIndex, out var weaponInfo) || weaponInfo == null)
			return;
		
		weaponInfo.StatTrak = !weaponInfo.StatTrak;
		RefreshWeapons(player);

		if (!string.IsNullOrEmpty(Localizer["wp_stattrak_action"]))
		{
			player.Print(Localizer["wp_stattrak_action"]);
		}
	}


	private void OnCommandSeed(CCSPlayerController? player, CommandInfo commandInfo)
	{
		if (player == null || !player.IsValid) return;

		var rawSeed = commandInfo.GetArg(1);
		if (IsPlayerHoldingKnife(player) && string.IsNullOrWhiteSpace(rawSeed))
		{
			OpenSeedWearTargetMenu(player, SeedWearInputKind.Seed);
			return;
		}

		var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
		if (weapon == null || !weapon.IsValid || WeaponSync == null) return;

		if (!HasChangedPaint(player, weapon.AttributeManager.Item.ItemDefinitionIndex, out var weaponInfo) || weaponInfo == null)
			return;

		if (!int.TryParse(rawSeed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed))
		{
			if (!string.IsNullOrEmpty(Localizer["wp_seed_usage"]))
			{
				player.Print(Localizer["wp_seed_usage"]);
			}
			return;
		}

		ApplyKnifeSeed(player, weapon, weaponInfo, seed);
	}

	private void OnCommandWear(CCSPlayerController? player, CommandInfo commandInfo)
	{
		if (player == null || !player.IsValid) return;

		var rawWear = commandInfo.GetArg(1);
		if (IsPlayerHoldingKnife(player) && string.IsNullOrWhiteSpace(rawWear))
		{
			OpenSeedWearTargetMenu(player, SeedWearInputKind.Wear);
			return;
		}

		var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
		if (weapon == null || !weapon.IsValid || WeaponSync == null) return;

		if (!HasChangedPaint(player, weapon.AttributeManager.Item.ItemDefinitionIndex, out var weaponInfo) || weaponInfo == null)
			return;

		if (!float.TryParse(rawWear, NumberStyles.Float, CultureInfo.InvariantCulture, out var wear) &&
		    !float.TryParse(rawWear, NumberStyles.Float, CultureInfo.CurrentCulture, out wear))
		{
			if (!string.IsNullOrEmpty(Localizer["wp_wear_usage"]))
			{
				player.Print(Localizer["wp_wear_usage"]);
			}
			return;
		}

		ApplyKnifeWear(player, weapon, weaponInfo, wear);
	}

	private HookResult OnPlayerChatSeedWearInput(CCSPlayerController? player, CommandInfo commandInfo)
	{
		if (!Utility.IsPlayerValid(player) || player is null) return HookResult.Continue;
		if (!GPlayersPendingSeedWearInput.TryGetValue(player.Slot, out var pending)) return HookResult.Continue;

		var input = commandInfo.GetArg(1)?.Trim();
		if (string.IsNullOrWhiteSpace(input)) return HookResult.Continue;

		input = input.Trim('"');
		if (input.StartsWith('!') || input.StartsWith('/')) return HookResult.Continue;

		if (pending.Kind == SeedWearInputKind.Seed)
		{
			if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed))
			{
				player.Print(Localizer["wp_seedwear_invalid_seed"]);
				return HookResult.Handled;
			}

			if (pending.Target == SeedWearInputTarget.Knife)
			{
				ApplyPendingKnifeSeed(player, seed);
			}
			else
			{
				ApplyPendingGloveSeed(player, seed);
			}
		}
		else
		{
			if (!float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var wear) &&
			    !float.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out wear))
			{
				player.Print(Localizer["wp_seedwear_invalid_wear"]);
				return HookResult.Handled;
			}

			if (pending.Target == SeedWearInputTarget.Knife)
			{
				ApplyPendingKnifeWear(player, wear);
			}
			else
			{
				ApplyPendingGloveWear(player, wear);
			}
		}

		GPlayersPendingSeedWearInput.TryRemove(player.Slot, out _);
		return HookResult.Handled;
	}

	private void OpenSeedWearTargetMenu(CCSPlayerController player, SeedWearInputKind kind)
	{
		var title = kind == SeedWearInputKind.Seed ? Localizer["wp_seedwear_menu_seed_title"] : Localizer["wp_seedwear_menu_wear_title"];
		var targetMenu = Utility.CreateMenu(title);
		if (targetMenu == null) return;

		targetMenu.PostSelectAction = PostSelectAction.Close;
		targetMenu.AddMenuOption(Localizer["wp_seedwear_target_knife"], (menuPlayer, _) => StartSeedWearChatInput(menuPlayer, kind, SeedWearInputTarget.Knife));
		targetMenu.AddMenuOption(Localizer["wp_seedwear_target_gloves"], (menuPlayer, _) => StartSeedWearChatInput(menuPlayer, kind, SeedWearInputTarget.Gloves));
		targetMenu.Open(player);
	}

	private void StartSeedWearChatInput(CCSPlayerController? player, SeedWearInputKind kind, SeedWearInputTarget target)
	{
		if (!Utility.IsPlayerValid(player) || player is null) return;

		GPlayersPendingSeedWearInput[player.Slot] = new PendingSeedWearInput
		{
			Kind = kind,
			Target = target
		};

		if (kind == SeedWearInputKind.Seed)
		{
			player.Print(Localizer["wp_seedwear_prompt_seed"]);
		}
		else
		{
			player.Print(Localizer["wp_seedwear_prompt_wear"]);
		}
	}

	private void ApplyPendingKnifeSeed(CCSPlayerController player, int seed)
	{
		var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
		if (weapon == null || !weapon.IsValid || !IsKnifeWeapon(weapon))
		{
			player.Print(Localizer["wp_seedwear_hold_knife_seed"]);
			return;
		}

		if (!HasChangedPaint(player, weapon.AttributeManager.Item.ItemDefinitionIndex, out var weaponInfo) || weaponInfo == null)
		{
			player.Print(Localizer["wp_seedwear_choose_knife_skin"]);
			return;
		}

		ApplyKnifeSeed(player, weapon, weaponInfo, seed);
	}

	private void ApplyPendingKnifeWear(CCSPlayerController player, float wear)
	{
		var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
		if (weapon == null || !weapon.IsValid || !IsKnifeWeapon(weapon))
		{
			player.Print(Localizer["wp_seedwear_hold_knife_wear"]);
			return;
		}

		if (!HasChangedPaint(player, weapon.AttributeManager.Item.ItemDefinitionIndex, out var weaponInfo) || weaponInfo == null)
		{
			player.Print(Localizer["wp_seedwear_choose_knife_skin"]);
			return;
		}

		ApplyKnifeWear(player, weapon, weaponInfo, wear);
	}

	private void ApplyPendingGloveSeed(CCSPlayerController player, int seed)
	{
		if (!TryGetPlayerGloveInfo(player, out var gloveDefIndex, out var weaponInfo) || weaponInfo == null)
		{
			player.Print(Localizer["wp_seedwear_choose_custom_gloves"]);
			return;
		}

		weaponInfo.Seed = Math.Clamp(seed, 0, 1000);
		SavePaintCustomization(player.Slot, player.Team, gloveDefIndex, weaponInfo.Paint, weaponInfo);
		RefreshPlayerGlovesAfterChatInput(player);
		SyncWeaponPaintsAfterSeedWear(player);

		if (!string.IsNullOrEmpty(Localizer["wp_seed_action"]))
		{
			player.Print(Localizer["wp_seed_action", weaponInfo.Seed]);
		}
	}

	private void ApplyPendingGloveWear(CCSPlayerController player, float wear)
	{
		if (!TryGetPlayerGloveInfo(player, out var gloveDefIndex, out var weaponInfo) || weaponInfo == null)
		{
			player.Print(Localizer["wp_seedwear_choose_custom_gloves"]);
			return;
		}

		weaponInfo.Wear = ClampWearValue(wear);
		SavePaintCustomization(player.Slot, player.Team, gloveDefIndex, weaponInfo.Paint, weaponInfo);
		ClearTemporaryWeaponWear(player.Slot, gloveDefIndex);
		RefreshPlayerGlovesAfterChatInput(player);
		SyncWeaponPaintsAfterSeedWear(player);

		if (!string.IsNullOrEmpty(Localizer["wp_wear_action"]))
		{
			player.Print(Localizer["wp_wear_action", weaponInfo.Wear.ToString("0.######", CultureInfo.InvariantCulture)]);
		}
	}

	private void ApplyKnifeSeed(CCSPlayerController player, CBasePlayerWeapon weapon, WeaponInfo weaponInfo, int seed)
	{
		weaponInfo.Seed = Math.Clamp(seed, 0, 1000);
		SavePaintCustomization(player.Slot, player.Team, weapon.AttributeManager.Item.ItemDefinitionIndex, weaponInfo.Paint, weaponInfo);
		RefreshWeapons(player);
		SyncWeaponPaintsAfterSeedWear(player);

		if (!string.IsNullOrEmpty(Localizer["wp_seed_action"]))
		{
			player.Print(Localizer["wp_seed_action", weaponInfo.Seed]);
		}
	}

	private void ApplyKnifeWear(CCSPlayerController player, CBasePlayerWeapon weapon, WeaponInfo weaponInfo, float wear)
	{
		weaponInfo.Wear = ClampWearValue(wear);
		SavePaintCustomization(player.Slot, player.Team, weapon.AttributeManager.Item.ItemDefinitionIndex, weaponInfo.Paint, weaponInfo);
		ClearTemporaryWeaponWear(player.Slot, weapon.AttributeManager.Item.ItemDefinitionIndex);
		RefreshWeapons(player);
		SyncWeaponPaintsAfterSeedWear(player);

		if (!string.IsNullOrEmpty(Localizer["wp_wear_action"]))
		{
			player.Print(Localizer["wp_wear_action", weaponInfo.Wear.ToString("0.######", CultureInfo.InvariantCulture)]);
		}
	}

	private void RefreshPlayerGlovesAfterChatInput(CCSPlayerController player)
	{
		GPlayersForceGloveKnifeRefresh[player.Slot] = 1;
		AddTimer(0.05f, () => GivePlayerGloves(player), TimerFlags.STOP_ON_MAPCHANGE);
	}

	private void SyncWeaponPaintsAfterSeedWear(CCSPlayerController player)
	{
		if (WeaponSync == null) return;

		try
		{
			var playerInfo = new PlayerInfo
			{
				UserId = player.UserId,
				Slot = player.Slot,
				Index = (int)player.Index,
				SteamId = player.SteamID.ToString(),
				Name = player.PlayerName,
				IpAddress = player.IpAddress?.Split(":")[0]
			};

			_ = Task.Run(async () => await WeaponSync.SyncWeaponPaintsToDatabase(playerInfo));
		}
		catch (Exception ex)
		{
			Utility.Log($"Error syncing weapon seed/wear: {ex.Message}");
		}
	}

	private static bool IsPlayerHoldingKnife(CCSPlayerController player)
	{
		var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
		return weapon != null && weapon.IsValid && IsKnifeWeapon(weapon);
	}

	private static bool IsKnifeWeapon(CBasePlayerWeapon weapon)
	{
		return weapon.DesignerName.Contains("knife", StringComparison.OrdinalIgnoreCase) ||
		       weapon.DesignerName.Contains("bayonet", StringComparison.OrdinalIgnoreCase);
	}

	private static string GetPaintCustomizationKey(CsTeam team, int weaponDefIndex, int paintId)
	{
		return $"{(int)team}:{weaponDefIndex}:{paintId}";
	}

	private static WeaponPaintCustomization GetOrCreatePaintCustomization(int slot, CsTeam team, int weaponDefIndex, int paintId)
	{
		var playerCustomizations = GPlayerWeaponPaintCustomizations.GetOrAdd(slot, _ => new ConcurrentDictionary<string, WeaponPaintCustomization>());
		return playerCustomizations.GetOrAdd(GetPaintCustomizationKey(team, weaponDefIndex, paintId), _ => new WeaponPaintCustomization
		{
			Wear = DefaultFactoryNewWear,
			Seed = 0
		});
	}

	private static void SavePaintCustomization(int slot, CsTeam team, int weaponDefIndex, int paintId, WeaponInfo weaponInfo)
	{
		if (paintId <= 0) return;

		var customization = GetOrCreatePaintCustomization(slot, team, weaponDefIndex, paintId);
		customization.Wear = ClampWearValue(weaponInfo.Wear);
		customization.Seed = Math.Clamp(weaponInfo.Seed, 0, 1000);
	}

	private static float ClampWearValue(float wear)
	{
		return Math.Clamp(wear, 0.0f, 1.0f);
	}

	private void ClearTemporaryWeaponWear(int slot, int weaponDefIndex)
	{
		if (_temporaryPlayerWeaponWear.TryGetValue(slot, out var playerWear))
		{
			playerWear.TryRemove(weaponDefIndex, out _);
		}
	}

	private void SetupKnifeMenu()
	{
		if (!Config.Additional.KnifeEnabled || !_gBCommandsAllowed) return;

		var knivesOnly = WeaponList
			.Where(pair => pair.Key.StartsWith("weapon_knife") || pair.Key.StartsWith("weapon_bayonet"))
			.ToDictionary(pair => pair.Key, pair => pair.Value);

		var giveItemMenu = Utility.CreateMenu(Localizer["wp_knife_menu_title"]);
			
		var handleGive = (CCSPlayerController player, ChatMenuOption option) =>
		{
			if (!Utility.IsPlayerValid(player)) return;

			var playerKnives = GPlayersKnife.GetOrAdd(player.Slot, new ConcurrentDictionary<CsTeam, string>());
			var teamsToCheck = player.TeamNum < 2 
				? new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist } 
				: [player.Team];
			
			var knifeName = option.Text;
			var knifeKey = knivesOnly.FirstOrDefault(x => x.Value == knifeName).Key;
			if (string.IsNullOrEmpty(knifeKey)) return;
			if (!string.IsNullOrEmpty(Localizer["wp_knife_menu_select"]))
		{
				player.Print(Localizer["wp_knife_menu_select", knifeName]);
			}

			if (!string.IsNullOrEmpty(Localizer["wp_knife_menu_kill"]) && Config.Additional.CommandKillEnabled)
		{
				player.Print(Localizer["wp_knife_menu_kill"]);
			}

			PlayerInfo playerInfo = new PlayerInfo
		{
				UserId = player.UserId,
				Slot = player.Slot,
				Index = (int)player.Index,
				SteamId = player.SteamID.ToString(),
				Name = player.PlayerName,
				IpAddress = player.IpAddress?.Split(":")[0]
			};
			
			foreach (var team in teamsToCheck)
		{
				// Attempt to get the existing knives
				playerKnives[team] = knifeKey;
			}
			
			if (_gBCommandsAllowed && (LifeState_t)player.LifeState == LifeState_t.LIFE_ALIVE)
				RefreshWeapons(player);

			if (WeaponSync != null)
				_ = Task.Run(async () => await WeaponSync.SyncKnifeToDatabase(playerInfo, knifeKey, teamsToCheck));
		};
		foreach (var knifePair in knivesOnly)
		{
			giveItemMenu?.AddMenuOption(knifePair.Value, handleGive);
		}
		_config.Additional.CommandKnife.ForEach(c =>
		{
			AddCommand($"css_{c}", "Knife Menu", (player, _) =>
		{
				if (giveItemMenu == null) return;
				if (!Utility.IsPlayerValid(player) || !_gBCommandsAllowed) return;

				if (player == null || player.UserId == null) return;

				if (!CommandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
				    DateTime.UtcNow >= (CommandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
				{
					CommandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
					giveItemMenu.PostSelectAction = PostSelectAction.Close;
					
					giveItemMenu.Open(player);

					return;
				}
				if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
				{
					player.Print(Localizer["wp_command_cooldown"]);
				}
			});
		});
	}


	private void SetupSkinsMenu()
	{
		var classNamesByWeapon = WeaponList
			.Where(kvp => kvp.Key != "weapon_knife")
			.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

		var weaponSelectionMenu = Utility.CreateMenu(Localizer["wp_skin_menu_weapon_title"]);
		if (weaponSelectionMenu == null) return;
		weaponSelectionMenu.PostSelectAction = PostSelectAction.Nothing;

		void OpenSkinSubMenu(CCSPlayerController? player, string selectedWeapon, string selectedWeaponClassname)
		{
			if (!Utility.IsPlayerValid(player) || player is null) return;

			var skinsForSelectedWeapon = SkinsList.Where(skin =>
				skin.TryGetValue("weapon_name", out var weaponName) &&
				weaponName?.ToString() == selectedWeaponClassname
			).ToList();

			var skinSubMenu = Utility.CreateMenu(Localizer["wp_skin_menu_skin_title", selectedWeapon]);
			if (skinSubMenu == null) return;
			skinSubMenu.PostSelectAction = PostSelectAction.Nothing;

			skinSubMenu.AddMenuOption(BackMenuLabel, (backPlayer, _) =>
			{
				if (Utility.IsPlayerValid(backPlayer))
				{
					weaponSelectionMenu.Open(backPlayer);
				}
			});

			var handleSkinSelection = (CCSPlayerController p, ChatMenuOption opt) =>
			{
				if (!Utility.IsPlayerValid(p)) return;

				var firstSkin = SkinsList.FirstOrDefault(skin =>
				{
					if (skin.TryGetValue("weapon_name", out var weaponName))
					{
						return weaponName?.ToString() == selectedWeaponClassname;
					}
					return false;
				});

				var selectedSkin = opt.Text;
				var selectedPaintId = selectedSkin[(selectedSkin.LastIndexOf('(') + 1)..].Trim(')');

				if (firstSkin == null ||
				    !firstSkin.TryGetValue("weapon_defindex", out var weaponDefIndexObj) ||
				    !int.TryParse(weaponDefIndexObj.ToString(), out var weaponDefIndex) ||
				    !int.TryParse(selectedPaintId, out var paintId)) return;

				if (Config.Additional.ShowSkinImage)
				{
					var foundSkin = SkinsList.FirstOrDefault(skin =>
						((int?)skin["weapon_defindex"] ?? 0) == weaponDefIndex &&
						((int?)skin["paint"] ?? 0) == paintId &&
						skin["image"] != null
					);
					var image = foundSkin?["image"]?.ToString() ?? "";
					_playerWeaponImage[p.Slot] = image;
					AddTimer(2.0f, () => _playerWeaponImage.Remove(p.Slot), TimerFlags.STOP_ON_MAPCHANGE);
				}

				p.Print(Localizer["wp_skin_menu_select", selectedSkin]);
				var playerSkins = GPlayerWeaponsInfo.GetOrAdd(p.Slot, new ConcurrentDictionary<CsTeam, ConcurrentDictionary<int, WeaponInfo>>());

				var teamsToCheck = p.TeamNum < 2
					? new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist }
					: [p.Team];

				foreach (var team in teamsToCheck)
				{
					var teamWeapons = playerSkins.GetOrAdd(team, _ => new ConcurrentDictionary<int, WeaponInfo>());
					var value = teamWeapons.GetOrAdd(weaponDefIndex, _ => new WeaponInfo());

					if (value.Paint > 0)
					{
						SavePaintCustomization(p.Slot, team, weaponDefIndex, value.Paint, value);
					}

					var customization = GetOrCreatePaintCustomization(p.Slot, team, weaponDefIndex, paintId);
					value.Paint = paintId;
					value.Wear = customization.Wear;
					value.Seed = customization.Seed;
					ClearTemporaryWeaponWear(p.Slot, weaponDefIndex);
				}

				var playerInfo = new PlayerInfo
				{
					UserId = p.UserId,
					Slot = p.Slot,
					Index = (int)p.Index,
					SteamId = p.SteamID.ToString(),
					Name = p.PlayerName,
					IpAddress = p.IpAddress?.Split(":")[0]
				};

				if (!_gBCommandsAllowed || (LifeState_t)p.LifeState != LifeState_t.LIFE_ALIVE ||
				    WeaponSync == null) return;

				RefreshWeapons(p);

				try
				{
					_ = Task.Run(async () => await WeaponSync.SyncWeaponPaintsToDatabase(playerInfo));
				}
				catch (Exception ex)
				{
					Utility.Log($"Error syncing weapon paints: {ex.Message}");
				}
			};

			foreach (var skin in skinsForSelectedWeapon)
			{
				if (!skin.TryGetValue("paint_name", out var paintNameObj) ||
				    !skin.TryGetValue("paint", out var paintObj)) continue;

				var paintName = paintNameObj?.ToString();
				var paint = paintObj?.ToString();

				if (!string.IsNullOrEmpty(paintName) && !string.IsNullOrEmpty(paint))
				{
					skinSubMenu.AddMenuOption($"{paintName} ({paint})", handleSkinSelection);
				}
			}

			AddTimer(0.05f, () =>
			{
				if (Utility.IsPlayerValid(player))
				{
					skinSubMenu.Open(player);
				}
			}, TimerFlags.STOP_ON_MAPCHANGE);
		}

		var groupedWeapons = WeaponList
			.Where(kvp => kvp.Key != "weapon_knife")
			.GroupBy(kvp => GetSkinWeaponMenuCategory(kvp.Key))
			.OrderBy(group => GetSkinWeaponMenuCategoryOrder(group.Key))
			.ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
			.ToList();

		foreach (var group in groupedWeapons)
		{
			var categoryName = group.Key;
			var weaponsInCategory = group
				.OrderBy(kvp => kvp.Value, StringComparer.OrdinalIgnoreCase)
				.ToList();

			weaponSelectionMenu.AddMenuOption(categoryName, (player, _) =>
			{
				if (!Utility.IsPlayerValid(player) || player is null) return;

				var categoryMenu = Utility.CreateMenu(categoryName);
				if (categoryMenu == null) return;
				categoryMenu.PostSelectAction = PostSelectAction.Nothing;

				categoryMenu.AddMenuOption(BackMenuLabel, (backPlayer, _) =>
				{
					if (Utility.IsPlayerValid(backPlayer))
					{
						weaponSelectionMenu.Open(backPlayer);
					}
				});

				foreach (var weaponPair in weaponsInCategory)
				{
					var weaponName = weaponPair.Value;
					var weaponClassname = weaponPair.Key;
					categoryMenu.AddMenuOption(weaponName, (submenuPlayer, __) => OpenSkinSubMenu(submenuPlayer, weaponName, weaponClassname));
				}

				AddTimer(0.05f, () =>
				{
					if (Utility.IsPlayerValid(player))
					{
						categoryMenu.Open(player);
					}
				}, TimerFlags.STOP_ON_MAPCHANGE);
			});
		}

		_config.Additional.CommandSkinSelection.ForEach(c =>
		{
			AddCommand($"css_{c}", "Skins selection menu", (player, _) =>
			{
				if (!Utility.IsPlayerValid(player)) return;

				if (player == null || player.UserId == null) return;

				if (!CommandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
				    DateTime.UtcNow >= (CommandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
				{
					CommandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
					weaponSelectionMenu.Open(player);
					return;
				}
				if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
				{
					player.Print(Localizer["wp_command_cooldown"]);
				}
			});
		});
	}

	private static string GetSkinWeaponMenuCategory(string weaponClassname)
	{
		if (weaponClassname.StartsWith("weapon_knife", StringComparison.OrdinalIgnoreCase) ||
		    weaponClassname.Equals("weapon_bayonet", StringComparison.OrdinalIgnoreCase))
		{
			return "Knives";
		}

		return weaponClassname switch
		{
			"weapon_ak47" or "weapon_aug" or "weapon_famas" or "weapon_galilar" or "weapon_m4a1" or
				"weapon_m4a1_silencer" or "weapon_sg556" => "Rifles",

			"weapon_awp" or "weapon_g3sg1" or "weapon_scar20" or "weapon_ssg08" => "Snipers",

			"weapon_deagle" or "weapon_elite" or "weapon_fiveseven" or "weapon_glock" or
				"weapon_hkp2000" or "weapon_p250" or "weapon_tec9" or "weapon_usp_silencer" or
				"weapon_cz75a" or "weapon_revolver" => "Pistols",

			"weapon_mac10" or "weapon_p90" or "weapon_mp5sd" or "weapon_ump45" or "weapon_bizon" or
				"weapon_mp7" or "weapon_mp9" => "SMGs",

			"weapon_m249" or "weapon_negev" or "weapon_xm1014" or "weapon_mag7" or
				"weapon_nova" or "weapon_sawedoff" => "Heavy",

			_ => "Other"
		};
	}

	private static int GetSkinWeaponMenuCategoryOrder(string category)
	{
		return category switch
		{
			"Knives" => 0,
			"Rifles" => 1,
			"Pistols" => 2,
			"SMGs" => 3,
			"Heavy" => 4,
			"Snipers" => 5,
			_ => 99
		};
	}

	private void SetupGlovesMenu()
	{
		var glovesSelectionMenu = Utility.CreateMenu(Localizer["wp_glove_menu_title"]);
		if (glovesSelectionMenu == null) return;
		glovesSelectionMenu.PostSelectAction = PostSelectAction.Nothing;

		var gloveEntries = GlovesList
			.Select(glove =>
		{
				var (gloveType, gloveFinish) = GetGloveMenuLabels(glove);
				return new
				{
					Glove = glove,
					Type = gloveType,
					Finish = gloveFinish,
					IsDefault = int.TryParse(glove["paint"]?.ToString(), out var paint) && paint == 0
				};
			})
			.Where(glove => glove.Type.Length > 0 && glove.Finish.Length > 0)
			.ToList();

		var defaultGlove = gloveEntries.FirstOrDefault(glove => glove.IsDefault);
		if (defaultGlove != null)
		{
			glovesSelectionMenu.AddMenuOption(defaultGlove.Finish, (player, _) => ApplyGloveSelection(player, defaultGlove.Glove));
		}

		var groupedGloves = gloveEntries
			.Where(glove => !glove.IsDefault)
			.GroupBy(glove => glove.Type, StringComparer.OrdinalIgnoreCase)
			.OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
			.ToList();

		foreach (var gloveGroup in groupedGloves)
		{
			glovesSelectionMenu.AddMenuOption(gloveGroup.Key, (player, _) =>
		{
				if (!Utility.IsPlayerValid(player) || player is null) return;

				var glovesSubMenu = Utility.CreateMenu(gloveGroup.Key);
				if (glovesSubMenu == null) return;
				glovesSubMenu.PostSelectAction = PostSelectAction.Nothing;

				glovesSubMenu.AddMenuOption(BackMenuLabel, (backPlayer, _) =>
				{
					if (Utility.IsPlayerValid(backPlayer))
					{
						glovesSelectionMenu.Open(backPlayer);
					}
				});

				foreach (var gloveEntry in gloveGroup.OrderBy(glove => glove.Finish, StringComparer.OrdinalIgnoreCase))
				{
					var selectedGlove = gloveEntry.Glove;
					glovesSubMenu.AddMenuOption(gloveEntry.Finish, (submenuPlayer, __) => ApplyGloveSelection(submenuPlayer, selectedGlove));
				}

				// Open the second-level gloves menu after the root menu finishes its close action.
				// Opening it immediately can be cancelled by the parent menu close, making the menu disappear.
				AddTimer(0.05f, () =>
				{
					if (Utility.IsPlayerValid(player))
					{
						glovesSubMenu.Open(player);
					}
				}, TimerFlags.STOP_ON_MAPCHANGE);
			});
		}

		// Command to open the gloves selection menu for players
		_config.Additional.CommandGlove.ForEach(c =>
		{
			AddCommand($"css_{c}", "Gloves selection menu", (player, info) =>
		{
				if (!Utility.IsPlayerValid(player) || !_gBCommandsAllowed) return;

				if (player == null || player.UserId == null) return;

				if (!CommandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
				    DateTime.UtcNow >= (CommandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
				{
					CommandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
					glovesSelectionMenu.Open(player);
					return;
				}
				if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
				{
					player.Print(Localizer["wp_command_cooldown"]);
				}
			});
		});
	}

	private void ApplyGloveSelection(CCSPlayerController? player, JObject selectedGlove)
	{
		if (!Utility.IsPlayerValid(player) || player is null ||
		    !int.TryParse(selectedGlove["weapon_defindex"]?.ToString(), out var weaponDefindex) ||
		    !int.TryParse(selectedGlove["paint"]?.ToString(), out var paint)) return;

		var selectedPaintName = selectedGlove["paint_name"]?.ToString() ?? string.Empty;
		var image = selectedGlove["image"]?.ToString() ?? string.Empty;
		var playerGloves = GPlayersGlove.GetOrAdd(player.Slot, new ConcurrentDictionary<CsTeam, ushort>());
		var teamsToCheck = player.TeamNum < 2
			? new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist }
			: [player.Team];

		if (Config.Additional.ShowSkinImage && !string.IsNullOrEmpty(image))
		{
			_playerWeaponImage[player.Slot] = image;
			AddTimer(2.0f, () => _playerWeaponImage.Remove(player.Slot), TimerFlags.STOP_ON_MAPCHANGE);
		}

		if (!string.IsNullOrEmpty(Localizer["wp_glove_menu_select"]))
		{
			player.Print(Localizer["wp_glove_menu_select", selectedPaintName]);
		}

		var playerInfo = new PlayerInfo
		{
			UserId = player.UserId,
			Slot = player.Slot,
			Index = (int)player.Index,
			SteamId = player.SteamID.ToString(),
			Name = player.PlayerName,
			IpAddress = player.IpAddress?.Split(":")[0]
		};

		if (paint != 0)
		{
			if (!TryGetPlayerGloveInfo(player, out _, out _))
		{
				var pawn = player.PlayerPawn.Value;
				if (pawn != null && pawn.IsValid)
				{
					CacheCurrentNativeGloveSnapshot(player, pawn);
				}
			}

			var playerWeapons = GPlayerWeaponsInfo.GetOrAdd(player.Slot, new ConcurrentDictionary<CsTeam, ConcurrentDictionary<int, WeaponInfo>>());

			foreach (var team in teamsToCheck)
		{
				playerGloves[team] = (ushort)weaponDefindex;

				var teamWeapons = playerWeapons.GetOrAdd(team, new ConcurrentDictionary<int, WeaponInfo>());
				var weaponInfo = teamWeapons.GetOrAdd(weaponDefindex, new WeaponInfo());
				weaponInfo.Paint = paint;
				weaponInfo.Wear = 0.00f;
				weaponInfo.Seed = 0;
			}
		}
		else
		{
			foreach (var team in teamsToCheck)
		{
				playerGloves.TryRemove(team, out _);
			}

			if (playerGloves.IsEmpty)
		{
				GPlayersGlove.TryRemove(player.Slot, out _);
			}
		}

		if (WeaponSync != null)
		{
			_ = Task.Run(async () =>
		{
				await WeaponSync.SyncGloveToDatabase(playerInfo, (ushort)weaponDefindex, teamsToCheck);
				await WeaponSync.SyncWeaponPaintsToDatabase(playerInfo);
			});
		}

		if (paint == 0)
		{
			GPlayersForceGloveKnifeRefresh[player.Slot] = 1;
			AddTimer(0.05f, () => RestorePlayerDefaultGloves(player), TimerFlags.STOP_ON_MAPCHANGE);
			return;
			}

		GPlayersForceGloveKnifeRefresh[player.Slot] = 1;
		AddTimer(0.05f, () => GivePlayerGloves(player), TimerFlags.STOP_ON_MAPCHANGE);
	}

	private static (string Type, string Finish) GetGloveMenuLabels(JObject glove)
	{
		var fullName = NormalizeGloveMenuText(glove["paint_name"]?.ToString());
		var gloveType = NormalizeGloveMenuText(glove["glove_type"]?.ToString());
		var finishName = NormalizeGloveMenuText(glove["finish_name"]?.ToString());

		if (gloveType.Length > 0 && finishName.Length > 0)
		{
			return (gloveType, finishName);
		}

		if (fullName.Length > 0)
		{
			var parts = fullName.Split('|', 2, StringSplitOptions.TrimEntries);
			if (parts.Length == 2 && parts[0].Length > 0 && parts[1].Length > 0)
		{
				return (parts[0], parts[1]);
			}

			return (fullName, fullName);
		}

		return (string.Empty, string.Empty);
	}

	private static string NormalizeGloveMenuText(string? value)
	{
		return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("★", string.Empty).Trim();
	}


	private void SetupAgentsMenu()
	{
		var handleAgentSelection = (CCSPlayerController? player, ChatMenuOption option) =>
		{
			if (!Utility.IsPlayerValid(player) || player is null) return;

			var selectedPaintName = option.Text;
			var selectedAgent = AgentsList.FirstOrDefault(g =>
				g.ContainsKey("agent_name") &&
				g["agent_name"] != null && g["agent_name"]!.ToString() == selectedPaintName &&
				g["team"] != null && (int)(g["team"]!) == player.TeamNum);

			if (selectedAgent == null) return;

			if (selectedAgent.ContainsKey("model"))
			{
				PlayerInfo playerInfo = new PlayerInfo
				{
					UserId = player.UserId,
					Slot = player.Slot,
					Index = (int)player.Index,
					SteamId = player.SteamID.ToString(),
					Name = player.PlayerName,
					IpAddress = player.IpAddress?.Split(":")[0]
				};

				if (Config.Additional.ShowSkinImage)
				{
					var image = selectedAgent["image"]?.ToString() ?? "";
					_playerWeaponImage[player.Slot] = image;
					AddTimer(2.0f, () => _playerWeaponImage.Remove(player.Slot), TimerFlags.STOP_ON_MAPCHANGE);
				}

				if (!string.IsNullOrEmpty(Localizer["wp_agent_menu_select"]))
				{
					player.Print(Localizer["wp_agent_menu_select", selectedPaintName]);
				}

				if (player.TeamNum == 3)
				{
					GPlayersAgent.AddOrUpdate(player.Slot,
						key => (selectedAgent["model"]!.ToString().Equals("null") ? null : selectedAgent["model"]!.ToString(), null),
						(key, oldValue) => (selectedAgent["model"]!.ToString().Equals("null") ? null : selectedAgent["model"]!.ToString(), oldValue.T));
				}
				else
				{
					GPlayersAgent.AddOrUpdate(player.Slot,
						key => (null, selectedAgent["model"]!.ToString().Equals("null") ? null : selectedAgent["model"]!.ToString()),
						(key, oldValue) => (oldValue.CT, selectedAgent["model"]!.ToString().Equals("null") ? null : selectedAgent["model"]!.ToString())
					);
				}

				if (WeaponSync != null)
				{
					_ = Task.Run(async () =>
					{
						await WeaponSync.SyncAgentToDatabase(playerInfo);
					});
				}
			}
		};

		_config.Additional.CommandAgent.ForEach(c =>
		{
			AddCommand($"css_{c}", "Agents selection menu", (player, info) =>
			{
				if (!Utility.IsPlayerValid(player) || !_gBCommandsAllowed) return;

				if (player == null || player.UserId == null) return;

				if (!CommandsCooldown.TryGetValue(player.Slot, out DateTime cooldownEndTime) ||
				    DateTime.UtcNow >= (CommandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
				{
					var agentsSelectionMenu = Utility.CreateMenu(Localizer["wp_agent_menu_title"]);
					if (agentsSelectionMenu == null) return;
					agentsSelectionMenu.PostSelectAction = PostSelectAction.Nothing;

					var filteredAgents = AgentsList.Where(agentObject =>
					{
						if (agentObject["team"]?.Value<int>() is { } teamNum)
						{
							return teamNum == player.TeamNum;
						}

						return false;
					}).ToList();

					var defaultAgent = filteredAgents.FirstOrDefault(agent =>
						string.Equals(agent["model"]?.ToString(), "null", StringComparison.OrdinalIgnoreCase));

					if (defaultAgent != null)
					{
						var defaultName = defaultAgent["agent_name"]?.ToString() ?? "";
						if (defaultName.Length > 0)
						{
							agentsSelectionMenu.AddMenuOption(defaultName, handleAgentSelection);
						}
					}

					var groupedAgents = filteredAgents
						.Where(agent => !string.Equals(agent["model"]?.ToString(), "null", StringComparison.OrdinalIgnoreCase))
						.Select(agent => new
						{
							Agent = agent,
							Name = agent["agent_name"]?.ToString() ?? string.Empty,
							Group = GetAgentMenuGroup(agent["agent_name"]?.ToString())
						})
						.Where(agent => agent.Name.Length > 0 && agent.Group.Length > 0)
						.GroupBy(agent => agent.Group, StringComparer.OrdinalIgnoreCase)
						.OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
						.ToList();

					foreach (var agentGroup in groupedAgents)
					{
						var groupName = agentGroup.Key;
						var agentsInGroup = agentGroup
							.OrderBy(agent => agent.Name, StringComparer.OrdinalIgnoreCase)
							.ToList();

						agentsSelectionMenu.AddMenuOption(groupName, (menuPlayer, _) =>
						{
							if (!Utility.IsPlayerValid(menuPlayer) || menuPlayer is null) return;

							var agentsSubMenu = Utility.CreateMenu(groupName);
							if (agentsSubMenu == null) return;
							agentsSubMenu.PostSelectAction = PostSelectAction.Nothing;

							agentsSubMenu.AddMenuOption(BackMenuLabel, (backPlayer, _) =>
							{
								if (Utility.IsPlayerValid(backPlayer))
								{
									agentsSelectionMenu.Open(backPlayer);
								}
							});

							foreach (var agentEntry in agentsInGroup)
							{
								agentsSubMenu.AddMenuOption(agentEntry.Name, handleAgentSelection);
							}

							AddTimer(0.05f, () =>
							{
								if (Utility.IsPlayerValid(menuPlayer))
								{
									agentsSubMenu.Open(menuPlayer);
								}
							}, TimerFlags.STOP_ON_MAPCHANGE);
						});
					}

					CommandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
					agentsSelectionMenu.Open(player);
					return;
				}
				if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
				{
					player.Print(Localizer["wp_command_cooldown"]);
				}
			});
		});
	}

	private static string GetAgentMenuGroup(string? agentName)
	{
		if (string.IsNullOrWhiteSpace(agentName)) return string.Empty;

		var parts = agentName.Split('|', 2, StringSplitOptions.TrimEntries);
		var groupName = parts.Length == 2 && parts[1].Length > 0
			? NormalizeMenuText(parts[1])
			: NormalizeMenuText(agentName);

		return groupName switch
		{
			"FBI" or "FBI HRT" or "FBI Sniper" or "FBI SWAT" => "FBI",
			"NZSAS" or "SAS" => "SAS",
			_ => groupName
		};
	}

	private void SetupMusicMenu()
	{
		var musicSelectionMenu = Utility.CreateMenu(Localizer["wp_music_menu_title"]);
		if (musicSelectionMenu == null) return;
		musicSelectionMenu.PostSelectAction = PostSelectAction.Close;

		var handleMusicSelection = (CCSPlayerController? player, ChatMenuOption option) =>
		{
			if (!Utility.IsPlayerValid(player) || player is null) return;

			var selectedPaintName = option.Text;
			
			var playerMusic = GPlayersMusic.GetOrAdd(player.Slot, new ConcurrentDictionary<CsTeam, ushort>());
			var teamsToCheck = player.TeamNum < 2 
				? new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist } 
				: [player.Team];  // Corrected array initializer

			var selectedMusic = MusicList.FirstOrDefault(g => g.ContainsKey("name") && g["name"]?.ToString() == selectedPaintName);
			if (selectedMusic != null)
		{
				if (!selectedMusic.ContainsKey("id") ||
				    !selectedMusic.ContainsKey("name") ||
				    !int.TryParse(selectedMusic["id"]?.ToString(), out var paint)) return;
				var image = selectedMusic["image"]?.ToString() ?? "";
				if (Config.Additional.ShowSkinImage)
				{
					_playerWeaponImage[player.Slot] = image;
					AddTimer(2.0f, () => _playerWeaponImage.Remove(player.Slot), TimerFlags.STOP_ON_MAPCHANGE);
				}

				PlayerInfo playerInfo = new PlayerInfo
				{
					UserId = player.UserId,
					Slot = player.Slot,
					Index = (int)player.Index,
					SteamId = player.SteamID.ToString(),
					Name = player.PlayerName,
					IpAddress = player.IpAddress?.Split(":")[0]
				};
				
				if (paint != 0)
				{
					foreach (var team in teamsToCheck)
					{
						playerMusic[team] = (ushort)paint;
					}
				}
				else
				{
					foreach (var team in teamsToCheck)
					{
						playerMusic[team] = 0;
					}
				}
				
				GivePlayerMusicKit(player);

				if (!string.IsNullOrEmpty(Localizer["wp_music_menu_select"]))
				{
					player.Print(Localizer["wp_music_menu_select", selectedPaintName]);
				}

				if (WeaponSync != null)
				{
					_ = Task.Run(async () =>
					{
						await WeaponSync.SyncMusicToDatabase(playerInfo, (ushort)paint, teamsToCheck);
					});
				}
			}
			else
		{
				PlayerInfo playerInfo = new PlayerInfo
				{
					UserId = player.UserId,
					Slot = player.Slot,
					Index = (int)player.Index,
					SteamId = player.SteamID.ToString(),
					Name = player.PlayerName,
					IpAddress = player.IpAddress?.Split(":")[0]
				};

				foreach (var team in teamsToCheck)
				{
					playerMusic[team] = 0;
				}
				
				GivePlayerMusicKit(player);

				if (!string.IsNullOrEmpty(Localizer["wp_music_menu_select"]))
				{
					player.Print(Localizer["wp_music_menu_select", Localizer["None"]]);
				}

				if (WeaponSync != null)
				{
					_ = Task.Run(async () =>
					{
						await WeaponSync.SyncMusicToDatabase(playerInfo, 0, teamsToCheck);
					});
				}
			}
		};

		musicSelectionMenu.AddMenuOption(Localizer["None"], handleMusicSelection);
		// Add weapon options to the weapon selection menu
		foreach (var paintName in MusicList.Select(musicObject => musicObject["name"]?.ToString() ?? "").Where(paintName => paintName.Length > 0))
		{
			musicSelectionMenu.AddMenuOption(paintName, handleMusicSelection);
		}

		// Command to open the weapon selection menu for players
		_config.Additional.CommandMusic.ForEach(c =>
		{
			AddCommand($"css_{c}", "Music selection menu", (player, info) =>
		{
				if (!Utility.IsPlayerValid(player) || !_gBCommandsAllowed) return;

				if (player == null || player.UserId == null) return;

				if (!CommandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
				    DateTime.UtcNow >= (CommandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
				{
					CommandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
					musicSelectionMenu.Open(player);
					return;
				}
				if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
				{
					player.Print(Localizer["wp_command_cooldown"]);
				}
			});
		});
	}
	

	private enum StickerAudience
	{
		Teams,
		Players
	}

	private void SetupStickersMenu()
	{
		if (!Config.Additional.SkinEnabled || !_gBCommandsAllowed) return;

		_config.Additional.CommandSticker.ForEach(c =>
		{
			AddCommand($"css_{c}", "Sticker selection menu", (player, _) =>
			{
				if (!Utility.IsPlayerValid(player) || !_gBCommandsAllowed) return;
				if (player == null || player.UserId == null) return;

				if (!CanUseStickerCommand(player))
				{
					if (!string.IsNullOrEmpty(Localizer["wp_sticker_vip_only"]))
					{
						player.Print(Localizer["wp_sticker_vip_only"]);
					}
					return;
				}

				if (!CommandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
				    DateTime.UtcNow >= (CommandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
				{
					CommandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
					OpenStickerSlotMenu(player);
					return;
				}

				if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
				{
					player.Print(Localizer["wp_command_cooldown"]);
				}
			});
		});
	}

	private bool CanUseStickerCommand(CCSPlayerController player)
	{
		if (!Config.Additional.StickersVipOnly) return true;

		var permission = Config.Additional.StickersVipPermission;
		if (string.IsNullOrWhiteSpace(permission)) return false;

		return AdminManager.PlayerHasPermissions(player, permission);
	}

	private void OpenStickerSlotMenu(CCSPlayerController player)
	{
		if (!Utility.IsPlayerValid(player)) return;

		var slotMenu = Utility.CreateMenu(Localizer["wp_sticker_menu_title"]);
		if (slotMenu == null) return;
		slotMenu.PostSelectAction = PostSelectAction.Nothing;

		for (var slotIndex = 0; slotIndex < 4; slotIndex++)
		{
			var capturedSlot = slotIndex;
			slotMenu.AddMenuOption(GetStickerSlotMenuLabel(player, capturedSlot), (menuPlayer, _) => OpenStickerSourceMenu(menuPlayer, capturedSlot));
		}

		slotMenu.AddMenuOption(Localizer["wp_sticker_remove_menu_title"], (menuPlayer, _) => OpenStickerRemoveMenu(menuPlayer));
		slotMenu.Open(player);
	}

	private void OpenStickerRemoveMenu(CCSPlayerController? player)
	{
		if (!Utility.IsPlayerValid(player) || player is null) return;

		if (!TryGetActiveWeaponStickerInfo(player, out _, out _, out var weaponInfo) || weaponInfo == null)
			return;

		var removeMenu = Utility.CreateMenu(Localizer["wp_sticker_remove_menu_title"]);
		if (removeMenu == null) return;
		removeMenu.PostSelectAction = PostSelectAction.Nothing;

		removeMenu.AddMenuOption(BackMenuLabel, (menuPlayer, _) => OpenStickerSlotMenu(menuPlayer));

		var hasAppliedSticker = false;
		for (var slotIndex = 0; slotIndex < 4; slotIndex++)
		{
			if (weaponInfo.Stickers.Count <= slotIndex || weaponInfo.Stickers[slotIndex].Id == 0) continue;

			hasAppliedSticker = true;
			var capturedSlot = slotIndex;
			removeMenu.AddMenuOption(GetStickerSlotMenuLabel(player, capturedSlot), (menuPlayer, _) => ApplyStickerSelection(menuPlayer, capturedSlot, null));
		}

		if (hasAppliedSticker)
		{
			removeMenu.AddMenuOption(Localizer["wp_sticker_remove_all"], ApplyRemoveAllStickers);
		}
		else
		{
			removeMenu.AddMenuOption(Localizer["wp_sticker_no_applied"], (_, _) => { });
		}

		AddTimer(0.05f, () =>
		{
			if (Utility.IsPlayerValid(player))
			{
				removeMenu.Open(player);
			}
		}, TimerFlags.STOP_ON_MAPCHANGE);
	}

	private void OpenStickerSourceMenu(CCSPlayerController? player, int stickerSlot)
	{
		if (!Utility.IsPlayerValid(player) || player is null) return;

		if (StickersList.Count == 0)
		{
			player.Print(Localizer["wp_sticker_no_data"]);
			return;
		}

		var sourceMenu = Utility.CreateMenu(Localizer["wp_sticker_source_menu_title", stickerSlot + 1]);
		if (sourceMenu == null) return;
		sourceMenu.PostSelectAction = PostSelectAction.Nothing;

		sourceMenu.AddMenuOption(BackMenuLabel, (menuPlayer, _) => OpenStickerSlotMenu(menuPlayer));

		var stickerSources = StickersList
			.Select(sticker => new
			{
				Sticker = sticker,
				Name = sticker["name"]?.ToString() ?? string.Empty,
				Source = GetStickerSourceMenuGroup(sticker["name"]?.ToString())
			})
			.Where(sticker => sticker.Name.Length > 0 && sticker.Source.Length > 0)
			.GroupBy(sticker => sticker.Source, StringComparer.OrdinalIgnoreCase)
			.OrderBy(group => GetStickerSourceMenuGroupOrder(group.Key))
			.ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
			.ToList();

		foreach (var sourceGroup in stickerSources)
		{
			var sourceName = sourceGroup.Key;
			sourceMenu.AddMenuOption(sourceName, (menuPlayer, _) => OpenStickerEventMenu(menuPlayer, stickerSlot, sourceName));
		}

		AddTimer(0.05f, () =>
		{
			if (Utility.IsPlayerValid(player))
			{
				sourceMenu.Open(player);
			}
		}, TimerFlags.STOP_ON_MAPCHANGE);
	}

	private void OpenStickerEventMenu(CCSPlayerController? player, int stickerSlot, string stickerSource)
	{
		if (!Utility.IsPlayerValid(player) || player is null) return;

		var eventMenu = Utility.CreateMenu(Localizer["wp_sticker_event_menu_title", stickerSource]);
		if (eventMenu == null) return;
		eventMenu.PostSelectAction = PostSelectAction.Nothing;

		eventMenu.AddMenuOption(BackMenuLabel, (menuPlayer, _) => OpenStickerSourceMenu(menuPlayer, stickerSlot));

		var stickerEvents = StickersList
			.Select(sticker => new
			{
				Sticker = sticker,
				Name = sticker["name"]?.ToString() ?? string.Empty,
				Source = GetStickerSourceMenuGroup(sticker["name"]?.ToString()),
				Event = GetStickerEventMenuGroup(sticker)
			})
			.Where(sticker => sticker.Name.Length > 0 &&
			                  string.Equals(sticker.Source, stickerSource, StringComparison.OrdinalIgnoreCase) &&
			                  sticker.Event.Length > 0)
			.GroupBy(sticker => sticker.Event, StringComparer.OrdinalIgnoreCase)
			.OrderBy(group => GetStickerEventMenuGroupOrder(group.Key))
			.ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
			.ToList();

		foreach (var eventGroup in stickerEvents)
		{
			var eventName = eventGroup.Key;
			eventMenu.AddMenuOption(eventName, (menuPlayer, _) => OpenStickerTypeMenu(menuPlayer, stickerSlot, stickerSource, eventName));
		}

		AddTimer(0.05f, () =>
		{
			if (Utility.IsPlayerValid(player))
			{
				eventMenu.Open(player);
			}
		}, TimerFlags.STOP_ON_MAPCHANGE);
	}

	private void OpenStickerTypeMenu(CCSPlayerController? player, int stickerSlot, string stickerSource, string stickerEvent)
	{
		if (!Utility.IsPlayerValid(player) || player is null) return;

		var typeMenu = Utility.CreateMenu(Localizer["wp_sticker_type_menu_title", stickerEvent]);
		if (typeMenu == null) return;
		typeMenu.PostSelectAction = PostSelectAction.Nothing;

		typeMenu.AddMenuOption(BackMenuLabel, (menuPlayer, _) => OpenStickerEventMenu(menuPlayer, stickerSlot, stickerSource));

		var stickerTypes = StickersList
			.Select(sticker => new
			{
				Sticker = sticker,
				Name = sticker["name"]?.ToString() ?? string.Empty,
				Source = GetStickerSourceMenuGroup(sticker["name"]?.ToString()),
				Event = GetStickerEventMenuGroup(sticker),
				Type = GetStickerTypeMenuGroup(sticker["name"]?.ToString())
			})
			.Where(sticker => sticker.Name.Length > 0 &&
			                  string.Equals(sticker.Source, stickerSource, StringComparison.OrdinalIgnoreCase) &&
			                  string.Equals(sticker.Event, stickerEvent, StringComparison.OrdinalIgnoreCase) &&
			                  sticker.Type.Length > 0)
			.GroupBy(sticker => sticker.Type, StringComparer.OrdinalIgnoreCase)
			.OrderBy(group => GetStickerTypeMenuGroupOrder(group.Key))
			.ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
			.ToList();

		foreach (var typeGroup in stickerTypes)
		{
			var stickerType = typeGroup.Key;
			typeMenu.AddMenuOption(stickerType, (menuPlayer, _) => OpenStickerAudienceMenu(menuPlayer, stickerSlot, stickerSource, stickerEvent, stickerType));
		}

		AddTimer(0.05f, () =>
		{
			if (Utility.IsPlayerValid(player))
			{
				typeMenu.Open(player);
			}
		}, TimerFlags.STOP_ON_MAPCHANGE);
	}

	private void OpenStickerAudienceMenu(CCSPlayerController? player, int stickerSlot, string stickerSource, string stickerEvent, string stickerType)
	{
		if (!Utility.IsPlayerValid(player) || player is null) return;

		var audienceMenu = Utility.CreateMenu(Localizer["wp_sticker_audience_menu_title", stickerType]);
		if (audienceMenu == null) return;
		audienceMenu.PostSelectAction = PostSelectAction.Nothing;

		audienceMenu.AddMenuOption(BackMenuLabel, (menuPlayer, _) => OpenStickerTypeMenu(menuPlayer, stickerSlot, stickerSource, stickerEvent));
		audienceMenu.AddMenuOption(Localizer["wp_sticker_teams"], (menuPlayer, _) => OpenStickerListMenu(menuPlayer, stickerSlot, stickerSource, stickerEvent, stickerType, StickerAudience.Teams));
		audienceMenu.AddMenuOption(Localizer["wp_sticker_players"], (menuPlayer, _) => OpenStickerListMenu(menuPlayer, stickerSlot, stickerSource, stickerEvent, stickerType, StickerAudience.Players));

		AddTimer(0.05f, () =>
		{
			if (Utility.IsPlayerValid(player))
			{
				audienceMenu.Open(player);
			}
		}, TimerFlags.STOP_ON_MAPCHANGE);
	}

	private void OpenStickerListMenu(CCSPlayerController? player, int stickerSlot, string stickerSource, string stickerEvent, string stickerType, StickerAudience stickerAudience)
	{
		if (!Utility.IsPlayerValid(player) || player is null) return;

		var stickerMenu = Utility.CreateMenu(Localizer["wp_sticker_list_menu_title", stickerSlot + 1]);
		if (stickerMenu == null) return;
		stickerMenu.PostSelectAction = PostSelectAction.Nothing;

		stickerMenu.AddMenuOption(BackMenuLabel, (menuPlayer, _) => OpenStickerAudienceMenu(menuPlayer, stickerSlot, stickerSource, stickerEvent, stickerType));

		var stickers = StickersList
			.Where(sticker => string.Equals(GetStickerSourceMenuGroup(sticker["name"]?.ToString()), stickerSource, StringComparison.OrdinalIgnoreCase) &&
			                  string.Equals(GetStickerEventMenuGroup(sticker), stickerEvent, StringComparison.OrdinalIgnoreCase) &&
			                  string.Equals(GetStickerTypeMenuGroup(sticker["name"]?.ToString()), stickerType, StringComparison.OrdinalIgnoreCase) &&
			                  GetStickerAudience(sticker) == stickerAudience)
			.OrderBy(sticker => GetStickerMenuName(sticker["name"]?.ToString() ?? string.Empty), StringComparer.OrdinalIgnoreCase)
			.ToList();

		if (stickers.Count == 0)
		{
			stickerMenu.AddMenuOption(Localizer["wp_sticker_no_data"], (_, _) => { });
		}
		else
		{
			foreach (var sticker in stickers)
			{
				var stickerId = sticker["id"]?.ToString();
				var stickerName = sticker["name"]?.ToString();
				if (string.IsNullOrEmpty(stickerId) || string.IsNullOrEmpty(stickerName)) continue;

				var selectedSticker = sticker;
				stickerMenu.AddMenuOption(GetStickerMenuName(stickerName), (menuPlayer, _) => ApplyStickerSelection(menuPlayer, stickerSlot, selectedSticker));
			}
		}

		AddTimer(0.05f, () =>
		{
			if (Utility.IsPlayerValid(player))
			{
				stickerMenu.Open(player);
			}
		}, TimerFlags.STOP_ON_MAPCHANGE);
	}

	private void ApplyStickerSelection(CCSPlayerController? player, int stickerSlot, JObject? selectedSticker)
	{
		if (!Utility.IsPlayerValid(player) || player is null || WeaponSync == null) return;

		if (!TryGetActiveWeaponStickerInfo(player, out var weapon, out var weaponDefIndex, out var weaponInfo) || weaponInfo == null)
			return;

		while (weaponInfo.Stickers.Count < 4)
		{
			weaponInfo.Stickers.Add(new StickerInfo());
		}

		var stickerName = Localizer["wp_sticker_menu_remove"].Value;
		if (selectedSticker == null)
		{
			weaponInfo.Stickers[stickerSlot] = new StickerInfo();
		}
		else
		{
			if (!uint.TryParse(selectedSticker["id"]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stickerId)) return;
			stickerName = GetStickerMenuName(selectedSticker["name"]?.ToString() ?? stickerId.ToString());

			weaponInfo.Stickers[stickerSlot] = new StickerInfo
			{
				Id = stickerId,
				Schema = 0,
				OffsetX = 0,
				OffsetY = 0,
				Wear = 0.0f,
				Scale = 0,
				Rotation = 0
			};
		}

		RefreshWeapons(player);
		SyncStickerChange(player);

		if (!string.IsNullOrEmpty(Localizer["wp_sticker_menu_select"]))
		{
			player.Print(Localizer["wp_sticker_menu_select", stickerName, stickerSlot + 1]);
		}

		AddTimer(0.05f, () =>
		{
			if (Utility.IsPlayerValid(player))
			{
				OpenStickerSlotMenu(player);
			}
		}, TimerFlags.STOP_ON_MAPCHANGE);
	}

	private void ApplyRemoveAllStickers(CCSPlayerController? player, ChatMenuOption option)
	{
		if (!Utility.IsPlayerValid(player) || player is null || WeaponSync == null) return;

		if (!TryGetActiveWeaponStickerInfo(player, out _, out _, out var weaponInfo) || weaponInfo == null)
			return;

		while (weaponInfo.Stickers.Count < 4)
		{
			weaponInfo.Stickers.Add(new StickerInfo());
		}

		for (var slotIndex = 0; slotIndex < 4; slotIndex++)
		{
			weaponInfo.Stickers[slotIndex] = new StickerInfo();
		}

		RefreshWeapons(player);
		SyncStickerChange(player);

		if (!string.IsNullOrEmpty(Localizer["wp_sticker_remove_all_done"]))
		{
			player.Print(Localizer["wp_sticker_remove_all_done"]);
		}

		AddTimer(0.05f, () =>
		{
			if (Utility.IsPlayerValid(player))
			{
				OpenStickerSlotMenu(player);
			}
		}, TimerFlags.STOP_ON_MAPCHANGE);
	}

	private bool TryGetActiveWeaponStickerInfo(CCSPlayerController player, out CBasePlayerWeapon? weapon, out int weaponDefIndex, out WeaponInfo? weaponInfo)
	{
		weapon = null;
		weaponDefIndex = 0;
		weaponInfo = null;

		weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
		if (weapon == null || !weapon.IsValid) return false;

		if (weapon.DesignerName.Contains("knife") || weapon.DesignerName.Contains("bayonet"))
		{
			player.Print(Localizer["wp_sticker_weapon_only"]);
			return false;
		}

		weaponDefIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;
		if (!HasChangedPaint(player, weaponDefIndex, out weaponInfo) || weaponInfo == null)
		{
			player.Print(Localizer["wp_sticker_need_skin"]);
			return false;
		}

		return true;
	}

	private void SyncStickerChange(CCSPlayerController player)
	{
		if (WeaponSync == null) return;

		var playerInfo = new PlayerInfo
		{
			UserId = player.UserId,
			Slot = player.Slot,
			Index = (int)player.Index,
			SteamId = player.SteamID.ToString(),
			Name = player.PlayerName,
			IpAddress = player.IpAddress?.Split(":")[0]
		};

		_ = Task.Run(async () => await WeaponSync.SyncWeaponPaintsToDatabase(playerInfo));
	}

	private string GetStickerSlotMenuLabel(CCSPlayerController player, int stickerSlot)
	{
		var displaySlot = stickerSlot + 1;
		if (!TryGetActiveWeaponStickerInfoSilent(player, out var weaponInfo) || weaponInfo == null ||
		    weaponInfo.Stickers.Count <= stickerSlot || weaponInfo.Stickers[stickerSlot].Id == 0)
		{
			return $"Slot {displaySlot}";
		}

		var stickerName = GetStickerNameById(weaponInfo.Stickers[stickerSlot].Id);
		return string.IsNullOrWhiteSpace(stickerName)
			? $"Slot {displaySlot}"
			: $"Slot {displaySlot}: {stickerName}";
	}

	private bool TryGetActiveWeaponStickerInfoSilent(CCSPlayerController player, out WeaponInfo? weaponInfo)
	{
		weaponInfo = null;

		var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
		if (weapon == null || !weapon.IsValid) return false;
		if (weapon.DesignerName.Contains("knife") || weapon.DesignerName.Contains("bayonet")) return false;

		var weaponDefIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;
		return HasChangedPaint(player, weaponDefIndex, out weaponInfo) && weaponInfo != null;
	}

	private static string GetStickerMenuName(string stickerName)
	{
		return string.IsNullOrWhiteSpace(stickerName)
			? string.Empty
			: stickerName.Replace("Sticker |", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
	}

	private static string GetStickerTypeMenuGroup(string? stickerName)
	{
		var name = stickerName ?? string.Empty;

		if (name.Contains("(Gold)", StringComparison.OrdinalIgnoreCase)) return "Gold";
		if (name.Contains("(Holo)", StringComparison.OrdinalIgnoreCase)) return "Holo";
		if (name.Contains("(Foil)", StringComparison.OrdinalIgnoreCase)) return "Foil";
		if (name.Contains("(Glitter)", StringComparison.OrdinalIgnoreCase)) return "Glitter";
		if (name.Contains("(Lenticular)", StringComparison.OrdinalIgnoreCase)) return "Lenticular";
		if (name.Contains("(Paper)", StringComparison.OrdinalIgnoreCase)) return "Paper";

		return "Normal";
	}

	private static int GetStickerTypeMenuGroupOrder(string type)
	{
		return type switch
		{
			"Normal" => 0,
			"Holo" => 1,
			"Foil" => 2,
			"Gold" => 3,
			"Glitter" => 4,
			"Lenticular" => 5,
			"Paper" => 6,
			_ => 99
		};
	}

	private static string GetStickerSourceMenuGroup(string? stickerName)
	{
		if (string.IsNullOrWhiteSpace(stickerName)) return string.Empty;

		var name = NormalizeMenuText(stickerName);
		if (IsMajorStickerEvent(name)) return "Major Stickers";
		if (IsTournamentSticker(name)) return "Tournament Stickers";
		if (name.Contains("Operation", StringComparison.OrdinalIgnoreCase)) return "Operation Stickers";
		if (name.Contains("Community", StringComparison.OrdinalIgnoreCase) ||
		    name.Contains("Workshop", StringComparison.OrdinalIgnoreCase) ||
		    name.Contains("Map", StringComparison.OrdinalIgnoreCase)) return "Community / Workshop Stickers";
		if (name.Contains("Capsule", StringComparison.OrdinalIgnoreCase) ||
		    name.Contains("Collection", StringComparison.OrdinalIgnoreCase)) return "Capsule Stickers";

		return "Other Stickers";
	}

	private static int GetStickerSourceMenuGroupOrder(string source)
	{
		return source switch
		{
			"Major Stickers" => 0,
			"Tournament Stickers" => 1,
			"Operation Stickers" => 2,
			"Community / Workshop Stickers" => 3,
			"Capsule Stickers" => 4,
			_ => 99
		};
	}

	private static string GetStickerEventMenuGroup(JObject sticker)
	{
		string[] eventKeys =
		[
			"tournament_event", "tournament", "event", "event_name", "tournament_name", "source", "capsule"
		];

		foreach (var key in eventKeys)
		{
			var value = NormalizeMenuText(sticker[key]?.ToString());
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value;
			}
		}

		var name = NormalizeMenuText(sticker["name"]?.ToString());
		if (string.IsNullOrWhiteSpace(name)) return "Other";

		var parts = name.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length >= 3)
		{
			return NormalizeMenuText(parts[^1]);
		}

		foreach (var knownEvent in KnownStickerEvents)
		{
			if (name.Contains(knownEvent, StringComparison.OrdinalIgnoreCase))
			{
				return knownEvent;
			}
		}

		return "Other";
	}

	private static int GetStickerEventMenuGroupOrder(string eventName)
	{
		if (string.Equals(eventName, "Other", StringComparison.OrdinalIgnoreCase)) return 999999;

		for (var index = 0; index < KnownStickerEvents.Length; index++)
		{
			if (string.Equals(eventName, KnownStickerEvents[index], StringComparison.OrdinalIgnoreCase))
			{
				return index;
			}
		}

		var year = ExtractStickerEventYear(eventName);
		return year > 0 ? (3000 - year) * 100 : 999998;
	}

	private static int ExtractStickerEventYear(string value)
	{
		for (var i = 0; i <= value.Length - 4; i++)
		{
			if (char.IsDigit(value[i]) && char.IsDigit(value[i + 1]) &&
			    char.IsDigit(value[i + 2]) && char.IsDigit(value[i + 3]) &&
			    int.TryParse(value.Substring(i, 4), NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
			{
				return year;
			}
		}

		return 0;
	}

	private static StickerAudience GetStickerAudience(JObject sticker)
	{
		string[] playerKeys = ["tournament_player", "player", "player_name", "pro_player", "autograph"];
		string[] teamKeys = ["tournament_team", "team", "team_name", "organization"];

		if (playerKeys.Any(key => !string.IsNullOrWhiteSpace(sticker[key]?.ToString())))
		{
			return StickerAudience.Players;
		}

		if (teamKeys.Any(key => !string.IsNullOrWhiteSpace(sticker[key]?.ToString())))
		{
			return StickerAudience.Teams;
		}

		var stickerSubject = GetStickerSubject(sticker["name"]?.ToString());
		return IsKnownTeamStickerSubject(stickerSubject) ? StickerAudience.Teams : StickerAudience.Players;
	}

	private static string GetStickerSubject(string? stickerName)
	{
		var name = GetStickerMenuName(stickerName ?? string.Empty);
		var parts = name.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		var subject = parts.Length > 0 ? parts[0] : name;

		var parenthesisIndex = subject.IndexOf('(');
		if (parenthesisIndex >= 0)
		{
			subject = subject[..parenthesisIndex];
		}

		return NormalizeMenuText(subject);
	}

	private static bool IsKnownTeamStickerSubject(string subject)
	{
		if (string.IsNullOrWhiteSpace(subject)) return false;

		return KnownStickerTeams.Any(team => string.Equals(subject, team, StringComparison.OrdinalIgnoreCase));
	}

	private static bool IsMajorStickerEvent(string name)
	{
		return KnownMajorStickerEvents.Any(eventName => name.Contains(eventName, StringComparison.OrdinalIgnoreCase));
	}

	private static bool IsTournamentSticker(string name)
	{
		string[] tournamentTerms =
		[
			"RMR", "ESL", "PGL", "StarLadder", "BLAST", "ELEAGUE", "FACEIT", "IEM",
			"ESWC", "DreamHack", "Cologne", "Katowice", "Major"
		];

		return tournamentTerms.Any(term => name.Contains(term, StringComparison.OrdinalIgnoreCase));
	}

	private static readonly string[] KnownStickerEvents =
	[
		"Budapest 2025", "Austin 2025", "Shanghai 2024", "Copenhagen 2024", "Paris 2023",
		"Rio 2022", "Antwerp 2022", "Stockholm 2021", "2020 RMR", "Berlin 2019", "Katowice 2019",
		"London 2018", "Boston 2018", "Krakow 2017", "Atlanta 2017", "Cologne 2016",
		"MLG Columbus 2016", "Cluj-Napoca 2015", "Cologne 2015", "Katowice 2015",
		"DreamHack 2014", "Cologne 2014", "Katowice 2014"
	];

	private static readonly string[] KnownMajorStickerEvents =
	[
		"Budapest 2025", "Austin 2025", "Shanghai 2024", "Copenhagen 2024", "Paris 2023",
		"Rio 2022", "Antwerp 2022", "Stockholm 2021", "Berlin 2019", "Katowice 2019",
		"London 2018", "Boston 2018", "Krakow 2017", "Atlanta 2017", "Cologne 2016",
		"MLG Columbus 2016", "Cluj-Napoca 2015", "Cologne 2015", "Katowice 2015",
		"DreamHack 2014", "Cologne 2014", "Katowice 2014"
	];

	private static readonly string[] KnownStickerTeams =
	[
		"3DMAX", "9INE", "9z Team", "100 Thieves", "ALTERNATE aTTaX", "AMKAL ESPORTS", "Astralis",
		"AVANGAR", "Bad News Eagles", "BIG", "Cloud9", "Complexity Gaming", "Counter Logic Gaming",
		"Copenhagen Flames", "CR4ZY", "dAT team", "ENCE", "Eternal Fire", "FaZe Clan", "FlipSid3 Tactics",
		"Fluxo", "forZe eSports", "Fnatic", "FURIA", "G2 Esports", "Gambit Esports", "GamerLegion",
		"GODSENT", "Grayhound Gaming", "HellRaisers", "Heroic", "Imperial Esports", "Into The Breach",
		"Keyd Stars", "Legacy", "Liquid", "Luminosity Gaming", "MIBR", "MOUZ", "mousesports", "Natus Vincere",
		"Ninjas in Pyjamas", "North", "OpTic Gaming", "paiN Gaming", "PENTA Sports", "Quantum Bellator Fire",
		"Renegades", "SAW", "SK Gaming", "Space Soldiers", "Spirit", "Team Dignitas", "Team EnVyUs",
		"Team Kinguin", "Team LDLC.com", "Team Liquid", "Team SoloMid", "Team Spirit", "Titan", "TYLOO",
		"Vega Squadron", "Virtus.Pro", "Virtus.pro", "Vitality", "Vox Eminor", "Winstrike Team", "Wolf", "The MongolZ",
		"MongolZ", "Rare Atom", "FlyQuest", "M80", "Wildcard", "Passion UA", "Aurora", "Nemiga", "BetBoom Team",
		"Falcons", "PARIVISION", "B8", "Lynn Vision", "NRG", "BESTIA", "RED Canids", "OG", "Endpoint",
		"Entropiq", "Evil Geniuses", "00 Nation", "IHC Esports", "Outsiders", "Sprout", "Movistar Riders",
		"MASONIC", "ECSTATIC", "Team One", "Team oNe", "Vici Gaming", "ViCi Gaming", "QBF", "LGB eSports"
	];

	private string GetStickerNameById(uint stickerId)
	{
		var sticker = StickersList.FirstOrDefault(item =>
			uint.TryParse(item["id"]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id == stickerId);

		return sticker == null ? string.Empty : GetStickerMenuName(sticker["name"]?.ToString() ?? stickerId.ToString());
	}

	private void SetupPinsMenu()
	{
		const string defaultPinLabel = "Default";

		var pinsSelectionMenu = Utility.CreateMenu(Localizer["wp_pins_menu_title"]);
		if (pinsSelectionMenu == null) return;
		pinsSelectionMenu.PostSelectAction = PostSelectAction.Nothing;

		var handlePinsSelection = (CCSPlayerController? player, ChatMenuOption option) =>
		{
			if (!Utility.IsPlayerValid(player) || player is null) return;

			var selectedPaintName = option.Text;

			var playerPins = GPlayersPin.GetOrAdd(player.Slot, new ConcurrentDictionary<CsTeam, ushort>());
			var teamsToCheck = player.TeamNum < 2
				? new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist }
				: [player.Team];

			var isDefaultPin = string.Equals(selectedPaintName, defaultPinLabel, StringComparison.OrdinalIgnoreCase) ||
			                   string.Equals(selectedPaintName, Localizer["None"].Value, StringComparison.OrdinalIgnoreCase);

			if (isDefaultPin)
			{
				PlayerInfo playerInfo = new PlayerInfo
				{
					UserId = player.UserId,
					Slot = player.Slot,
					Index = (int)player.Index,
					SteamId = player.SteamID.ToString(),
					Name = player.PlayerName,
					IpAddress = player.IpAddress?.Split(":")[0]
				};

				foreach (var team in teamsToCheck)
				{
					playerPins.TryRemove(team, out _);
				}

				if (playerPins.IsEmpty)
				{
					GPlayersPin.TryRemove(player.Slot, out _);
				}

				if (!string.IsNullOrEmpty(Localizer["wp_pins_menu_select"]))
				{
					player.Print(Localizer["wp_pins_menu_select", defaultPinLabel]);
				}

				RestorePlayerDefaultPin(player);

				if (WeaponSync != null)
				{
					_ = Task.Run(async () =>
					{
						await WeaponSync.SyncPinToDatabase(playerInfo, 0, teamsToCheck);
					});
				}

				return;
			}

			var selectedPin = PinsList.FirstOrDefault(g => g.ContainsKey("name") && g["name"]?.ToString() == selectedPaintName);
			if (selectedPin == null ||
			    !selectedPin.ContainsKey("id") ||
			    !selectedPin.ContainsKey("name") ||
			    !int.TryParse(selectedPin["id"]?.ToString(), out var paint)) return;

			var image = selectedPin["image"]?.ToString() ?? "";
			if (Config.Additional.ShowSkinImage)
			{
				_playerWeaponImage[player.Slot] = image;
				AddTimer(2.0f, () => _playerWeaponImage.Remove(player.Slot), TimerFlags.STOP_ON_MAPCHANGE);
			}

			PlayerInfo customPinPlayerInfo = new PlayerInfo
			{
				UserId = player.UserId,
				Slot = player.Slot,
				Index = (int)player.Index,
				SteamId = player.SteamID.ToString(),
				Name = player.PlayerName,
				IpAddress = player.IpAddress?.Split(":")[0]
			};

			CacheCurrentNativePin(player);

			foreach (var team in teamsToCheck)
			{
				playerPins[team] = (ushort)paint;
			}

			if (!string.IsNullOrEmpty(Localizer["wp_pins_menu_select"]))
			{
				player.Print(Localizer["wp_pins_menu_select", selectedPaintName]);
			}

			GivePlayerPin(player);

			if (WeaponSync != null)
			{
				_ = Task.Run(async () =>
				{
					await WeaponSync.SyncPinToDatabase(customPinPlayerInfo, (ushort)paint, teamsToCheck);
				});
			}
		};

		pinsSelectionMenu.AddMenuOption(defaultPinLabel, handlePinsSelection);

		var groupedPins = PinsList
			.Select(pin => new
			{
				Pin = pin,
				Name = pin["name"]?.ToString() ?? string.Empty,
				Source = GetPinSourceMenuGroup(pin["name"]?.ToString())
			})
			.Where(pin => pin.Name.Length > 0 && pin.Source.Length > 0)
			.GroupBy(pin => pin.Source, StringComparer.OrdinalIgnoreCase)
			.OrderBy(group => GetPinSourceMenuGroupOrder(group.Key))
			.ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
			.ToList();

		foreach (var pinGroup in groupedPins)
		{
			var groupName = pinGroup.Key;
			var pinsInGroup = pinGroup
				.OrderBy(pin => pin.Name, StringComparer.OrdinalIgnoreCase)
				.ToList();

			pinsSelectionMenu.AddMenuOption(groupName, (player, _) =>
			{
				if (!Utility.IsPlayerValid(player) || player is null) return;

				var pinsSubMenu = Utility.CreateMenu(groupName);
				if (pinsSubMenu == null) return;
				pinsSubMenu.PostSelectAction = PostSelectAction.Nothing;

				pinsSubMenu.AddMenuOption(BackMenuLabel, (backPlayer, _) =>
				{
					if (Utility.IsPlayerValid(backPlayer))
					{
						pinsSelectionMenu.Open(backPlayer);
					}
				});

				foreach (var pinEntry in pinsInGroup)
				{
					pinsSubMenu.AddMenuOption(pinEntry.Name, handlePinsSelection);
				}

				AddTimer(0.05f, () =>
				{
					if (Utility.IsPlayerValid(player))
					{
						pinsSubMenu.Open(player);
					}
				}, TimerFlags.STOP_ON_MAPCHANGE);
			});
		}

		_config.Additional.CommandPin.ForEach(c =>
		{
			AddCommand($"css_{c}", "Pin selection menu", (player, info) =>
			{
				if (!Utility.IsPlayerValid(player) || !_gBCommandsAllowed) return;

				if (player == null || player.UserId == null) return;

				if (!CommandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
				    DateTime.UtcNow >= (CommandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
				{
					CommandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
					pinsSelectionMenu.Open(player);
					return;
				}

				if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
				{
					player.Print(Localizer["wp_command_cooldown"]);
				}
			});
		});
	}

	private static string GetPinSourceMenuGroup(string? pinName)
	{
		if (string.IsNullOrWhiteSpace(pinName)) return string.Empty;

		var name = NormalizeMenuText(pinName);

		if (name.Contains("Pick'Em Trophy", StringComparison.OrdinalIgnoreCase) ||
		    name.Contains("Fantasy Trophy", StringComparison.OrdinalIgnoreCase))
		{
			return "Major Pick'Em / Fantasy";
		}

		if (name.StartsWith("Champion at ", StringComparison.OrdinalIgnoreCase) ||
		    name.StartsWith("Finalist at ", StringComparison.OrdinalIgnoreCase) ||
		    name.StartsWith("Semifinalist at ", StringComparison.OrdinalIgnoreCase) ||
		    name.StartsWith("Quarterfinalist at ", StringComparison.OrdinalIgnoreCase))
		{
			return "Major Trophies";
		}

		if (name.Contains("Viewer Pass", StringComparison.OrdinalIgnoreCase) ||
		    name.Contains("Souvenir Token", StringComparison.OrdinalIgnoreCase) ||
		    name.Contains("Souvenir Package", StringComparison.OrdinalIgnoreCase) ||
		    IsMajorEventCoin(name))
		{
			return "Major Viewer Passes / Coins";
		}

		if (name.Contains("Operation ", StringComparison.OrdinalIgnoreCase))
		{
			return "Operations";
		}

		if (name.Contains("Service Medal", StringComparison.OrdinalIgnoreCase))
		{
			return "Service Medals";
		}

		if (name.Contains("Premier Season", StringComparison.OrdinalIgnoreCase))
		{
			return "Premier Medals";
		}

		if (name.EndsWith("Map Coin", StringComparison.OrdinalIgnoreCase))
		{
			return "Workshop Map Coins";
		}

		if (name.EndsWith(" Pin", StringComparison.OrdinalIgnoreCase))
		{
			return "Pins";
		}

		if (name.EndsWith(" Coin", StringComparison.OrdinalIgnoreCase))
		{
			return "Coins";
		}

		return "Other";
	}

	private static bool IsMajorEventCoin(string name)
	{
		if (!name.Contains(" Coin", StringComparison.OrdinalIgnoreCase)) return false;

		string[] majorEventNames =
		[
			"Katowice", "Berlin", "Stockholm", "Antwerp", "Rio", "Paris", "Copenhagen",
			"Shanghai", "Austin", "Budapest", "Cologne", "Krakow", "Boston", "London",
			"Columbus", "Cluj-Napoca", "DreamHack", "ELEAGUE", "FACEIT", "BLAST.tv",
			"PGL", "StarLadder", "Perfect World", "EMS One", "ESL One", "MLG"
		];

		return majorEventNames.Any(eventName => name.Contains(eventName, StringComparison.OrdinalIgnoreCase));
	}

	private static int GetPinSourceMenuGroupOrder(string group)
	{
		return group switch
		{
			"Major Trophies" => 0,
			"Major Pick'Em / Fantasy" => 1,
			"Major Viewer Passes / Coins" => 2,
			"Operations" => 3,
			"Workshop Map Coins" => 4,
			"Pins" => 5,
			"Service Medals" => 6,
			"Premier Medals" => 7,
			"Coins" => 8,
			_ => 99
		};
	}


	private static bool TryGetPlayerGloveInfo(CCSPlayerController player, out ushort gloveId, out WeaponInfo? weaponInfo)
	{
		gloveId = 0;
		weaponInfo = null;

		if (!GPlayersGlove.TryGetValue(player.Slot, out var gloveInfo) ||
		    !gloveInfo.TryGetValue(player.Team, out gloveId) ||
		    gloveId == 0)
		{
			return false;
		}

		return HasChangedPaint(player, gloveId, out weaponInfo) && weaponInfo != null;
	}

	private void CacheCurrentNativeGloveSnapshot(CCSPlayerController player, CCSPlayerPawn pawn)
	{
		if (player.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist)) return;

		var snapshotsByTeam = GPlayersNativeGlove.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, NativeGloveSnapshot>());
		if (snapshotsByTeam.ContainsKey(player.Team)) return;

		var item = pawn.EconGloves;
		snapshotsByTeam[player.Team] = new NativeGloveSnapshot
		{
			ItemDefinitionIndex = item.ItemDefinitionIndex,
			EntityQuality = item.EntityQuality,
			EntityLevel = item.EntityLevel,
			ItemID = item.ItemID,
			ItemIDHigh = item.ItemIDHigh,
			ItemIDLow = item.ItemIDLow,
			AccountID = item.AccountID,
			InventoryPosition = item.InventoryPosition,
			Initialized = item.Initialized,
			CustomName = item.CustomName,
			CustomNameOverride = item.CustomNameOverride
		};
	}

	private void RestorePlayerDefaultGloves(CCSPlayerController player)
	{
		if (!Utility.IsPlayerValid(player) || player.PlayerPawn.Value == null) return;

		var pawn = player.PlayerPawn.Value;
		var item = pawn.EconGloves;
		item.NetworkedDynamicAttributes.Attributes.RemoveAll();
		item.AttributeList.Attributes.RemoveAll();

		if (GPlayersNativeGlove.TryGetValue(player.Slot, out var snapshotsByTeam) &&
		    snapshotsByTeam.TryGetValue(player.Team, out var snapshot))
		{
			item.ItemDefinitionIndex = snapshot.ItemDefinitionIndex;
			item.EntityQuality = snapshot.EntityQuality;
			item.EntityLevel = snapshot.EntityLevel;
			item.ItemID = snapshot.ItemID;
			item.ItemIDHigh = snapshot.ItemIDHigh;
			item.ItemIDLow = snapshot.ItemIDLow;
			item.AccountID = snapshot.AccountID;
			item.InventoryPosition = snapshot.InventoryPosition;
			item.CustomName = snapshot.CustomName;
			item.CustomNameOverride = snapshot.CustomNameOverride;
			item.Initialized = snapshot.Initialized || snapshot.ItemDefinitionIndex != 0 || snapshot.ItemID != 0;
		}
		else
		{
			item.ItemDefinitionIndex = 0;
			item.ItemID = 0;
			item.ItemIDHigh = 0;
			item.ItemIDLow = 0;
			item.Initialized = false;
		}

		Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInventoryServices");
		TouchGloveState(pawn);
		if (GPlayersForceGloveKnifeRefresh.TryRemove(player.Slot, out _))
		{
			ForceKnifeSlotRefresh(player);
		}
	}

	private static void CacheCurrentNativePin(CCSPlayerController player)
	{
		if (player.InventoryServices == null) return;
		var nativePinsByTeam = GPlayersNativePin.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, MedalRank_t>());
		nativePinsByTeam.GetOrAdd(player.Team, _ => player.InventoryServices.Rank[5]);
	}

	private static void RestorePlayerDefaultPin(CCSPlayerController player)
	{
		if (player.InventoryServices == null) return;
		if (!GPlayersNativePin.TryGetValue(player.Slot, out var nativePinsByTeam) ||
		    !nativePinsByTeam.TryGetValue(player.Team, out var nativePin))
		{
			return;
		}

		player.InventoryServices.Rank[5] = nativePin;
		Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInventoryServices");
	}

	private void TouchGloveState(CCSPlayerPawn pawn)
	{
		unchecked { pawn.EconGlovesChanged++; }
		Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_nEconGlovesChanged");
		SetBodygroup(pawn, "first_or_third_person", 0);
		AddTimer(0.2f, () => { if (pawn.IsValid) SetBodygroup(pawn, "first_or_third_person", 1); }, TimerFlags.STOP_ON_MAPCHANGE);
	}

	private void ForceKnifeSlotRefresh(CCSPlayerController player)
	{
		AddTimer(0.03f, () =>
		{
			if (!Utility.IsPlayerValid(player) || !player.PawnIsAlive) return;
			if (GetActiveWeaponSlotCommand(player) == "slot3")
			{
				RefreshKnifeEntityForGloves(player);
				return;
			}
			player.ExecuteClientCommand("slot3");
		}, TimerFlags.STOP_ON_MAPCHANGE);
	}

	private void RefreshKnifeEntityForGloves(CCSPlayerController player)
	{
		var weapons = player.PlayerPawn.Value?.WeaponServices?.MyWeapons;
		if (weapons == null) return;

		foreach (var weaponHandle in weapons)
		{
			var weapon = weaponHandle.Value;
			if (weapon == null || !weapon.IsValid) continue;
			if (IsKnifeWeapon(weapon)) weapon.AddEntityIOEvent("Kill", weapon, null, "", 0.01f);
		}

		AddTimer(0.06f, () =>
		{
			if (!Utility.IsPlayerValid(player) || !player.PawnIsAlive) return;
			var newKnife = new CBasePlayerWeapon(player.GiveNamedItem(GetDefaultKnifeForTeam(player)));
			Server.NextFrame(() =>
			{
				if (!Utility.IsPlayerValid(player) || !player.PawnIsAlive) return;
				if (newKnife != null && newKnife.IsValid) GivePlayerWeaponSkin(player, newKnife);
				player.ExecuteClientCommand("slot3");
			});
		}, TimerFlags.STOP_ON_MAPCHANGE);
	}

	private static string? GetActiveWeaponSlotCommand(CCSPlayerController player)
	{
		try
		{
			var activeWeapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
			if (activeWeapon == null || !activeWeapon.IsValid) return null;
			if (IsKnifeWeapon(activeWeapon)) return "slot3";
			var weaponData = activeWeapon.As<CCSWeaponBase>().VData;
			return weaponData?.GearSlot switch
			{
				gear_slot_t.GEAR_SLOT_RIFLE => "slot1",
				gear_slot_t.GEAR_SLOT_PISTOL => "slot2",
				gear_slot_t.GEAR_SLOT_KNIFE => "slot3",
				_ => null
			};
		}
		catch { return null; }
	}

	private static string GetDefaultKnifeForTeam(CCSPlayerController player)
	{
		return player.Team == CsTeam.Terrorist ? "weapon_knife_t" : "weapon_knife";
	}

	private static string NormalizeMenuText(string? value)
	{
		return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("★", string.Empty).Trim();
	}

}
