using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace WeaponPaints
{
	public class Database(string dbConnectionString)
	{
		private readonly string _dbConnectionString = dbConnectionString;

		public async Task<MySqlConnection> GetConnectionAsync()
		{
			try
			{
				var connection = new MySqlConnection(_dbConnectionString);
				await connection.OpenAsync();
				return connection;
			}
			catch (Exception ex)
			{
				WeaponPaints.Instance.Logger.LogError($"Unable to connect to database: {ex.Message}");
				throw;
			}
		}
	}
}