using System.Collections.Concurrent;
using System.Data;

namespace PetitionD.Infrastructure.Database;

public class DbConnectionPool : IAsyncDisposable
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ConcurrentBag<IDbConnection> _connections;
    private readonly SemaphoreSlim _poolSemaphore;
    private readonly int _maxSize;
    private volatile bool _isDisposed;

    public DbConnectionPool(
        ISqlConnectionFactory connectionFactory,
        int maxSize)
    {
        _connectionFactory = connectionFactory;
        _maxSize = maxSize;
        _connections = new ConcurrentBag<IDbConnection>();
        _poolSemaphore = new SemaphoreSlim(maxSize, maxSize);
    }

    public async Task<IDbConnection> GetConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_isDisposed)
        {
            await _poolSemaphore.WaitAsync(cancellationToken);

            try
            {
                if (_connections.TryTake(out var connection))
                {
                    if (connection.State == ConnectionState.Open)
                        return connection;

                    try
                    {
                        connection.Open();
                        return connection;
                    }
                    catch
                    {
                        await ReleaseConnectionAsync(connection);
                        throw;
                    }
                }

                // Create new connection if pool isn't full
                connection = await _connectionFactory.CreateConnectionAsync();
                return connection;
            }
            catch
            {
                _poolSemaphore.Release();
                throw;
            }
        }

        throw new ObjectDisposedException(nameof(DbConnectionPool));
    }

    public async Task ReleaseConnectionAsync(IDbConnection connection)
    {
        if (_isDisposed)
        {
            await DisposeConnectionAsync(connection);
            return;
        }

        try
        {
            if (connection.State != ConnectionState.Closed)
                connection.Close();

            _connections.Add(connection);
        }
        finally
        {
            _poolSemaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        while (_connections.TryTake(out var connection))
        {
            await DisposeConnectionAsync(connection);
        }

        _poolSemaphore.Dispose();
    }

    private static async ValueTask DisposeConnectionAsync(IDbConnection connection)
    {
        if (connection is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else
            connection.Dispose();
    }
}