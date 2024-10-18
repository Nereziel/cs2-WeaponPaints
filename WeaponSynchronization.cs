using Dapper;
using MySqlConnector;
using System.Collections.Concurrent;


namespace WeaponPaints
{
	internal class WeaponSynchronization
	{
		private readonly WeaponPaintsConfig _config;
		private readonly Database _database;

		internal WeaponSynchronization(Database database, WeaponPaintsConfig config)
		{
			_database = database;
			_config = config;
		}

		internal async Task GetPlayerData(PlayerInfo? player)
		{
			try
			{
				await using var connection = await _database.GetConnectionAsync();

				if (_config.Additional.KnifeEnabled)
					GetKnifeFromDatabase(player, connection);
				if (_config.Additional.GloveEnabled)
					GetGloveFromDatabase(player, connection);
				if (_config.Additional.AgentEnabled)
					GetAgentFromDatabase(player, connection);
				if (_config.Additional.MusicEnabled)
					GetMusicFromDatabase(player, connection);
				if (_config.Additional.SkinEnabled)
					GetWeaponPaintsFromDatabase(player, connection);
				if (_config.Additional.PinsEnabled)
					GetPinsFromDatabase(player, connection);
			}
			catch (Exception ex)
			{
				// Log the exception or handle it appropriately
				Console.WriteLine($"An error occurred: {ex.Message}");
			}
		}

		private void GetKnifeFromDatabase(PlayerInfo? player, MySqlConnection connection)
		{
			try
			{
				if (!_config.Additional.KnifeEnabled || string.IsNullOrEmpty(player?.SteamId))
					return;

				const string query = "SELECT `knife` FROM `wp_player_knife` WHERE `steamid` = @steamid";
				var playerKnife = connection.QueryFirstOrDefault<string>(query, new { steamid = player.SteamId });

				if (!string.IsNullOrEmpty(playerKnife))
				{
					WeaponPaints.GPlayersKnife[player.Slot] = playerKnife;
				}
			}
			catch (Exception ex)
			{
				Utility.Log($"An error occurred in GetKnifeFromDatabase: {ex.Message}");
			}
		}

		private void GetGloveFromDatabase(PlayerInfo? player, MySqlConnection connection)
		{
			try
			{
				if (!_config.Additional.GloveEnabled || string.IsNullOrEmpty(player?.SteamId))
					return;

				const string query = "SELECT `weapon_defindex` FROM `wp_player_gloves` WHERE `steamid` = @steamid";
				var gloveData = connection.QueryFirstOrDefault<ushort?>(query, new { steamid = player.SteamId });

				if (gloveData != null)
				{
					WeaponPaints.GPlayersGlove[player.Slot] = gloveData.Value;
				}
			}
			catch (Exception ex)
			{
				Utility.Log($"An error occurred in GetGloveFromDatabase: {ex.Message}");
			}
		}

		private void GetAgentFromDatabase(PlayerInfo? player, MySqlConnection connection)
		{
			try
			{
				if (!_config.Additional.AgentEnabled || string.IsNullOrEmpty(player?.SteamId))
					return;

				const string query = "SELECT `agent_ct`, `agent_t` FROM `wp_player_agents` WHERE `steamid` = @steamid";
				var agentData = connection.QueryFirstOrDefault<(string, string)>(query, new { steamid = player.SteamId });

				if (agentData == default) return;
				var agentCT = agentData.Item1;
				var agentT = agentData.Item2;

				if (!string.IsNullOrEmpty(agentCT) || !string.IsNullOrEmpty(agentT))
				{
					WeaponPaints.GPlayersAgent[player.Slot] = (
						agentCT,
						agentT
					);
				}
			}
			catch (Exception ex)
			{
				Utility.Log($"An error occurred in GetAgentFromDatabase: {ex.Message}");
			}
		}

