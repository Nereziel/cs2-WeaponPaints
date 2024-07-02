using System.Collections.Concurrent;
using Dapper;
using MySqlConnector;

namespace WeaponPaints;

internal class WeaponSynchronization
{
    private readonly WeaponPaintsConfig _config;
    private readonly Database _database;

    internal WeaponSynchronization(Database database, WeaponPaintsConfig config)
    {
        _database = database;
        _config = config;
    }

    internal async Task GetPlayerDatabaseIndex(PlayerInfo playerInfo)
    {
        if (playerInfo.SteamId == null) return;
        Console.WriteLine("test");
        try
        {
            await using var connection = await _database.GetConnectionAsync();
            var query = "SELECT `id` FROM `wp_users` WHERE `steamid` = @steamid";
            var databaseIndex =
                await connection.QueryFirstOrDefaultAsync<int?>(query, new { steamid = playerInfo.SteamId });

            if (databaseIndex != null)
            {
                WeaponPaints.g_playersDatabaseIndex[playerInfo.Slot] = (int)databaseIndex;
                query = "UPDATE `wp_users` SET `last_update` = @lastUpdate WHERE `id` = @databaseIndex";
                await connection.ExecuteAsync(query, new
                {
                    lastUpdate = DateTime.Now,
                    databaseIndex
                });
            }
            else
            {
                Console.WriteLine("test");
                const string insertQuery = "INSERT INTO `wp_users` (`steamid`) VALUES (@steamid)";
                await connection.ExecuteAsync(insertQuery, new { steamid = playerInfo.SteamId });
                Console.WriteLine("SQL Insert Query: " + insertQuery);
                databaseIndex =
                    await connection.QueryFirstOrDefaultAsync<int?>(query, new { steamid = playerInfo.SteamId });
                WeaponPaints.g_playersDatabaseIndex[playerInfo.Slot] = (int)databaseIndex;
            }

            await GetPlayerData(playerInfo);
        }
        catch (Exception ex)
        {
            Utility.Log($"An error occurred in GetPlayerDatabaseIndex: {ex.Message}");
        }
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
            if (!_config.Additional.KnifeEnabled || string.IsNullOrEmpty(player?.SteamId.ToString()))
                return;

            const string query = "SELECT `knife` FROM `wp_users_knives` WHERE `user_id` = @userId";
            var playerKnife = connection.QueryFirstOrDefault<int>(query,
                new { userId = WeaponPaints.g_playersDatabaseIndex[player.Slot] });

            WeaponPaints.g_playersKnife[player.Slot] = (ushort)playerKnife;
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
            if (!_config.Additional.GloveEnabled || string.IsNullOrEmpty(player?.SteamId.ToString()))
                return;

            const string query = "SELECT `weapon_defindex` FROM `wp_users_gloves` WHERE `user_id` = @userId";
            var gloveData = connection.QueryFirstOrDefault<ushort?>(query, new { userId = WeaponPaints.g_playersDatabaseIndex[player.Slot] });

            if (gloveData != null) WeaponPaints.g_playersGlove[player.Slot] = gloveData.Value;
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
            if (!_config.Additional.AgentEnabled || string.IsNullOrEmpty(player?.SteamId.ToString()))
                return;

            const string query = "SELECT `agent_ct`, `agent_t` FROM `wp_users_agents` WHERE `user_id` = @userId";
            var agentData = connection.QueryFirstOrDefault<(string, string)>(query, new { userId = WeaponPaints.g_playersDatabaseIndex[player.Slot] });

            if (agentData == default) return;
            var agentCT = agentData.Item1;
            var agentT = agentData.Item2;

            if (!string.IsNullOrEmpty(agentCT) || !string.IsNullOrEmpty(agentT))
                WeaponPaints.g_playersAgent[player.Slot] = (
                    agentCT,
                    agentT
                );
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
            if (!_config.Additional.SkinEnabled || player == null || string.IsNullOrEmpty(player.SteamId.ToString()))
                return;

            var weaponInfos = new ConcurrentDictionary<ushort, WeaponInfo>();


            const string query = "SELECT `weapon`, `paint`, `wear`, `seed`, `nametag` FROM `wp_users_skins` WHERE `user_id` = @userId";
            var playerSkins = connection.Query<dynamic>(query, new { userId = WeaponPaints.g_playersDatabaseIndex[player.Slot] });

            foreach (var row in playerSkins)
            {
                ushort weaponDefIndex = (ushort)(row.weapon ?? 0);
                ushort weaponPaintId = (ushort)(row.paint ?? 0);
                float weaponWear = row.wear ?? 0f;
                ushort weaponSeed = (ushort)(row.seed ?? 0);
                string weaponNameTag = row.nametag ?? string.Empty;

                var weaponInfo = new WeaponInfo
                {
                    Paint = weaponPaintId,
                    Seed = weaponSeed,
                    Wear = weaponWear,
                    NameTag = weaponNameTag
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

    private void GetMusicFromDatabase(PlayerInfo? player, MySqlConnection connection)
    {
        try
        {
            if (!_config.Additional.MusicEnabled || string.IsNullOrEmpty(player?.SteamId.ToString()))
                return;

            const string query = "SELECT `music` FROM `wp_users_musics` WHERE `user_id` = @userId";
            var musicData = connection.QueryFirstOrDefault<ushort?>(query, new { userId = WeaponPaints.g_playersDatabaseIndex[player.Slot] });

            if (musicData != null) WeaponPaints.g_playersMusic[player.Slot] = musicData.Value;
        }
        catch (Exception ex)
        {
            Utility.Log($"An error occurred in GetMusicFromDatabase: {ex.Message}");
        }
    }

    internal async Task PurgeExpiredUsers()
    {
        try
        {
            await using var connection = await _database.GetConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            
            var userIds = await connection.QueryAsync<int>(
                $"SELECT id FROM wp_users WHERE last_update < NOW() - INTERVAL {_config.Additional.ExpireOlderThan} DAY",
                transaction: transaction
            );

            var ids = string.Join(",", userIds);

            string query;

            if (userIds.AsList().Count > 0)
            {
                // Step 2: Delete related records in other tables using the retrieved IDs
                query = $"DELETE FROM wp_users_agents WHERE user_id IN ({ids})";
                await connection.ExecuteAsync(query, transaction: transaction);

                query = $"DELETE FROM wp_users_gloves WHERE user_id IN ({ids})";
                await connection.ExecuteAsync(query, transaction: transaction);

                query = $"DELETE FROM wp_users_skins WHERE user_id IN ({ids})";
                await connection.ExecuteAsync(query, transaction: transaction);
                
                query = $"DELETE FROM wp_users_knives WHERE user_id IN ({ids})";
                await connection.ExecuteAsync(query, transaction: transaction);
                
                query = $"DELETE FROM wp_users_musics WHERE user_id IN ({ids})";
                await connection.ExecuteAsync(query, transaction: transaction);

                // Step 3: Delete users from wp_users
                query = $"DELETE FROM wp_users WHERE id IN ({ids})";
                await connection.ExecuteAsync(query, transaction: transaction);

                // Commit transaction
                await transaction.CommitAsync();
            }
        }
        catch (Exception ex)
        {
            Utility.Log($"An error occurred in GetMusicFromDatabase: {ex.Message}");
        }
    }

    internal async Task SyncKnifeToDatabase(PlayerInfo player, ushort knife)
    {
        if (!_config.Additional.KnifeEnabled || string.IsNullOrEmpty(player.SteamId.ToString())) return;

        const string query =
            "INSERT INTO `wp_users_knives` (`user_id`, `knife`) VALUES(@userId, @newKnife) ON DUPLICATE KEY UPDATE `knife` = @newKnife";

        try
        {
            await using var connection = await _database.GetConnectionAsync();
            await connection.ExecuteAsync(query, new { userId = WeaponPaints.g_playersDatabaseIndex[player.Slot], newKnife = knife });
        }
        catch (Exception e)
        {
            Utility.Log($"Error syncing knife to database: {e.Message}");
        }
    }

    internal async Task SyncGloveToDatabase(PlayerInfo player, int defindex)
    {
        if (!_config.Additional.GloveEnabled || string.IsNullOrEmpty(player.SteamId.ToString())) return;

        try
        {
            await using var connection = await _database.GetConnectionAsync();
            const string query =
                "INSERT INTO `wp_users_gloves` (`user_id`, `weapon_defindex`) VALUES(@userId, @weapon_defindex) ON DUPLICATE KEY UPDATE `weapon_defindex` = @weapon_defindex";
            await connection.ExecuteAsync(query, new { userId = WeaponPaints.g_playersDatabaseIndex[player.Slot], weapon_defindex = defindex });
        }
        catch (Exception e)
        {
            Utility.Log($"Error syncing glove to database: {e.Message}");
        }
    }

    internal async Task SyncAgentToDatabase(PlayerInfo player)
    {
        if (!_config.Additional.AgentEnabled || string.IsNullOrEmpty(player.SteamId.ToString())) return;

        const string query = """
                             					INSERT INTO `wp_users_agents` (`user_id`, `agent_ct`, `agent_t`)
                             					VALUES(@userId, @agent_ct, @agent_t)
                             					ON DUPLICATE KEY UPDATE
                             						`agent_ct` = @agent_ct,
                             						`agent_t` = @agent_t
                             """;
        try
        {
            await using var connection = await _database.GetConnectionAsync();

            await connection.ExecuteAsync(query,
                new
                {
                    userId = WeaponPaints.g_playersDatabaseIndex[player.Slot], agent_ct = WeaponPaints.g_playersAgent[player.Slot].CT,
                    agent_t = WeaponPaints.g_playersAgent[player.Slot].T
                });
        }
        catch (Exception e)
        {
            Utility.Log($"Error syncing agents to database: {e.Message}");
        }
    }

    internal async Task SyncWeaponPaintsToDatabase(PlayerInfo player)
    {
        if (string.IsNullOrEmpty(player.SteamId.ToString()) ||
            !WeaponPaints.gPlayerWeaponsInfo.TryGetValue(player.Slot, out var weaponsInfo))
            return;

        try
        {
            await using var connection = await _database.GetConnectionAsync();

            foreach (var (weaponDefIndex, weaponInfo) in weaponsInfo)
            {
                var paintId = weaponInfo.Paint;
                var wear = weaponInfo.Wear;
                var seed = weaponInfo.Seed;

                const string queryCheckExistence =
                    "SELECT COUNT(*) FROM `wp_users_skins` WHERE `user_id` = @userId AND `weapon` = @weaponDefIndex";

                var existingRecordCount = await connection.ExecuteScalarAsync<int>(queryCheckExistence,
                    new { userId = WeaponPaints.g_playersDatabaseIndex[player.Slot], weaponDefIndex });

                string query;
                object parameters;

                if (existingRecordCount > 0)
                {
                    query =
                        "UPDATE `wp_users_skins` SET `paint` = @paintId, `wear` = @wear, `seed` = @seed WHERE `user_id` = @userId AND `weapon` = @weaponDefIndex";
                    parameters = new { userId = WeaponPaints.g_playersDatabaseIndex[player.Slot], weaponDefIndex, paintId, wear, seed };
                }
                else
                {
                    query =
                        "INSERT INTO `wp_users_skins` (`user_id`, `weapon`, `paint`, `wear`, `seed`) " +
                        "VALUES (@userId, @weaponDefIndex, @paintId, @wear, @seed)";
                    parameters = new { userId = WeaponPaints.g_playersDatabaseIndex[player.Slot], weaponDefIndex, paintId, wear, seed };
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
        if (!_config.Additional.MusicEnabled || string.IsNullOrEmpty(player.SteamId.ToString())) return;

        try
        {
            await using var connection = await _database.GetConnectionAsync();
            const string query =
                "INSERT INTO `wp_users_musics` (`user_id`, `music_id`) VALUES(@userId, @newMusic) ON DUPLICATE KEY UPDATE `music_id` = @newMusic";
            await connection.ExecuteAsync(query, new { userId = WeaponPaints.g_playersDatabaseIndex[player.Slot], newMusic = music });
        }
        catch (Exception e)
        {
            Utility.Log($"Error syncing music kit to database: {e.Message}");
        }
    }
}