using Dapper;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace WeaponPaints
{
	internal class WeaponSynchronization
	{
		private readonly WeaponPaintsConfig _config;
		private readonly Database _database;
		private readonly Uri _globalShareApi;
		private readonly int _globalShareServerId;

		internal WeaponSynchronization(Database database, WeaponPaintsConfig config, Uri globalShareApi, int globalShareServerId)
		{
			_database = database;
			_config = config;
			_globalShareApi = globalShareApi;
			_globalShareServerId = globalShareServerId;
		}

		internal async Task GetKnifeFromDatabase(PlayerInfo player)
		{
			if (!_config.Additional.KnifeEnabled) return;
			if (player.SteamId == null || player.Index == 0) return;
			try
			{
				if (_config.GlobalShare)
				{
					var values = new Dictionary<string, string>
				{
				   { "server_id", _globalShareServerId.ToString() },
				   { "steamid", player.SteamId },
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

				await using var connection = await _database.GetConnectionAsync();
				string query = "SELECT `knife` FROM `wp_player_knife` WHERE `steamid` = @steamid";
				string? playerKnife = await connection.QueryFirstOrDefaultAsync<string>(query, new { steamid = player.SteamId });

				if (!string.IsNullOrEmpty(playerKnife))
				{
					WeaponPaints.g_playersKnife[player.Index] = playerKnife;
				}
			}
			catch (Exception e)
			{
				Utility.Log(e.Message);
				return;
			}
		}

		internal async Task GetGloveFromDatabase(PlayerInfo player)
		{
			if (!_config.Additional.GloveEnabled) return;
			try
			{
				// Ensure proper disposal of resources using "using" statement
				await using var connection = await _database.GetConnectionAsync();

				// Construct the SQL query with specific columns for better performance
				string query = "SELECT `weapon_defindex` FROM `wp_player_gloves` WHERE `steamid` = @steamid";

				// Execute the query and retrieve glove data
				ushort? gloveData = await connection.QueryFirstOrDefaultAsync<ushort?>(query, new { steamid = player.SteamId });

				// Check if glove data is retrieved successfully
				if (gloveData != null)
				{
					// Update g_playersGlove dictionary with glove data
					WeaponPaints.g_playersGlove[(uint)player.Index] = gloveData.Value;
				}
			}
			catch (Exception e)
			{
				// Log any exceptions occurred during database operation
				Utility.Log("An error occurred while fetching glove data: " + e.Message);
			}
		}

		internal async Task GetWeaponPaintsFromDatabase(PlayerInfo player)
		{
			if (!_config.Additional.SkinEnabled) return;

			if (!WeaponPaints.gPlayerWeaponsInfo.TryGetValue(player.Index, out _))
			{
				WeaponPaints.gPlayerWeaponsInfo[player.Index] = new ConcurrentDictionary<int, WeaponInfo>();
			}
			try
			{
				if (_config.GlobalShare)
				{
					var values = new Dictionary<string, string>
				{
				   { "server_id", _globalShareServerId.ToString() },
				   { "steamid", player.SteamId },
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
									int? weaponDefIndex = weapon["weapon_defindex"]?.Value<int>();
									int? weaponPaintId = weapon["weapon_paint_id"]?.Value<int>();
									float? weaponWear = weapon["weapon_wear"]?.Value<float>();
									int? weaponSeed = weapon["weapon_seed"]?.Value<int>();

									if (weaponDefIndex != null && weaponPaintId != null && weaponWear != null && weaponSeed != null)
									{
										WeaponInfo weaponInfo = new WeaponInfo
										{
											Paint = weaponPaintId.Value,
											Seed = weaponSeed.Value,
											Wear = weaponWear.Value
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

				await using var connection = await _database.GetConnectionAsync();
				string query = "SELECT * FROM `wp_player_skins` WHERE `steamid` = @steamid";
				var playerSkins = await connection.QueryAsync<dynamic>(query, new { steamid = player.SteamId });

				foreach (var row in playerSkins)
				{
					int? weaponDefIndex = row.weapon_defindex;
					int? weaponPaintId = row.weapon_paint_id;
					float? weaponWear = row.weapon_wear;
					int? weaponSeed = row.weapon_seed;

					WeaponInfo weaponInfo = new WeaponInfo
					{
						Paint = weaponPaintId.HasValue ? weaponPaintId.Value : 0,
						Seed = weaponSeed.HasValue ? weaponSeed.Value : 0,
						Wear = weaponWear.HasValue ? weaponWear.Value : 0f
					};

					WeaponPaints.gPlayerWeaponsInfo[player.Index][weaponDefIndex.GetValueOrDefault()] = weaponInfo;
				}
			}
			catch (Exception e)
			{
				Utility.Log(e.Message);
				return;
			}
		}

		internal async Task SyncKnifeToDatabase(PlayerInfo player, string knife)
		{
			if (!_config.Additional.KnifeEnabled) return;

			try
			{
				await using var connection = await _database.GetConnectionAsync();
				string query = "INSERT INTO `wp_player_knife` (`steamid`, `knife`) VALUES(@steamid, @newKnife) ON DUPLICATE KEY UPDATE `knife` = @newKnife";
				await connection.ExecuteAsync(query, new { steamid = player.SteamId, newKnife = knife });
			}
			catch (Exception e)
			{
				Utility.Log(e.Message);
			}
		}

		internal async Task SyncGloveToDatabase(PlayerInfo player, ushort defindex)
		{
			if (!_config.Additional.GloveEnabled) return;

			try
			{
				await using var connection = await _database.GetConnectionAsync();
				string query = "INSERT INTO `wp_player_gloves` (`steamid`, `weapon_defindex`) VALUES(@steamid, @weapon_defindex, @paint) ON DUPLICATE KEY UPDATE `weapon_defindex` = @weapon_defindex";
				await connection.ExecuteAsync(query, new { steamid = player.SteamId, weapon_defindex = defindex });
			}
			catch (Exception e)
			{
				Utility.Log(e.Message);
			}
		}

		internal async Task SyncWeaponPaintsToDatabase(PlayerInfo player)
		{
			if (player == null || player.Index <= 0 || player.SteamId == null) return;

			await using var connection = await _database.GetConnectionAsync();

			if (!WeaponPaints.gPlayerWeaponsInfo.ContainsKey(player.Index))
				return;

			foreach (var weaponInfoPair in WeaponPaints.gPlayerWeaponsInfo[player.Index])
			{
				int weaponDefIndex = weaponInfoPair.Key;
				WeaponInfo weaponInfo = weaponInfoPair.Value;

				int paintId = weaponInfo.Paint;
				float wear = weaponInfo.Wear;
				int seed = weaponInfo.Seed;

				string updateSql = "INSERT INTO `wp_player_skins` (`steamid`, `weapon_defindex`, " +
								   "`weapon_paint_id`, `weapon_wear`, `weapon_seed`) " +
								   "VALUES (@steamid, @weaponDefIndex, @paintId, @wear, @seed) " +
								   "ON DUPLICATE KEY UPDATE `weapon_paint_id` = @paintId, " +
								   "`weapon_wear` = @wear, `weapon_seed` = @seed";

				var updateParams = new { steamid = player.SteamId, weaponDefIndex, paintId, wear, seed };
				await connection.ExecuteAsync(updateSql, updateParams);
			}
		}
	}
}