		private void GetWeaponPaintsFromDatabase(PlayerInfo? player, MySqlConnection connection)
		{
			try
			{
				if (!_config.Additional.SkinEnabled || player == null || string.IsNullOrEmpty(player.SteamId))
					return;

				var weaponInfos = new ConcurrentDictionary<int, WeaponInfo>();

				const string query = "SELECT * FROM `wp_player_skins` WHERE `steamid` = @steamid";
				var playerSkins = connection.Query<dynamic>(query, new { steamid = player.SteamId });

				foreach (var row in playerSkins)
				{
					int weaponDefIndex = row?.weapon_defindex ?? 0;
					int weaponPaintId = row?.weapon_paint_id ?? 0;
					float weaponWear = row?.weapon_wear ?? 0f;
					int weaponSeed = row?.weapon_seed ?? 0;
					string weaponNameTag = row?.weapon_nametag ?? "";

					string[]? keyChainParts = row?.weapon_keychain?.ToString().Split(';');

					KeyChainInfo keyChainInfo = new KeyChainInfo();

					if (keyChainParts!.Length == 5 &&
						uint.TryParse(keyChainParts[0], out uint keyChainId) &&
						float.TryParse(keyChainParts[1], out float keyChainOffsetX) &&
						float.TryParse(keyChainParts[2], out float keyChainOffsetY) &&
						float.TryParse(keyChainParts[3], out float keyChainOffsetZ) &&
						uint.TryParse(keyChainParts[4], out uint keyChainSeed))
					{
						// Successfully parsed the values
						keyChainInfo.Id = keyChainId;
						keyChainInfo.OffsetX = keyChainOffsetX;
						keyChainInfo.OffsetY = keyChainOffsetY;
						keyChainInfo.OffsetZ = keyChainOffsetZ;
						keyChainInfo.Seed = keyChainSeed;
					}
					else
					{
						// Failed to parse the values, default to 0
						keyChainInfo.Id = 0;
						keyChainInfo.OffsetX = 0f;
						keyChainInfo.OffsetY = 0f;
						keyChainInfo.OffsetZ = 0f;
						keyChainInfo.Seed = 0;
					}

					// Create the WeaponInfo object
					WeaponInfo weaponInfo = new WeaponInfo
					{
						Paint = weaponPaintId,
						Seed = weaponSeed,
						Wear = weaponWear,
						Nametag = weaponNameTag,
						KeyChain = keyChainInfo
					};

					// Retrieve and parse sticker data (up to 5 slots)
					for (int i = 0; i <= 4; i++)
					{
						// Access the sticker data dynamically using reflection
						string stickerColumn = $"weapon_sticker_{i}";
						var stickerData = ((IDictionary<string, object>)row!)[stickerColumn]; // Safely cast row to a dictionary

						if (string.IsNullOrEmpty(stickerData.ToString())) continue;
						
						var parts = stickerData.ToString()!.Split(';');

						//"id;schema;x;y;wear;scale;rotation"
						if (parts.Length != 7 ||
						    !uint.TryParse(parts[0], out uint stickerId) ||
						    !uint.TryParse(parts[1], out uint stickerSchema) ||
						    !float.TryParse(parts[2], out float stickerOffsetX) ||
						    !float.TryParse(parts[3], out float stickerOffsetY) ||
						    !float.TryParse(parts[4], out float stickerWear) ||
						    !float.TryParse(parts[5], out float stickerScale) ||
						    !float.TryParse(parts[6], out float stickerRotation)) continue;
						
						StickerInfo stickerInfo = new StickerInfo
						{
							Id = stickerId,
							Schema = stickerSchema,
							OffsetX = stickerOffsetX,
							OffsetY = stickerOffsetY,
							Wear = stickerWear,
							Scale = stickerScale,
							Rotation = stickerRotation
						};

						weaponInfo.Stickers.Add(stickerInfo);
					}

					weaponInfos[weaponDefIndex] = weaponInfo;
				}

				WeaponPaints.GPlayerWeaponsInfo[player.Slot] = weaponInfos;
			}
			catch (Exception ex)
			{
				Utility.Log($"An error occurred in GetWeaponPaintsFromDatabase: {ex.Message}");
			}
		}

