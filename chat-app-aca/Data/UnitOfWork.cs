using System.Data.Common;

namespace chat_app_aca.Data;

public sealed class UnitOfWork : IAsyncDisposable
{
    private readonly DbConnection _connection;
    private DbTransaction? _transaction;
    private bool _completed;

    public DbTransaction? Transaction => _transaction;

    public UnitOfWork(DbConnection connection)
    {
        _connection = connection;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("Transaction already started.");
        }

        _transaction = await _connection.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction to commit.");
        }

        await _transaction.CommitAsync(cancellationToken);
        _completed = true;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction to rollback.");
        }

        await _transaction.RollbackAsync(cancellationToken);
        _completed = true;
    }

    public void Dispose()
    {
        _connection.Dispose();
        _transaction?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_transaction != null && !_completed)
            {
                await _transaction.RollbackAsync();
            }
        }
        finally
        {
            await _connection.DisposeAsync();
        }
    }
}