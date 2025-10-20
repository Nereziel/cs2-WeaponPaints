using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Data;

namespace WeaponPaints
{
	public class MySQLConnection : IDatabaseConnection
	{
		private readonly MySqlConnection _connection;
		private readonly ILogger _logger;

		public MySQLConnection(string connectionString, ILogger logger)
		{
			_connection = new MySqlConnection(connectionString);
			_logger = logger;
		}

		public async Task OpenAsync()
		{
			try
			{
				await _connection.OpenAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError($"Unable to connect to MySQL database: {ex.Message}");
				throw;
			}
		}

		public async Task CloseAsync()
		{
			await _connection.CloseAsync();
		}

		public async Task<IDbTransaction> BeginTransactionAsync()
		{
			return await _connection.BeginTransactionAsync();
		}

		public Task CommitTransactionAsync(IDbTransaction transaction)
		{
			transaction.Commit();
			return Task.CompletedTask;
		}

		public Task RollbackTransactionAsync(IDbTransaction transaction)
		{
			transaction.Rollback();
			return Task.CompletedTask;
		}

		public IDbConnection GetConnection()
		{
			return _connection;
		}

		public void Dispose()
		{
			_connection?.Dispose();
		}

		public async ValueTask DisposeAsync()
		{
			await _connection.DisposeAsync();
		}
	}
}
