using Microsoft.Extensions.Logging;

namespace WeaponPaints
{
    public class Database
    {
        private readonly Func<IDatabaseConnection> _connectionFactory;

        public Database(Func<IDatabaseConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IDatabaseConnection> GetConnectionAsync()
        {
            var connection = _connectionFactory();
            await connection.OpenAsync();
            return connection;
        }
    }
}