		private void GetMusicFromDatabase(PlayerInfo? player, MySqlConnection connection)
		{
			try
			{
				if (!_config.Additional.MusicEnabled || string.IsNullOrEmpty(player?.SteamId))
					return;

				const string query = "SELECT `music_id` FROM `wp_player_music` WHERE `steamid` = @steamid";
				var musicData = connection.QueryFirstOrDefault<ushort?>(query, new { steamid = player.SteamId });

				if (musicData != null)
				{
					WeaponPaints.GPlayersMusic[player.Slot] = musicData.Value;
				}
			}
			catch (Exception ex)
			{
				Utility.Log($"An error occurred in GetMusicFromDatabase: {ex.Message}");
			}
		}

		private void GetPinsFromDatabase(PlayerInfo? player, MySqlConnection connection)
		{
			try
			{
				if (string.IsNullOrEmpty(player?.SteamId))
					return;

				const string query = "SELECT `id` FROM `wp_player_pins` WHERE `steamid` = @steamid";
				var pinData = connection.QueryFirstOrDefault<ushort?>(query, new { steamid = player.SteamId });

				if (pinData != null)
				{
					WeaponPaints.GPlayersPin[player.Slot] = pinData.Value;
				}
			}
			catch (Exception ex)
			{
				Utility.Log($"An error occurred in GetPinsFromDatabase: {ex.Message}");
			}
		}

		internal async Task SyncKnifeToDatabase(PlayerInfo player, string knife)
		{
			if (!_config.Additional.KnifeEnabled || string.IsNullOrEmpty(player.SteamId) || string.IsNullOrEmpty(knife)) return;

			const string query = "INSERT INTO `wp_player_knife` (`steamid`, `knife`) VALUES(@steamid, @newKnife) ON DUPLICATE KEY UPDATE `knife` = @newKnife";

			try
			{
				await using var connection = await _database.GetConnectionAsync();
				await connection.ExecuteAsync(query, new { steamid = player.SteamId, newKnife = knife });
			}
			catch (Exception e)
			{
				Utility.Log($"Error syncing knife to database: {e.Message}");
			}
		}

		internal async Task SyncGloveToDatabase(PlayerInfo player, int defindex)
		{
			if (!_config.Additional.GloveEnabled || string.IsNullOrEmpty(player.SteamId)) return;

			try
			{
				await using var connection = await _database.GetConnectionAsync();
				const string query = "INSERT INTO `wp_player_gloves` (`steamid`, `weapon_defindex`) VALUES(@steamid, @weapon_defindex) ON DUPLICATE KEY UPDATE `weapon_defindex` = @weapon_defindex";
				await connection.ExecuteAsync(query, new { steamid = player.SteamId, weapon_defindex = defindex });
			}
			catch (Exception e)
			{
				Utility.Log($"Error syncing glove to database: {e.Message}");
			}
		}

		internal async Task SyncAgentToDatabase(PlayerInfo player)
		{
			if (!_config.Additional.AgentEnabled || string.IsNullOrEmpty(player.SteamId)) return;

			const string query = """
			                     					INSERT INTO `wp_player_agents` (`steamid`, `agent_ct`, `agent_t`)
			                     					VALUES(@steamid, @agent_ct, @agent_t)
			                     					ON DUPLICATE KEY UPDATE
			                     						`agent_ct` = @agent_ct,
			                     						`agent_t` = @agent_t
			                     """;
			try
			{
				await using var connection = await _database.GetConnectionAsync();

				await connection.ExecuteAsync(query, new { steamid = player.SteamId, agent_ct = WeaponPaints.GPlayersAgent[player.Slot].CT, agent_t = WeaponPaints.GPlayersAgent[player.Slot].T });
			}
			catch (Exception e)
			{
				Utility.Log($"Error syncing agents to database: {e.Message}");
			}
		}

