using System.Data;

namespace WeaponPaints
{
	public interface IDatabaseConnection : IAsyncDisposable, IDisposable
	{
		Task OpenAsync();
		Task CloseAsync();
		Task<IDbTransaction> BeginTransactionAsync();
		Task CommitTransactionAsync(IDbTransaction transaction);
		Task RollbackTransactionAsync(IDbTransaction transaction);
		IDbConnection GetConnection();
	}
}
