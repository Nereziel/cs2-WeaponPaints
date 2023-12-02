using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using MySqlConnector;
using Newtonsoft.Json.Linq;

namespace WeaponPaints
{
	internal class WeaponSynchronization
	{
		private readonly string _databaseConnectionString;
		private readonly WeaponPaintsConfig _config;

		private readonly Uri _globalShareApi;
		private readonly int _globalShareServerId;


		internal WeaponSynchronization(string databaseConnectionString, WeaponPaintsConfig config, Uri globalShareApi, int globalShareServerId)
		{
			_databaseConnectionString = databaseConnectionString;
			_config = config;
			_globalShareApi = globalShareApi;
			_globalShareServerId = globalShareServerId;
		}

		internal async Task GetKnifeFromDatabase(int playerIndex)
		{
			if (!_config.Additional.KnifeEnabled) return;
			try
			{
				CCSPlayerController player = Utilities.GetPlayerFromIndex(playerIndex);
				if (!Utility.IsPlayerValid(player)) return;
				if (player.AuthorizedSteamID == null) return;
				string steamId = player.AuthorizedSteamID.SteamId64.ToString();

				if (_config.GlobalShare)
				{
					var values = new Dictionary<string, string>
				{
				   { "server_id", _globalShareServerId.ToString() },
				   { "steamid", steamId },
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
								WeaponPaints.g_playersKnife[playerIndex] = result;
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
					string? PlayerKnife = await connection.QueryFirstOrDefaultAsync<string>(query, new { steamid = steamId });
					if (PlayerKnife != null)
					{
						WeaponPaints.g_playersKnife[playerIndex] = PlayerKnife;
					}
					else
					{
						return;
					}
					await connection.CloseAsync();
				}
				//Log($"{player.PlayerName} has this knife -> {g_playersKnife[playerIndex]}");
			}
			catch (Exception e)
			{
				Utility.Log(e.Message);
				return;
			}
		}

		internal async Task SyncKnifeToDatabase(int playerIndex, string knife)
		{
			if (!_config.Additional.KnifeEnabled) return;
			try
			{
				CCSPlayerController player = Utilities.GetPlayerFromIndex(playerIndex);
				if (player == null || !player.IsValid) return;
				if (player.AuthorizedSteamID == null) return;
				string steamId = player.AuthorizedSteamID.SteamId64.ToString();

				using var connection = new MySqlConnection(_databaseConnectionString);
				await connection.OpenAsync();
				string query = "INSERT INTO `wp_player_knife` (`steamid`, `knife`) VALUES(@steamid, @newKnife) ON DUPLICATE KEY UPDATE `knife` = @newKnife";
				await connection.ExecuteAsync(query, new { steamid = steamId, newKnife = knife });
				await connection.CloseAsync();
			}
			catch (Exception e)
			{
				Utility.Log(e.Message);
				return;
			}
		}

		internal async Task GetWeaponPaintsFromDatabase(int playerIndex)
		{
			if (!_config.Additional.SkinEnabled) return;

			CCSPlayerController player = Utilities.GetPlayerFromIndex(playerIndex);
			if (!Utility.IsPlayerValid(player)) return;

			if (player.AuthorizedSteamID == null) return;

			string steamId = player.AuthorizedSteamID.SteamId64.ToString();

			if (!WeaponPaints.gPlayerWeaponsInfo.TryGetValue(playerIndex, out _))
			{
				WeaponPaints.gPlayerWeaponsInfo[playerIndex] = new Dictionary<int, WeaponInfo>();
			}
			try
			{
				if (_config.GlobalShare)
				{
					var values = new Dictionary<string, string>
				{
				   { "server_id", _globalShareServerId.ToString() },
				   { "steamid", steamId },
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
											Paint = weaponPaintId.Value, // Example paint value
											Seed = weaponSeed.Value, // Example seed value
											Wear = weaponWear.Value // Example wear value
										};
										WeaponPaints.gPlayerWeaponsInfo[playerIndex][weaponDefIndex.Value] = weaponInfo;
										/*
										gPlayerWeaponPaints[playerIndex][weaponDefIndex.Value] = weaponPaintId.Value;
										gPlayerWeaponWear[playerIndex][weaponDefIndex.Value] = weaponWear.Value;
										gPlayerWeaponSeed[playerIndex][weaponDefIndex.Value] = weaponSeed.Value;
										*/
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
					IEnumerable<dynamic> PlayerSkins = await connection.QueryAsync<dynamic>(query, new { steamid = steamId });

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
							WeaponPaints.gPlayerWeaponsInfo[playerIndex][weaponDefIndex] = weaponInfo;
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

		internal async Task SyncWeaponPaintsToDatabase(CCSPlayerController? player)
		{
			if (player == null || !Utility.IsPlayerValid(player)) return;

			int playerIndex = (int)player.Index;
			if (player.AuthorizedSteamID == null) return;
			string steamId = player.AuthorizedSteamID.SteamId64.ToString();

			using var connection = new MySqlConnection(_databaseConnectionString);
			await connection.OpenAsync();

			if (!WeaponPaints.gPlayerWeaponsInfo.ContainsKey(playerIndex))
				return;

			foreach (var weaponInfoPair in WeaponPaints.gPlayerWeaponsInfo[playerIndex])
			{
				int weaponDefIndex = weaponInfoPair.Key;
				WeaponInfo weaponInfo = weaponInfoPair.Value;

				int paintId = weaponInfo.Paint;
				float wear = weaponInfo.Wear;
				int seed = weaponInfo.Seed;

				string updateSql = "UPDATE `wp_player_skins` SET `weapon_paint_id` = @paintId, " +
								   "`weapon_wear` = @wear, `weapon_seed` = @seed WHERE `steamid` = @steamid " +
								   "AND `weapon_defindex` = @weaponDefIndex";

				var updateParams = new { paintId, wear, seed, steamid = steamId, weaponDefIndex };
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
