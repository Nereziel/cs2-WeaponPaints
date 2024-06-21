using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using Newtonsoft.Json.Linq;

namespace WeaponPaints
{
	public partial class WeaponPaints
	{
        	private void OnCommandRefresh(CCSPlayerController? player, CommandInfo command)
        	{
			if (!Config.Additional.CommandWpEnabled || !Config.Additional.SkinEnabled || !g_bCommandsAllowed) return;
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
                		if (player != null && !commandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
    	player != null && DateTime.UtcNow >= (commandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
                		{
                    			commandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);

                    			if (weaponSync != null)
                    			{
                        			_ = Task.Run(async () => await weaponSync.GetPlayerData(playerInfo));

                        			GivePlayerGloves(player);
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

        private void OnCommandRefreshBySteamId(CommandInfo command)
        {
		var player = Utilities.GetPlayerFromSteamId(Convert.ToUInt64(command.ArgByIndex(1)));

            	if (!Config.Additional.CommandWpEnabled || !Config.Additional.SkinEnabled || !g_bCommandsAllowed) return;
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
                	if (player != null && !commandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
    player != null && DateTime.UtcNow >= (commandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
                	{
                    		commandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);

				if (weaponSync != null)
				{
					_ = Task.Run(async () => await weaponSync.GetPlayerData(playerInfo));

					GivePlayerGloves(player);
					RefreshWeapons(player);
				}
                   		return;
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
                if (!Utility.IsPlayerValid(player)) return;
                OnCommandRefresh(player, info);
            });
            AddCommand($"css_steamid_{Config.Additional.CommandRefresh}", "Skins refresh by SteamID", (player, info) =>
            {
                if (Utility.IsPlayerValid(player)) return;
                OnCommandRefreshBySteamId(info);
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

			var knivesOnly = WeaponList
				.Where(pair => pair.Key.StartsWith("weapon_knife") || pair.Key.StartsWith("weapon_bayonet"))
				.ToDictionary(pair => pair.Key, pair => pair.Value);

			var giveItemMenu = new ChatMenu(Localizer["wp_knife_menu_title"]);
			var handleGive = (CCSPlayerController player, ChatMenuOption option) =>
			{
				if (!Utility.IsPlayerValid(player)) return;

				var knifeName = option.Text;
				var knifeKey = knivesOnly.FirstOrDefault(x => x.Value == knifeName).Key;
				if (string.IsNullOrEmpty(knifeKey)) return;
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
					Slot = player.Slot,
					Index = (int)player.Index,
					SteamId = player.SteamID.ToString(),
					Name = player.PlayerName,
					IpAddress = player.IpAddress?.Split(":")[0]
				};

				g_playersKnife[player.Slot] = knifeKey;

				if (g_bCommandsAllowed && (LifeState_t)player.LifeState == LifeState_t.LIFE_ALIVE)
					RefreshWeapons(player);

				if (weaponSync != null)
					_ = Task.Run(async () => await weaponSync.SyncKnifeToDatabase(playerInfo, knifeKey));
			};
			foreach (var knifePair in knivesOnly)
			{
				giveItemMenu.AddMenuOption(knifePair.Value, handleGive);
			}
			AddCommand($"css_{Config.Additional.CommandKnife}", "Knife Menu", (player, info) =>
			{
				if (!Utility.IsPlayerValid(player) || !g_bCommandsAllowed) return;

				if (player == null || player.UserId == null) return;

				if (!commandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
	DateTime.UtcNow >= (commandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
				{
					commandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
					giveItemMenu.PostSelectAction = PostSelectAction.Close;
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
			var classNamesByWeapon = WeaponList.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
			var weaponSelectionMenu = new ChatMenu(Localizer["wp_skin_menu_weapon_title"]);

			// Function to handle skin selection for a specific weapon
			var handleWeaponSelection = (CCSPlayerController? player, ChatMenuOption option) =>
			{
				if (!Utility.IsPlayerValid(player)) return;

				var selectedWeapon = option.Text;
				if (!classNamesByWeapon.TryGetValue(selectedWeapon, out var selectedWeaponClassname)) return;
				var skinsForSelectedWeapon = skinsList?.Where(skin =>
					skin.TryGetValue("weapon_name", out var weaponName) &&
					weaponName?.ToString() == selectedWeaponClassname
				)?.ToList();

				var skinSubMenu = new ChatMenu(Localizer["wp_skin_menu_skin_title", selectedWeapon]);

				// Function to handle skin selection for the chosen weapon
				var handleSkinSelection = (CCSPlayerController p, ChatMenuOption opt) =>
				{
					if (!Utility.IsPlayerValid(p)) return;

					var steamId = p.SteamID.ToString();
					var firstSkin = skinsList?.FirstOrDefault(skin =>
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
					{
						if (Config.Additional.ShowSkinImage && skinsList != null)
						{
							var foundSkin = skinsList.FirstOrDefault(skin =>
								((int?)skin?["weapon_defindex"] ?? 0) == weaponDefIndex &&
								((int?)skin?["paint"] ?? 0) == paintId &&
								skin?["image"] != null
							);
							var image = foundSkin?["image"]?.ToString() ?? "";
							PlayerWeaponImage[p.Slot] = image;
							AddTimer(2.0f, () => PlayerWeaponImage.Remove(p.Slot), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
						}

						p.Print(Localizer["wp_skin_menu_select", selectedSkin]);

						if (!gPlayerWeaponsInfo[p.Slot].TryGetValue(weaponDefIndex, out var value))
						{
                            value = new WeaponInfo();
                            gPlayerWeaponsInfo[p.Slot][weaponDefIndex] = value;
						}

                        value.Paint = paintId;
                        value.Wear = 0.01f;
                        value.Seed = 0;

						PlayerInfo playerInfo = new PlayerInfo
						{
							UserId = p.UserId,
							Slot = p.Slot,
							Index = (int)p.Index,
							SteamId = p.SteamID.ToString(),
							Name = p.PlayerName,
							IpAddress = p.IpAddress?.Split(":")[0]
						};

						if (!g_bCommandsAllowed || (LifeState_t)p.LifeState != LifeState_t.LIFE_ALIVE ||
						    weaponSync == null) return;
						RefreshWeapons(player);

						try
						{
							_ = Task.Run(async () => await weaponSync.SyncWeaponPaintsToDatabase(playerInfo));
						}
						catch (Exception ex)
						{
							Utility.Log($"Error syncing weapon paints: {ex.Message}");
						}
					}
				};

				// Add skin options to the submenu for the selected weapon
				if (skinsForSelectedWeapon != null)
				{
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
				}
				if (player != null && Utility.IsPlayerValid(player))
					MenuManager.OpenChatMenu(player, skinSubMenu);
			};

			// Add weapon options to the weapon selection menu
			foreach (var weaponName in WeaponList.Keys.Select(weaponClass => WeaponList[weaponClass]))
			{
				weaponSelectionMenu.AddMenuOption(weaponName, handleWeaponSelection);
			}
			// Command to open the weapon selection menu for players
			AddCommand($"css_{Config.Additional.CommandSkinSelection}", "Skins selection menu", (player, info) =>
					{
						if (!Utility.IsPlayerValid(player)) return;

						if (player == null || player.UserId == null) return;

						if (!commandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
			DateTime.UtcNow >= (commandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
						{
							commandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
							MenuManager.OpenChatMenu(player, weaponSelectionMenu);
							return;
						}
						if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
						{
							player!.Print(Localizer["wp_command_cooldown"]);
						}
					});
		}

		private void SetupGlovesMenu()
		{
			var glovesSelectionMenu = new ChatMenu(Localizer["wp_glove_menu_title"])
			{
				PostSelectAction = PostSelectAction.Close
			};

			var handleGloveSelection = (CCSPlayerController? player, ChatMenuOption option) =>
			{
				if (!Utility.IsPlayerValid(player) || player is null) return;

				var selectedPaintName = option.Text;

				var selectedGlove = glovesList.FirstOrDefault(g => g.ContainsKey("paint_name") && g["paint_name"]?.ToString() == selectedPaintName);
				var image = selectedGlove?["image"]?.ToString() ?? "";
				if (selectedGlove == null ||
				    !selectedGlove.ContainsKey("weapon_defindex") ||
				    !selectedGlove.ContainsKey("paint") ||
				    !int.TryParse(selectedGlove["weapon_defindex"]?.ToString(), out var weaponDefindex) ||
				    !int.TryParse(selectedGlove["paint"]?.ToString(), out var paint)) return;
				if (Config.Additional.ShowSkinImage)
				{
					PlayerWeaponImage[player.Slot] = image;
					AddTimer(2.0f, () => PlayerWeaponImage.Remove(player.Slot), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
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
					g_playersGlove[player.Slot] = (ushort)weaponDefindex;

					if (!gPlayerWeaponsInfo[player.Slot].ContainsKey(weaponDefindex))
					{
						WeaponInfo weaponInfo = new()
						{
							Paint = paint
						};
						gPlayerWeaponsInfo[player.Slot][weaponDefindex] = weaponInfo;
					}
				}
				else
				{
					g_playersGlove.TryRemove(player.Slot, out _);
				}

				if (!string.IsNullOrEmpty(Localizer["wp_glove_menu_select"]))
				{
					player!.Print(Localizer["wp_glove_menu_select", selectedPaintName]);
				}

				if (weaponSync == null) return;
				
				_ = Task.Run(async () =>
				{
					await weaponSync.SyncGloveToDatabase(playerInfo, weaponDefindex);

					if (!gPlayerWeaponsInfo[playerInfo.Slot].TryGetValue(weaponDefindex, out var value))
					{
						value = new WeaponInfo();
						gPlayerWeaponsInfo[playerInfo.Slot][weaponDefindex] = value;
					}

					value.Paint = paint;
					value.Wear = 0.00f;
					value.Seed = 0;

					await weaponSync.SyncWeaponPaintsToDatabase(playerInfo);
				});
				
				AddTimer(0.1f, () => GivePlayerGloves(player));
				AddTimer(0.15f, () => GivePlayerGloves(player));
			};

			// Add weapon options to the weapon selection menu
			foreach (var paintName in glovesList.Select(gloveObject => gloveObject["paint_name"]?.ToString() ?? "").Where(paintName => paintName.Length > 0))
			{
				glovesSelectionMenu.AddMenuOption(paintName, handleGloveSelection);
			}

			// Command to open the weapon selection menu for players
			AddCommand($"css_{Config.Additional.CommandGlove}", "Gloves selection menu", (player, info) =>
							{
								if (!Utility.IsPlayerValid(player) || !g_bCommandsAllowed) return;

								if (player == null || player.UserId == null) return;

								if (!commandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
					DateTime.UtcNow >= (commandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
								{
									commandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
									MenuManager.OpenChatMenu(player, glovesSelectionMenu);
									return;
								}
								if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
								{
									player!.Print(Localizer["wp_command_cooldown"]);
								}
							});
		}

		private void SetupAgentsMenu()
		{
			var handleAgentSelection = (CCSPlayerController? player, ChatMenuOption option) =>
			{
				if (!Utility.IsPlayerValid(player) || player is null) return;

				var selectedPaintName = option.Text;
				var selectedAgent = agentsList.FirstOrDefault(g =>
					g.ContainsKey("agent_name") &&
					g["agent_name"] != null && g["agent_name"]!.ToString() == selectedPaintName &&
					g["team"] != null && (int)(g["team"]!) == player.TeamNum);

				if (selectedAgent == null) return;

				if (
					selectedAgent.ContainsKey("model")
				)
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
						PlayerWeaponImage[player.Slot] = image;
						AddTimer(2.0f, () => PlayerWeaponImage.Remove(player.Slot), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
					}

					if (!string.IsNullOrEmpty(Localizer["wp_agent_menu_select"]))
					{
						player!.Print(Localizer["wp_agent_menu_select", selectedPaintName]);
					}

					if (player.TeamNum == 3)
					{
						g_playersAgent.AddOrUpdate(player.Slot,
						key => (selectedAgent["model"]!.ToString().Equals("null") ? null : selectedAgent["model"]!.ToString(), null),
						(key, oldValue) => (selectedAgent["model"]!.ToString().Equals("null") ? null : selectedAgent["model"]!.ToString(), oldValue.T));
					}
					else
					{
						g_playersAgent.AddOrUpdate(player.Slot,
							key => (null, selectedAgent["model"]!.ToString().Equals("null") ? null : selectedAgent["model"]!.ToString()),
							(key, oldValue) => (oldValue.CT, selectedAgent["model"]!.ToString().Equals("null") ? null : selectedAgent["model"]!.ToString())
						);
					}

					if (weaponSync != null)
					{
						_ = Task.Run(async () =>
						{
							await weaponSync.SyncAgentToDatabase(playerInfo);
						});
					}
				};
			};

			// Command to open the weapon selection menu for players
			AddCommand($"css_{Config.Additional.CommandAgent}", "Agents selection menu", (player, info) =>
			{
				if (!Utility.IsPlayerValid(player) || !g_bCommandsAllowed) return;

				if (player == null || player.UserId == null) return;

				if (!commandsCooldown.TryGetValue(player.Slot, out DateTime cooldownEndTime) ||
	DateTime.UtcNow >= (commandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
				{
					var agentsSelectionMenu = new ChatMenu(Localizer["wp_agent_menu_title"])
					{
						PostSelectAction = PostSelectAction.Close
					};

					var filteredAgents = agentsList.Where(agentObject =>
					{
						if (agentObject["team"]?.Value<int>() is { } teamNum)
						{
							return teamNum == player.TeamNum;
						}
						else
						{
							return false;
						}
					});

					// Add weapon options to the weapon selection menu

					foreach (var agentObject in filteredAgents)
					{
						var paintName = agentObject["agent_name"]?.ToString() ?? "";

						if (paintName.Length > 0)
							agentsSelectionMenu.AddMenuOption(paintName, handleAgentSelection);
					}

					commandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
					MenuManager.OpenChatMenu(player, agentsSelectionMenu);
					return;
				}
				if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
				{
					player!.Print(Localizer["wp_command_cooldown"]);
				}
			});
		}

		private void SetupMusicMenu()
		{
			var musicSelectionMenu = new ChatMenu(Localizer["wp_music_menu_title"])
			{
				PostSelectAction = PostSelectAction.Close
			};

			var handleMusicSelection = (CCSPlayerController? player, ChatMenuOption option) =>
			{
				if (!Utility.IsPlayerValid(player) || player is null) return;

				var selectedPaintName = option.Text;

				var selectedMusic = musicList.FirstOrDefault(g => g.ContainsKey("name") && g["name"]?.ToString() == selectedPaintName);
				if (selectedMusic != null)
				{
					if (!selectedMusic.ContainsKey("id") ||
					    !selectedMusic.ContainsKey("name") ||
					    !int.TryParse(selectedMusic["id"]?.ToString(), out var paint)) return;
					var image = selectedMusic["image"]?.ToString() ?? "";
					if (Config.Additional.ShowSkinImage)
					{
						PlayerWeaponImage[player.Slot] = image;
						AddTimer(2.0f, () => PlayerWeaponImage.Remove(player.Slot), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
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
						g_playersMusic[player.Slot] = (ushort)paint;
					}
					else
					{
						g_playersMusic[player.Slot] = 0;
					}

					if (!string.IsNullOrEmpty(Localizer["wp_music_menu_select"]))
					{
						player!.Print(Localizer["wp_music_menu_select", selectedPaintName]);
					}

					if (weaponSync != null)
					{
						_ = Task.Run(async () =>
						{
							await weaponSync.SyncMusicToDatabase(playerInfo, (ushort)paint);
						});
					}

					//RefreshGloves(player);
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

					g_playersMusic[player.Slot] = 0;

					if (!string.IsNullOrEmpty(Localizer["wp_music_menu_select"]))
					{
						player!.Print(Localizer["wp_music_menu_select", Localizer["None"]]);
					}

					if (weaponSync != null)
					{
						_ = Task.Run(async () =>
						{
							await weaponSync.SyncMusicToDatabase(playerInfo, 0);
						});
					}
				}
			};

			musicSelectionMenu.AddMenuOption(Localizer["None"], handleMusicSelection);
			// Add weapon options to the weapon selection menu
			foreach (var paintName in musicList.Select(musicObject => musicObject["name"]?.ToString() ?? "").Where(paintName => paintName.Length > 0))
			{
				musicSelectionMenu.AddMenuOption(paintName, handleMusicSelection);
			}

			// Command to open the weapon selection menu for players
			AddCommand($"css_{Config.Additional.CommandMusic}", "Music selection menu", (player, info) =>
			{
				if (!Utility.IsPlayerValid(player) || !g_bCommandsAllowed) return;

				if (player == null || player.UserId == null) return;

				if (!commandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
	DateTime.UtcNow >= (commandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
				{
					commandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
					MenuManager.OpenChatMenu(player, musicSelectionMenu);
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
