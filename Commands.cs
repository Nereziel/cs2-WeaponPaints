using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;

namespace WeaponPaints
{
	public partial class WeaponPaints
	{
		private void OnCommandRefresh(CCSPlayerController? player, CommandInfo command)
		{
			if (!Config.Additional.CommandWpEnabled || !Config.Additional.SkinEnabled || !g_bCommandsAllowed) return;
			if (!Utility.IsPlayerValid(player)) return;

			if (player == null || !player.IsValid || player.UserId == null || player.IsBot) return;

			int playerIndex = (int)player!.Index;

			PlayerInfo playerInfo = new PlayerInfo
			{
				UserId = player.UserId,
				Index = (int)player.Index,
				SteamId = player?.SteamID.ToString(),
				Name = player?.PlayerName,
				IpAddress = player?.IpAddress?.Split(":")[0]
			};

			if (player == null || !player.IsValid || player.UserId == null || player.IsBot) return;

			try
			{
				if (!commandsCooldown.TryGetValue((int)player.UserId, out DateTime cooldownEndTime) ||
	DateTime.UtcNow >= (commandsCooldown.TryGetValue((int)player.UserId, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
				{
					commandsCooldown[(int)player.UserId] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
					if (weaponSync != null)
						Task.Run(async () => await weaponSync.GetWeaponPaintsFromDatabase(playerInfo));
					if (Config.Additional.KnifeEnabled)
					{
						if (weaponSync != null)
							Task.Run(async () => await weaponSync.GetKnifeFromDatabase(playerInfo));

						RefreshWeapons(player);
					}
					if (!string.IsNullOrEmpty(Localizer["wp_command_refresh_done"]))
					{
						player!.Print(Localizer["wp_command_refresh_done"]);
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
			if (!Config.Additional.KnifeEnabled) return;
			if (!string.IsNullOrEmpty(Localizer["wp_info_knife"]))
			{
				player!.Print(Localizer["wp_info_knife"]);
			}
		}

		private void RegisterCommands()
		{
			AddCommand($"css_{Config.Additional.CommandSkin}", "Skins info", (player, info) =>
			{
				if (!Utility.IsPlayerValid(player)) return;
				OnCommandWS(player, info);
			});
			AddCommand($"css_{Config.Additional.CommandRefresh}", "Skins refresh", (player, info) =>
			{
				if (!Utility.IsPlayerValid(player) || !g_bCommandsAllowed) return;
				OnCommandRefresh(player, info);
			});
			if (Config.Additional.CommandKillEnabled)
			{
				AddCommand($"css_{Config.Additional.CommandKill}", "kill yourself", (player, info) =>
				{
					if (player == null || !Utility.IsPlayerValid(player) || player.PlayerPawn.Value == null || !player!.PlayerPawn.IsValid) return;

					player.PlayerPawn.Value.CommitSuicide(true, false);
				});
			}
		}

		private void SetupKnifeMenu()
		{
			if (!Config.Additional.KnifeEnabled || !g_bCommandsAllowed) return;

			var knivesOnly = weaponList
				.Where(pair => pair.Key.StartsWith("weapon_knife") || pair.Key.StartsWith("weapon_bayonet"))
				.ToDictionary(pair => pair.Key, pair => pair.Value);

			var giveItemMenu = new ChatMenu(Localizer["wp_knife_menu_title"]);
			giveItemMenu.PostSelectAction = PostSelectAction.Close;
			var handleGive = (CCSPlayerController player, ChatMenuOption option) =>
			{
				if (!Utility.IsPlayerValid(player)) return;

				var knifeName = option.Text;
				var knifeKey = knivesOnly.FirstOrDefault(x => x.Value == knifeName).Key;
				if (!string.IsNullOrEmpty(knifeKey))
				{
					if (!string.IsNullOrEmpty(Localizer["wp_knife_menu_select"]))
					{
						player!.Print(Localizer["wp_knife_menu_select", knifeName]);
					}

					if (!string.IsNullOrEmpty(Localizer["wp_knife_menu_kill"]) && Config.Additional.CommandKillEnabled)
					{
						player!.Print(Localizer["wp_knife_menu_kill"]);
					}

					PlayerInfo playerInfo = new PlayerInfo
					{
						UserId = player.UserId,
						Index = (int)player.Index,
						SteamId = player.SteamID.ToString(),
						Name = player.PlayerName,
						IpAddress = player.IpAddress?.Split(":")[0]
					};

					g_playersKnife[(int)player!.Index] = knifeKey;

					if (g_bCommandsAllowed && (LifeState_t)player.LifeState == LifeState_t.LIFE_ALIVE)
						RefreshKnife(player);

					_ = weaponSync?.SyncKnifeToDatabase(playerInfo, knifeKey) ?? Task.CompletedTask;
				}
			};
			foreach (var knifePair in knivesOnly)
			{
				giveItemMenu.AddMenuOption(knifePair.Value, handleGive);
			}
			AddCommand($"css_{Config.Additional.CommandKnife}", "Knife Menu", (player, info) =>
			{
				if (!Utility.IsPlayerValid(player) || !g_bCommandsAllowed) return;

				if (player == null || player.UserId == null) return;

				if (!commandsCooldown.TryGetValue((int)player.UserId, out DateTime cooldownEndTime) ||
					DateTime.UtcNow >= (commandsCooldown.TryGetValue((int)player.UserId, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
				{
					commandsCooldown[(int)player.UserId] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
					MenuManager.OpenChatMenu(player, giveItemMenu);
					return;
				}
				if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
				{
					player!.Print(Localizer["wp_command_cooldown"]);
				}
			});
		}

		private void SetupSkinsMenu()
		{
			var classNamesByWeapon = weaponList.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
			var weaponSelectionMenu = new ChatMenu(Localizer["wp_skin_menu_weapon_title"]);

			// Function to handle skin selection for a specific weapon
			var handleWeaponSelection = (CCSPlayerController? player, ChatMenuOption option) =>
			{
				if (!Utility.IsPlayerValid(player)) return;

				int playerIndex = (int)player!.Index;
				string selectedWeapon = option.Text;
				if (classNamesByWeapon.TryGetValue(selectedWeapon, out string? selectedWeaponClassname))
				{
					if (selectedWeaponClassname == null) return;
					var skinsForSelectedWeapon = skinsList?.Where(skin =>
					skin != null &&
					skin.TryGetValue("weapon_name", out var weaponName) &&
					weaponName?.ToString() == selectedWeaponClassname
				)?.ToList();

					var skinSubMenu = new ChatMenu(Localizer["wp_skin_menu_skin_title", selectedWeapon]);
					skinSubMenu.PostSelectAction = PostSelectAction.Close;

					// Function to handle skin selection for the chosen weapon
					var handleSkinSelection = (CCSPlayerController p, ChatMenuOption opt) =>
					{
						if (!Utility.IsPlayerValid(p)) return;

						playerIndex = (int)p.Index;

						string steamId = p.SteamID.ToString();
						var firstSkin = skinsList?.FirstOrDefault(skin =>
						{
							if (skin != null && skin.TryGetValue("weapon_name", out var weaponName))
							{
								return weaponName?.ToString() == selectedWeaponClassname;
							}
							return false;
						});

						string selectedSkin = opt.Text;
						string selectedPaintID = selectedSkin.Split('(')[1].Trim(')').Trim();

						if (firstSkin != null &&
							firstSkin.TryGetValue("weapon_defindex", out var weaponDefIndexObj) &&
							weaponDefIndexObj != null &&
							int.TryParse(weaponDefIndexObj.ToString(), out var weaponDefIndex) &&
							int.TryParse(selectedPaintID, out var paintID))
						{
							if (Config.Additional.ShowSkinImage && skinsList != null)
							{
								var foundSkin = skinsList.FirstOrDefault(skin =>
									((int?)skin?["weapon_defindex"] ?? 0) == weaponDefIndex &&
									((int?)skin?["paint"] ?? 0) == paintID &&
									skin?["image"] != null
								);
								string image = foundSkin?["image"]?.ToString() ?? "";
								PlayerWeaponImage[p.Slot] = image;
								AddTimer(2.0f, () => PlayerWeaponImage.Remove(p.Slot));
							}

							p.Print(Localizer["wp_skin_menu_select", selectedSkin]);

							if (!gPlayerWeaponsInfo[playerIndex].ContainsKey(weaponDefIndex))
							{
								gPlayerWeaponsInfo[playerIndex][weaponDefIndex] = new WeaponInfo();
							}

							gPlayerWeaponsInfo[playerIndex][weaponDefIndex].Paint = paintID;
							gPlayerWeaponsInfo[playerIndex][weaponDefIndex].Wear = 0.01f;
							gPlayerWeaponsInfo[playerIndex][weaponDefIndex].Seed = 0;

							PlayerInfo playerInfo = new PlayerInfo
							{
								UserId = p.UserId,
								Index = (int)p.Index,
								SteamId = p.SteamID.ToString(),
								Name = p.PlayerName,
								IpAddress = p.IpAddress?.Split(":")[0]
							};

							if (g_bCommandsAllowed && (LifeState_t)p.LifeState == LifeState_t.LIFE_ALIVE)
								RefreshWeapons(p);

							if (!Config.GlobalShare)
							{
								if (weaponSync != null)
									Task.Run(async () => await weaponSync.SyncWeaponPaintsToDatabase(playerInfo));
							}
						}
					};

					// Add skin options to the submenu for the selected weapon
					if (skinsForSelectedWeapon != null)
					{
						foreach (var skin in skinsForSelectedWeapon.Where(s => s != null))
						{
							if (skin.TryGetValue("paint_name", out var paintNameObj) && skin.TryGetValue("paint", out var paintObj))
							{
								var paintName = paintNameObj?.ToString();
								var paint = paintObj?.ToString();

								if (!string.IsNullOrEmpty(paintName) && !string.IsNullOrEmpty(paint))
								{
									skinSubMenu.AddMenuOption($"{paintName} ({paint})", handleSkinSelection);
								}
							}
						}
					}

					// Open the submenu for skin selection of the chosen weapon
					MenuManager.OpenChatMenu(player, skinSubMenu);
				}
			};

			// Add weapon options to the weapon selection menu
			foreach (var weaponClass in weaponList.Keys)
			{
				string weaponName = weaponList[weaponClass];
				weaponSelectionMenu.AddMenuOption(weaponName, handleWeaponSelection);
			}
			// Command to open the weapon selection menu for players
			AddCommand($"css_{Config.Additional.CommandSkinSelection}", "Skins selection menu", (player, info) =>
					{
						if (!Utility.IsPlayerValid(player)) return;

						if (player == null || player.UserId == null) return;

						if (!commandsCooldown.TryGetValue((int)player.UserId, out DateTime cooldownEndTime) ||
					DateTime.UtcNow >= (commandsCooldown.TryGetValue((int)player.UserId, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
						{
							commandsCooldown[(int)player.UserId] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
							MenuManager.OpenChatMenu(player, weaponSelectionMenu);
							return;
						}
						if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
						{
							player!.Print(Localizer["wp_command_cooldown"]);
						}
					});
		}
	}
}