using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace WeaponPaints
{
	internal class WeaponSynchronization
	{
		private readonly WeaponPaintsConfig _config;
		private readonly string _databaseConnectionString;
		private readonly Uri _globalShareApi;
		private readonly int _globalShareServerId;

		internal WeaponSynchronization(string databaseConnectionString, WeaponPaintsConfig config, Uri globalShareApi, int globalShareServerId)
		{
			_databaseConnectionString = databaseConnectionString;
			_config = config;
			_globalShareApi = globalShareApi;
			_globalShareServerId = globalShareServerId;
		}
		internal async Task GetPlayerDatabaseIndex(PlayerInfo player)
		{
            if (player.SteamId == null || player.Index == 0) return;

            try
			{
				using (var connection = new MySqlConnection(_databaseConnectionString))
				{
					await connection.OpenAsync();

					string query = "SELECT `id` FROM `wp_users` WHERE `steamid` = @steamid";
					int? databaseIndex = await connection.QueryFirstOrDefaultAsync<int?>(query, new { steamid = player.SteamId });
                    if (databaseIndex != null)
                    {
                        WeaponPaints.g_playersDatabaseIndex[player.Index] = (int)databaseIndex;
                    }
                    else
                    {
                        string insertQuery = "INSERT INTO `wp_users` (`steamid`) VALUES (@steamid)";
                        await connection.ExecuteAsync(insertQuery, new { steamid = player.SteamId });
                        Console.WriteLine("SQL Insert Query: " + insertQuery);
                        databaseIndex = await connection.QueryFirstOrDefaultAsync<int?>(query, new { steamid = player.SteamId });
                        WeaponPaints.g_playersDatabaseIndex[(int)player.Index] = (int)databaseIndex;
                    }
					await connection.CloseAsync();

                    if (databaseIndex != null)
                    {
                        if (_config.AdditionalSetting.SkinEnabled)
                            await GetWeaponPaintsFromDatabase(player);
                        if (_config.AdditionalSetting.KnifeEnabled)
                            await GetKnifeFromDatabase(player);
                        if (_config.AdditionalSetting.MusicKitEnabled)
                            await GetMusicKitFromDatabase(player);
                    }
                }

            }
			catch (Exception e)
			{
                Utility.Log("GetPlayerDatabaseIndex: " + e.Message);
                return;
            }
        }
        internal async Task GetKnifeFromDatabase(PlayerInfo player)
		{
			if (!_config.AdditionalSetting.KnifeEnabled) return;
			if (player.SteamId == null || player.Index == 0) return;
			try
			{
				if (_config.GlobalShare)
				{
					var values = new Dictionary<string, string>
					{
					   { "server_id", _globalShareServerId.ToString() },
					   { "steamid", player.SteamId.ToString()! },
					   { "knife", "1" }
					};

					UriBuilder builder = new UriBuilder(_globalShareApi);
					builder.Query = string.Join("&", values.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

					using (var httpClient = new HttpClient())
					{
						httpClient.BaseAddress = _globalShareApi;
						var formContent = new FormUrlEncodedContent(values);
						HttpResponseMessage response = await httpClient.GetAsync(builder.Uri);

						if (response.IsSuccessStatusCode)
						{
							string result = await response.Content.ReadAsStringAsync();
							if (!string.IsNullOrEmpty(result))
							{
								WeaponPaints.g_playersKnife[player.Index] = result;
							}
							else
							{
								return;
							}
						}
						else
						{
							return;
						}
					}
					return;
				}

                if (!WeaponPaints.g_playersDatabaseIndex.TryGetValue(player.Index, out _))
                {
                    return;
                }

                using (var connection = new MySqlConnection(_databaseConnectionString))
				{
					await connection.OpenAsync();
					string query = "SELECT `knife` FROM `wp_users_knife` WHERE `user_id` = @userId";
					string? PlayerKnife = await connection.QueryFirstOrDefaultAsync<string>(query, new { userId = WeaponPaints.g_playersDatabaseIndex[player.Index] });
                    if (PlayerKnife != null)
					{
						WeaponPaints.g_playersKnife[player.Index] = PlayerKnife;
					}
					else
					{
						return;
					}
					await connection.CloseAsync();
				}
			}
			catch (Exception e)
			{
				Utility.Log("GetKnifeFromDatabase: " + e.Message);
				return;
			}
		}
        internal async Task GetMusicKitFromDatabase(PlayerInfo player)
        {
            if (!_config.AdditionalSetting.MusicKitEnabled) return;
            if (player.SteamId == null || player.Index == 0) return;
            if (!WeaponPaints.g_playersDatabaseIndex.TryGetValue(player.Index, out _))
            {
                return;
            }
            try
            {
                using (var connection = new MySqlConnection(_databaseConnectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT `music` FROM `wp_users_music` WHERE `user_id` = @userId";
                    int? PlayerMusitKit = await connection.QueryFirstOrDefaultAsync<int?>(query, new { userId = WeaponPaints.g_playersDatabaseIndex[player.Index] });
                    if (PlayerMusitKit != null)
                    {
                        WeaponPaints.g_playersMusicKit[player.Index] = PlayerMusitKit;
                    }
                    else
                    {
                        return;
                    }
                    await connection.CloseAsync();
                }
            }
            catch (Exception e)
            {
                Utility.Log("GetMusicKitFromDatabase: " + e.Message);
                return;
            }
        }

        internal async Task GetWeaponPaintsFromDatabase(PlayerInfo player)
		{
			if (!_config.AdditionalSetting.SkinEnabled) return;
			if (player.SteamId == null || player.Index == 0) return;

            if (!WeaponPaints.gPlayerWeaponsInfo.TryGetValue(player.Index, out _))
            {
                WeaponPaints.gPlayerWeaponsInfo[player.Index] = new ConcurrentDictionary<ushort, WeaponInfo>();
            }

            try
			{
				if (_config.GlobalShare)
				{
					var values = new Dictionary<string, string>
					{
					   { "server_id", _globalShareServerId.ToString() },
					   { "steamid", player.SteamId.ToString()! },
					   { "skins", "1" }
					};
					UriBuilder builder = new UriBuilder(_globalShareApi);
					builder.Query = string.Join("&", values.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

					using (var httpClient = new HttpClient())
					{
						httpClient.BaseAddress = _globalShareApi;
						var formContent = new FormUrlEncodedContent(values);
						HttpResponseMessage response = await httpClient.GetAsync(builder.Uri);

						if (response.IsSuccessStatusCode)
						{
							string responseBody = await response.Content.ReadAsStringAsync();
							JArray jsonArray = JArray.Parse(responseBody);
							if (jsonArray != null && jsonArray.Count > 0)
							{
								foreach (var weapon in jsonArray)
								{
                                    ushort? weaponDefIndex = weapon["weapon_defindex"]?.Value<ushort>();
                                    ushort? weaponPaintId = weapon["weapon_paint_id"]?.Value<ushort>();
									float? weaponWear = weapon["weapon_wear"]?.Value<float>();
									ushort? weaponSeed = weapon["weapon_seed"]?.Value<ushort>();

                                    if (weaponDefIndex != null && weaponPaintId != null && weaponWear != null && weaponSeed != null)
									{
										WeaponInfo weaponInfo = new WeaponInfo
										{
											Paint = weaponPaintId.Value,
											Seed = weaponSeed.Value,
											Wear = weaponWear.Value,
                                            NameTag = null
                                        };
										WeaponPaints.gPlayerWeaponsInfo[player.Index][weaponDefIndex.Value] = weaponInfo;
									}
								}
							}
							return;
						}
						else
						{
							return;
						}
					}
				}

                if (!WeaponPaints.g_playersDatabaseIndex.TryGetValue(player.Index, out _))
                {
                    return;
                }

                using (var connection = new MySqlConnection(_databaseConnectionString))
				{
					await connection.OpenAsync();
                    string query = "SELECT `weapon`, `paint`, `wear`, `seed`, `nametag` FROM `wp_users_items` WHERE `user_id` = @userId";
                    IEnumerable<dynamic> PlayerSkins = await connection.QueryAsync<dynamic>(query, new { userId = WeaponPaints.g_playersDatabaseIndex[player.Index] });
                    if (PlayerSkins != null && PlayerSkins.Any())
					{
                        PlayerSkins.ToList().ForEach(row =>
						{
                            ushort weaponDefIndex = row.weapon ?? default(ushort);
                            ushort weaponPaintId = row.paint ?? default(ushort);
                            float weaponWear = row.wear ?? default(float);
                            ushort weaponSeed = row.seed ?? default(ushort);
                            string weaponNameTag = row.nametag;

                            WeaponInfo weaponInfo = new WeaponInfo
							{
								Paint = weaponPaintId,
								Seed = weaponSeed,
								Wear = weaponWear,
                                NameTag = weaponNameTag
                            };
							WeaponPaints.gPlayerWeaponsInfo[player.Index][weaponDefIndex] = weaponInfo;
						});
					}
					else
					{
						return;
					}
					await connection.CloseAsync();
				}
			}
			catch (Exception e)
			{
				Utility.Log("GetWeaponPaintsFromDatabase: " + e.Message);
				return;
			}
		}

		internal async Task SyncKnifeToDatabase(PlayerInfo player, string knife)
		{
			if (!_config.AdditionalSetting.KnifeEnabled) return;
            if(player == null || player.Index <= 0) return;
            try
			{
                if (!WeaponPaints.g_playersDatabaseIndex.TryGetValue(player.Index, out _))
                    return;

                using var connection = new MySqlConnection(_databaseConnectionString);
				await connection.OpenAsync();
				string query = "INSERT INTO `wp_users_knife` (`user_id`, `knife`) VALUES(@userId, @newKnife) ON DUPLICATE KEY UPDATE `knife` = @newKnife";
                await connection.ExecuteAsync(query, new { userId = WeaponPaints.g_playersDatabaseIndex[player.Index], newKnife = knife });
				await connection.CloseAsync();
			}
			catch (Exception e)
			{
				Utility.Log(e.Message);
				return;
			}
		}
		internal async Task SyncWeaponPaintToDatabase(PlayerInfo player, ushort weaponDefIndex)
		{
            if (!_config.AdditionalSetting.SkinEnabled) return;
            if (player == null || player.Index <= 0) return;

            if (!WeaponPaints.g_playersDatabaseIndex.TryGetValue(player.Index, out var playerDatabaseIndex))
                return;
                
            if (!WeaponPaints.gPlayerWeaponsInfo.TryGetValue(player.Index, out var playerSavedWeapons))
                return;
                
			if (!playerSavedWeapons.TryGetValue(weaponDefIndex, out var weaponInfo))
                return;

            using var connection = new MySqlConnection(_databaseConnectionString);
            string querySql = @"
			INSERT INTO `wp_users_items` 
				(`user_id`, `weapon`, `paint`, `wear`, `seed`) 
			VALUES 
				(@userId, @weaponDefIndex, @paintId, @wear, @seed) 
			ON DUPLICATE KEY UPDATE 
				paint = @paintId,
				wear = @wear,
				seed = @seed";
            var queryParams = new { weaponDefIndex, userId = playerDatabaseIndex, paintId = weaponInfo.Paint, wear = weaponInfo.Wear, seed = weaponInfo.Seed };
            await connection.ExecuteAsync(querySql, queryParams);
            await connection.CloseAsync();
            
		}
	}
}