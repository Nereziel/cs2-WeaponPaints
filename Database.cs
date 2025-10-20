using Microsoft.Extensions.Logging;

namespace WeaponPaints
{
	public class Database
	{
		private readonly IDatabaseConnection _connection;

		public Database(IDatabaseConnection connection)
		{
			_connection = connection;
		}

		public async Task<IDatabaseConnection> GetConnectionAsync()
		{
			await _connection.OpenAsync();
			return _connection;
		}
	}
}