using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;

namespace WeaponPaints
{
	public partial class WeaponPaints
	{
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
					if (!Utility.IsPlayerValid(player) || !player!.PlayerPawn.IsValid) return;

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

			var giveItemMenu = new ChatMenu(Utility.ReplaceTags($" {Config.Messages.KnifeMenuTitle}"));
			var handleGive = (CCSPlayerController? player, ChatMenuOption option) =>
			{
				if (Utility.IsPlayerValid(player))
				{
					var knifeName = option.Text;
					var knifeKey = knivesOnly.FirstOrDefault(x => x.Value == knifeName).Key;
					if (!string.IsNullOrEmpty(knifeKey))
					{
						string temp = "";

						if (!string.IsNullOrEmpty(Config.Messages.ChosenKnifeMenu))
						{
							temp = $" {Config.Prefix} {Config.Messages.ChosenKnifeMenu}".Replace("{KNIFE}", knifeName);
							player!.PrintToChat(Utility.ReplaceTags(temp));
						}

						if (!string.IsNullOrEmpty(Config.Messages.ChosenKnifeMenuKill) && Config.Additional.CommandKillEnabled)
						{
							temp = $" {Config.Prefix} {Config.Messages.ChosenKnifeMenuKill}";
							player!.PrintToChat(Utility.ReplaceTags(temp));
						}

						g_playersKnife[(int)player!.EntityIndex!.Value.Value] = knifeKey;

						if (player!.PawnIsAlive && g_bCommandsAllowed)
						{
							g_changedKnife.Add((int)player.EntityIndex!.Value.Value);
							RefreshWeapons(player);
							//RefreshPlayerKnife(player);

							/*
							AddTimer(1.0f, () => GiveKnifeToPlayer(player));
							*/
						}
						if (weaponSync != null)
							Task.Run(() => weaponSync.SyncKnifeToDatabase((int)player.EntityIndex!.Value.Value, knifeKey));
					}
				}
			};
			foreach (var knifePair in knivesOnly)
			{
				giveItemMenu.AddMenuOption(knifePair.Value, handleGive);
			}
			AddCommand($"css_{Config.Additional.CommandKnife}", "Knife Menu", (player, info) =>
			{
				if (!Utility.IsPlayerValid(player) || !g_bCommandsAllowed) return;
				int playerIndex = (int)player!.EntityIndex!.Value.Value;

				if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CmdRefreshCooldownSeconds) && playerIndex > 0 && playerIndex < commandCooldown.Length)
				{
					commandCooldown[playerIndex] = DateTime.UtcNow;
					ChatMenus.OpenMenu(player, giveItemMenu);
					return;
				}
				if (!string.IsNullOrEmpty(Config.Messages.CooldownRefreshCommand))
				{
					string temp = $" {Config.Prefix} {Config.Messages.CooldownRefreshCommand}";
					player.PrintToChat(Utility.ReplaceTags(temp));
				}
			});
		}

		private void SetupSkinsMenu()
		{
			var classNamesByWeapon = weaponList.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
			var weaponSelectionMenu = new ChatMenu(Utility.ReplaceTags($" {Config.Messages.WeaponMenuTitle}"));

			// Function to handle skin selection for a specific weapon
			var handleWeaponSelection = (CCSPlayerController? player, ChatMenuOption option) =>
			{
				if (!Utility.IsPlayerValid(player)) return;

				int playerIndex = (int)player!.EntityIndex!.Value.Value;
				string selectedWeapon = option.Text;
				if (classNamesByWeapon.TryGetValue(selectedWeapon, out string? selectedWeaponClassname))
				{
					if (selectedWeaponClassname == null) return;
					var skinsForSelectedWeapon = skinsList?.Where(skin =>
					skin != null &&
					skin.TryGetValue("weapon_name", out var weaponName) &&
					weaponName?.ToString() == selectedWeaponClassname
				)?.ToList();

					var skinSubMenu = new ChatMenu(Utility.ReplaceTags($" {Config.Messages.SkinMenuTitle}").Replace("{WEAPON}", selectedWeapon));

					// Function to handle skin selection for the chosen weapon
					var handleSkinSelection = (CCSPlayerController? p, ChatMenuOption opt) =>
					{
						if (p == null || !p.IsValid || !p.EntityIndex.HasValue) return;

						playerIndex = (int)p.EntityIndex.Value.Value;

						var steamId = new SteamID(p.SteamID);
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
							string temp = $" {Config.Prefix} {Config.Messages.ChosenSkinMenu}".Replace("{SKIN}", selectedSkin);
							p.PrintToChat(Utility.ReplaceTags(temp));

							if (!gPlayerWeaponsInfo[playerIndex].ContainsKey(weaponDefIndex))
							{
								gPlayerWeaponsInfo[playerIndex][weaponDefIndex] = new WeaponInfo();
							}

							gPlayerWeaponsInfo[playerIndex][weaponDefIndex].Paint = paintID;
							gPlayerWeaponsInfo[playerIndex][weaponDefIndex].Wear = 0.0f;
							gPlayerWeaponsInfo[playerIndex][weaponDefIndex].Seed = 0;

							if (!Config.GlobalShare)
							{
								if (weaponSync == null) return;
								Task.Run(async () =>
								{
									await weaponSync.SyncWeaponPaintsToDatabase(p);
								});
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
					ChatMenus.OpenMenu(player, skinSubMenu);
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
				int playerIndex = (int)player!.EntityIndex!.Value.Value;

				if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CmdRefreshCooldownSeconds) && playerIndex > 0 && playerIndex < commandCooldown.Length)
				{
					commandCooldown[playerIndex] = DateTime.UtcNow;
					ChatMenus.OpenMenu(player, weaponSelectionMenu);
					return;
				}
				if (!string.IsNullOrEmpty(Config.Messages.CooldownRefreshCommand))
				{
					string temp = $"{Config.Prefix} {Config.Messages.CooldownRefreshCommand}";
					player.PrintToChat(Utility.ReplaceTags(temp));
				}

			});
		}

		private void OnCommandRefresh(CCSPlayerController? player, CommandInfo command)
		{
			if (!Config.Additional.CommandWpEnabled || !Config.Additional.SkinEnabled || !g_bCommandsAllowed) return;
			if (!Utility.IsPlayerValid(player)) return;
			string temp = "";
			if (!player!.EntityIndex.HasValue) return;
			int playerIndex = (int)player!.EntityIndex!.Value.Value;
			if (playerIndex != 0 && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CmdRefreshCooldownSeconds))
			{
				commandCooldown[playerIndex] = DateTime.UtcNow;
				if (weaponSync != null)
					Task.Run(async () => await weaponSync.GetWeaponPaintsFromDatabase(playerIndex));
				if (Config.Additional.KnifeEnabled)
				{
					/*if (PlayerHasKnife(player))
						RefreshPlayerKnife(player);
					/*
					AddTimer(1.0f, () =>
					{
						GiveKnifeToPlayer(player);
					});
					*/
					if (weaponSync != null)
						Task.Run(async () => await weaponSync.GetKnifeFromDatabase(playerIndex));
					/*
					RemoveKnifeFromPlayer(player);
					AddTimer(0.2f, () => GiveKnifeToPlayer(player));
					*/

					RefreshWeapons(player);
				}
				if (!string.IsNullOrEmpty(Config.Messages.SuccessRefreshCommand))
				{
					temp = $" {Config.Prefix} {Config.Messages.SuccessRefreshCommand}";
					player.PrintToChat(Utility.ReplaceTags(temp));
				}
				return;
			}
			if (!string.IsNullOrEmpty(Config.Messages.CooldownRefreshCommand))
			{
				temp = $" {Config.Prefix} {Config.Messages.CooldownRefreshCommand}";
				player.PrintToChat(Utility.ReplaceTags(temp));
			}
		}
		private void OnCommandWS(CCSPlayerController? player, CommandInfo command)
		{
			if (!Config.Additional.SkinEnabled) return;
			if (!Utility.IsPlayerValid(player)) return;

			string temp;
			if (!string.IsNullOrEmpty(Config.Messages.WebsiteMessageCommand))
			{
				temp = $" {Config.Prefix} {Config.Messages.WebsiteMessageCommand}";
				player!.PrintToChat(Utility.ReplaceTags(temp));
			}
			if (!string.IsNullOrEmpty(Config.Messages.SynchronizeMessageCommand))
			{
				temp = $" {Config.Prefix} {Config.Messages.SynchronizeMessageCommand}";
				player!.PrintToChat(Utility.ReplaceTags(temp));
			}
			if (!Config.Additional.KnifeEnabled) return;
			if (!string.IsNullOrEmpty(Config.Messages.KnifeMessageCommand))
			{
				temp = $" {Config.Prefix} {Config.Messages.KnifeMessageCommand}";
				player!.PrintToChat(Utility.ReplaceTags(temp));
			}
		}
	}
}
