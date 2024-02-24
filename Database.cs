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
            var connection = new MySqlConnection(_dbConnectionString);
            await connection.OpenAsync();
            return connection;
        }
	}
}