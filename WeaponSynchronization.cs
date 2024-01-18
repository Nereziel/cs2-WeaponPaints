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

				using (var connection = new MySqlConnection(_databaseConnectionString))
				{
					await connection.OpenAsync();
					string query = "SELECT `knife` FROM `wp_player_knife` WHERE `steamid` = @steamid";
					string? PlayerKnife = await connection.QueryFirstOrDefaultAsync<string>(query, new { steamid = player.SteamId });

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
				Utility.Log(e.Message);
				return;
			}
		}

		internal async Task GetWeaponPaintsFromDatabase(PlayerInfo player)
		{
			if (!_config.Additional.SkinEnabled) return;
			if (player.SteamId == null || player.Index == 0) return;

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

				using (var connection = new MySqlConnection(_databaseConnectionString))
				{
					await connection.OpenAsync();

					string query = "SELECT * FROM `wp_player_skins` WHERE `steamid` = @steamid";
					IEnumerable<dynamic> PlayerSkins = await connection.QueryAsync<dynamic>(query, new { steamid = player.SteamId });

					if (PlayerSkins != null && PlayerSkins.AsList().Count > 0)
					{
						PlayerSkins.ToList().ForEach(row =>
						{
							int weaponDefIndex = row.weapon_defindex ?? default(int);
							int weaponPaintId = row.weapon_paint_id ?? default(int);
							float weaponWear = row.weapon_wear ?? default(float);
							int weaponSeed = row.weapon_seed ?? default(int);

							WeaponInfo weaponInfo = new WeaponInfo
							{
								Paint = weaponPaintId,
								Seed = weaponSeed,
								Wear = weaponWear
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
				Utility.Log(e.Message);
				return;
			}
		}

		internal async Task SyncKnifeToDatabase(PlayerInfo player, string knife)
		{
			if (!_config.Additional.KnifeEnabled) return;
			try
			{
				if (player.SteamId == null || player.Index == 0) return;

				using var connection = new MySqlConnection(_databaseConnectionString);
				await connection.OpenAsync();
				string query = "INSERT INTO `wp_player_knife` (`steamid`, `knife`) VALUES(@steamid, @newKnife) ON DUPLICATE KEY UPDATE `knife` = @newKnife";
				await connection.ExecuteAsync(query, new { steamid = player.SteamId, newKnife = knife });
				await connection.CloseAsync();
			}
			catch (Exception e)
			{
				Utility.Log(e.Message);
				return;
			}
		}
		internal async Task SyncWeaponPaintsToDatabase(PlayerInfo player)
		{
			if (player == null || player.Index <= 0 || player.SteamId == null) return;

			using var connection = new MySqlConnection(_databaseConnectionString);
			await connection.OpenAsync();

			if (!WeaponPaints.gPlayerWeaponsInfo.ContainsKey(player.Index))
				return;

			foreach (var weaponInfoPair in WeaponPaints.gPlayerWeaponsInfo[player.Index])
			{
				int weaponDefIndex = weaponInfoPair.Key;
				WeaponInfo weaponInfo = weaponInfoPair.Value;

				int paintId = weaponInfo.Paint;
				float wear = weaponInfo.Wear;
				int seed = weaponInfo.Seed;

				string updateSql = "UPDATE `wp_player_skins` SET `weapon_paint_id` = @paintId, " +
								   "`weapon_wear` = @wear, `weapon_seed` = @seed WHERE `steamid` = @steamid " +
								   "AND `weapon_defindex` = @weaponDefIndex";

				var updateParams = new { paintId, wear, seed, steamid = player.SteamId, weaponDefIndex };
				int rowsAffected = await connection.ExecuteAsync(updateSql, updateParams);

				if (rowsAffected == 0)
				{
					string insertSql = "INSERT INTO `wp_player_skins` (`steamid`, `weapon_defindex`, " +
									   "`weapon_paint_id`, `weapon_wear`, `weapon_seed`) " +
									   "VALUES (@steamid, @weaponDefIndex, @paintId, @wear, @seed)";

					await connection.ExecuteAsync(insertSql, updateParams);
				}
			}
			await connection.CloseAsync();
		}
	}
}