		internal async Task SyncWeaponPaintsToDatabase(PlayerInfo player)
		{
			if (string.IsNullOrEmpty(player.SteamId) || !WeaponPaints.GPlayerWeaponsInfo.TryGetValue(player.Slot, out var weaponsInfo))
				return;

			try
			{
				await using var connection = await _database.GetConnectionAsync();

				foreach (var (weaponDefIndex, weaponInfo) in weaponsInfo)
				{
					var paintId = weaponInfo.Paint;
					var wear = weaponInfo.Wear;
					var seed = weaponInfo.Seed;

					const string queryCheckExistence = "SELECT COUNT(*) FROM `wp_player_skins` WHERE `steamid` = @steamid AND `weapon_defindex` = @weaponDefIndex";

					var existingRecordCount = await connection.ExecuteScalarAsync<int>(queryCheckExistence, new { steamid = player.SteamId, weaponDefIndex = weaponDefIndex });

					string query;
					object parameters;

					if (existingRecordCount > 0)
					{
						query = "UPDATE `wp_player_skins` SET `weapon_paint_id` = @paintId, `weapon_wear` = @wear, `weapon_seed` = @seed WHERE `steamid` = @steamid AND `weapon_defindex` = @weaponDefIndex";
						parameters = new { steamid = player.SteamId, weaponDefIndex = weaponDefIndex, paintId, wear, seed };
					}
					else
					{
						query = "INSERT INTO `wp_player_skins` (`steamid`, `weapon_defindex`, `weapon_paint_id`, `weapon_wear`, `weapon_seed`) " +
								"VALUES (@steamid, @weaponDefIndex, @paintId, @wear, @seed)";
						parameters = new { steamid = player.SteamId, weaponDefIndex = weaponDefIndex, paintId, wear, seed };
					}

					await connection.ExecuteAsync(query, parameters);
				}
			}
			catch (Exception e)
			{
				Utility.Log($"Error syncing weapon paints to database: {e.Message}");
			}
		}

		internal async Task SyncMusicToDatabase(PlayerInfo player, ushort music)
		{
			if (!_config.Additional.MusicEnabled || string.IsNullOrEmpty(player.SteamId)) return;

			try
			{
				await using var connection = await _database.GetConnectionAsync();
				const string query = "INSERT INTO `wp_player_music` (`steamid`, `music_id`) VALUES(@steamid, @newMusic) ON DUPLICATE KEY UPDATE `music_id` = @newMusic";
				await connection.ExecuteAsync(query, new { steamid = player.SteamId, newMusic = music });
			}
			catch (Exception e)
			{
				Utility.Log($"Error syncing music kit to database: {e.Message}");
			}
		}

		internal async Task SyncStatTrakToDatabase(PlayerInfo player, Dictionary<int, int> weaponStatTrakCounts)
		{
			if (string.IsNullOrEmpty(player.SteamId) || weaponStatTrakCounts == null || weaponStatTrakCounts.Count == 0)
				return;

			try
			{
				await using var connection = await _database.GetConnectionAsync();
				await using var transaction = await connection.BeginTransactionAsync();

				foreach (var weapon in weaponStatTrakCounts)
				{
					int defindex = weapon.Key;
					int statTrakCount = weapon.Value;

					const string query = @"
						INSERT INTO `wp_player_skins` (`steamid`, `weapon_defindex`, `weapon_stattrak_count`) 
						VALUES (@steamid, @weaponDefIndex, @StatTrakCount) 
						ON DUPLICATE KEY UPDATE `weapon_stattrak_count` = @StatTrakCount";

					var parameters = new
					{
						steamid = player.SteamId,
						weaponDefIndex = defindex,
						StatTrakCount = statTrakCount
					};

					await connection.ExecuteAsync(query, parameters, transaction);
				}

				await transaction.CommitAsync();
			}
			catch (Exception e)
			{
				Utility.Log($"Error syncing weapon paints to database: {e.Message}");
			}
		}
	}
}