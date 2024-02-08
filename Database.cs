using MySqlConnector;

namespace WeaponPaints
{
	public class Database
	{
		private readonly string _dbConnectionString;

		public Database(string dbConnectionString)
		{
			_dbConnectionString = dbConnectionString;
		}

		public async Task<MySqlConnection> GetConnectionAsync()
		{
			try
			{
				var connection = new MySqlConnection(_dbConnectionString);
				await connection.OpenAsync();
				return connection;
			}
			catch (Exception)
			{
				throw;
			}
		}
	}
}