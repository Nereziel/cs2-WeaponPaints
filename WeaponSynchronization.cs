using Dapper;
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

		internal async Task GetKnifeFromDatabase(PlayerInfo player)
		{
			try
			{
				if (!_config.Additional.KnifeEnabled || string.IsNullOrEmpty(player?.SteamId))
					return;

				await using var connection = await _database.GetConnectionAsync();
				string query = "SELECT `knife` FROM `wp_player_knife` WHERE `steamid` = @steamid";
				string? playerKnife = await connection.QueryFirstOrDefaultAsync<string>(query, new { steamid = player.SteamId });

				if (!string.IsNullOrEmpty(playerKnife))
				{
					WeaponPaints.g_playersKnife[player.Slot] = playerKnife;
				}
			}
			catch (Exception ex)
			{
				Utility.Log($"An error occurred in GetKnifeFromDatabase: {ex.Message}");
			}
		}

		internal async Task GetGloveFromDatabase(PlayerInfo player)
		{
			try
			{
				if (!_config.Additional.GloveEnabled || string.IsNullOrEmpty(player?.SteamId))
					return;

				await using var connection = await _database.GetConnectionAsync();
				string query = "SELECT `weapon_defindex` FROM `wp_player_gloves` WHERE `steamid` = @steamid";
				ushort? gloveData = await connection.QueryFirstOrDefaultAsync<ushort?>(query, new { steamid = player.SteamId });

				if (gloveData != null)
				{
					WeaponPaints.g_playersGlove[player.Slot] = gloveData.Value;
				}
			}
			catch (Exception ex)
			{
				Utility.Log($"An error occurred in GetGloveFromDatabase: {ex.Message}");
			}
		}

		internal async Task GetAgentFromDatabase(PlayerInfo player)
		{
			try
			{
				if (!_config.Additional.AgentEnabled || string.IsNullOrEmpty(player?.SteamId))
					return;

				await using var connection = await _database.GetConnectionAsync();
				string query = "SELECT `agent_ct`, `agent_t` FROM `wp_player_agents` WHERE `steamid` = @steamid";
				var agentData = await connection.QueryFirstOrDefaultAsync<(string, string)>(query, new { steamid = player.SteamId });

				if (agentData != default)
				{
					string agentCT = agentData.Item1;
					string agentT = agentData.Item2;

					if (!string.IsNullOrEmpty(agentCT) || !string.IsNullOrEmpty(agentT))
					{
						WeaponPaints.g_playersAgent[player.Slot] = (
							agentCT,
							agentT
						);
					}
				}
			}
			catch (Exception ex)
			{
				Utility.Log($"An error occurred in GetGloveFromDatabase: {ex.Message}");
			}
		}

		internal async Task GetWeaponPaintsFromDatabase(PlayerInfo player)
		{
			try
			{
				if (!_config.Additional.SkinEnabled || player == null || string.IsNullOrEmpty(player.SteamId))
					return;

				var weaponInfos = new ConcurrentDictionary<int, WeaponInfo>();

				await using var connection = await _database.GetConnectionAsync();
				string query = "SELECT * FROM `wp_player_skins` WHERE `steamid` = @steamid";
				var playerSkins = await connection.QueryAsync<dynamic>(query, new { steamid = player.SteamId });

				if (playerSkins == null)
				{
					WeaponPaints.gPlayerWeaponsInfo[player.Slot] = weaponInfos;
					return;
				}

				foreach (var row in playerSkins)
				{
					int weaponDefIndex = row?.weapon_defindex ?? 0;
					int weaponPaintId = row?.weapon_paint_id ?? 0;
					float weaponWear = row?.weapon_wear ?? 0f;
					int weaponSeed = row?.weapon_seed ?? 0;

					WeaponInfo weaponInfo = new WeaponInfo
					{
						Paint = weaponPaintId,
						Seed = weaponSeed,
						Wear = weaponWear
					};

					weaponInfos[weaponDefIndex] = weaponInfo;
				}

				WeaponPaints.gPlayerWeaponsInfo[player.Slot] = weaponInfos;
			}
			catch (Exception ex)
			{
				Utility.Log($"An error occurred in GetWeaponPaintsFromDatabase: {ex.Message}");
			}
		}

		internal async Task GetMusicFromDatabase(PlayerInfo player)
		{
			try
			{
				if (!_config.Additional.MusicEnabled || string.IsNullOrEmpty(player.SteamId))
					return;

				await using var connection = await _database.GetConnectionAsync();
				string query = "SELECT `music_id` FROM `wp_player_music` WHERE `steamid` = @steamid";
				ushort? musicData = await connection.QueryFirstOrDefaultAsync<ushort?>(query, new { steamid = player.SteamId });

				if (musicData != null)
				{
					WeaponPaints.g_playersMusic[player.Slot] = musicData.Value;
				}
			}
			catch (Exception ex)
			{
				Utility.Log($"An error occurred in GetMusicFromDatabase: {ex.Message}");
			}
		}

		internal async Task SyncKnifeToDatabase(PlayerInfo player, string knife)
		{
			if (!_config.Additional.KnifeEnabled || player == null || string.IsNullOrEmpty(player.SteamId) || string.IsNullOrEmpty(knife)) return;

			try
			{
				await using var connection = await _database.GetConnectionAsync();
				string query = "INSERT INTO `wp_player_knife` (`steamid`, `knife`) VALUES(@steamid, @newKnife) ON DUPLICATE KEY UPDATE `knife` = @newKnife";
				await connection.ExecuteAsync(query, new { steamid = player.SteamId, newKnife = knife });
			}
			catch (Exception e)
			{
				Utility.Log($"Error syncing knife to database: {e.Message}");
			}
		}

		internal async Task SyncGloveToDatabase(PlayerInfo player, int defindex)
		{
			if (!_config.Additional.GloveEnabled || player == null || string.IsNullOrEmpty(player.SteamId)) return;

			try
			{
				await using var connection = await _database.GetConnectionAsync();
				string query = "INSERT INTO `wp_player_gloves` (`steamid`, `weapon_defindex`) VALUES(@steamid, @weapon_defindex) ON DUPLICATE KEY UPDATE `weapon_defindex` = @weapon_defindex";
				await connection.ExecuteAsync(query, new { steamid = player.SteamId, weapon_defindex = defindex });
			}
			catch (Exception e)
			{
				Utility.Log($"Error syncing glove to database: {e.Message}");
			}
		}

		internal async Task SyncAgentToDatabase(PlayerInfo player)
		{
			if (!_config.Additional.AgentEnabled || player == null || string.IsNullOrEmpty(player.SteamId)) return;

			try
			{
				await using var connection = await _database.GetConnectionAsync();
				string query = @"
					INSERT INTO `wp_player_agents` (`steamid`, `agent_ct`, `agent_t`)
					VALUES(@steamid, @agent_ct, @agent_t)
					ON DUPLICATE KEY UPDATE
						`agent_ct` = @agent_ct,
						`agent_t` = @agent_t";

				await connection.ExecuteAsync(query, new { steamid = player.SteamId, agent_ct = WeaponPaints.g_playersAgent[player.Slot].CT, agent_t = WeaponPaints.g_playersAgent[player.Slot].T });
			}
			catch (Exception e)
			{
				Utility.Log($"Error syncing agents to database: {e.Message}");
			}
		}

		internal async Task SyncWeaponPaintsToDatabase(PlayerInfo player)
		{
			if (player == null || string.IsNullOrEmpty(player.SteamId) || !WeaponPaints.gPlayerWeaponsInfo.TryGetValue(player.Slot, out var weaponsInfo))
				return;

			try
			{
				await using var connection = await _database.GetConnectionAsync();

				foreach (var weaponInfoPair in weaponsInfo)
				{
					int weaponDefIndex = weaponInfoPair.Key;
					WeaponInfo weaponInfo = weaponInfoPair.Value;

					int paintId = weaponInfo.Paint;
					float wear = weaponInfo.Wear;
					int seed = weaponInfo.Seed;

					string queryCheckExistence = "SELECT COUNT(*) FROM `wp_player_skins` WHERE `steamid` = @steamid AND `weapon_defindex` = @weaponDefIndex";

					int existingRecordCount = await connection.ExecuteScalarAsync<int>(queryCheckExistence, new { steamid = player.SteamId, weaponDefIndex });

					string query;
					object parameters;

					if (existingRecordCount > 0)
					{
						query = "UPDATE `wp_player_skins` SET `weapon_paint_id` = @paintId, `weapon_wear` = @wear, `weapon_seed` = @seed WHERE `steamid` = @steamid AND `weapon_defindex` = @weaponDefIndex";
						parameters = new { steamid = player.SteamId, weaponDefIndex, paintId, wear, seed };
					}
					else
					{
						query = "INSERT INTO `wp_player_skins` (`steamid`, `weapon_defindex`, `weapon_paint_id`, `weapon_wear`, `weapon_seed`) " +
								"VALUES (@steamid, @weaponDefIndex, @paintId, @wear, @seed)";
						parameters = new { steamid = player.SteamId, weaponDefIndex, paintId, wear, seed };
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
			if (!_config.Additional.MusicEnabled || player == null || string.IsNullOrEmpty(player.SteamId)) return;

			try
			{
				await using var connection = await _database.GetConnectionAsync();
				string query = "INSERT INTO `wp_player_music` (`steamid`, `music_id`) VALUES(@steamid, @newMusic) ON DUPLICATE KEY UPDATE `music_id` = @newMusic";
				await connection.ExecuteAsync(query, new { steamid = player.SteamId, newMusic = music });
			}
			catch (Exception e)
			{
				Utility.Log($"Error syncing music kit to database: {e.Message}");
			}
		}
	}
}