using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.Data;

namespace PetitionD.Infrastructure.Database;

public class DbConnectionPool(
    ISqlConnectionFactory connectionFactory,
    int maxSize) : IAsyncDisposable
{
    private readonly ISqlConnectionFactory _connectionFactory = connectionFactory;
    private readonly ConcurrentBag<IDbConnection> _connections = [];
    private readonly SemaphoreSlim _poolSemaphore = new(maxSize, maxSize);
    private readonly int _maxSize = maxSize;
    private volatile bool _isDisposed;

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

    public async Task<T> ExecuteReaderAsync<T>(
    string storedProcName,
    object parameters,
    Func<SqlDataReader, CancellationToken, Task<T>> mapper,
    CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        using var command = new SqlCommand(storedProcName, (SqlConnection)connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddParameters(command, parameters);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await mapper(reader, cancellationToken);
    }

    private static void AddParameters(SqlCommand command, object parameters)
    {
        foreach (var prop in parameters.GetType().GetProperties())
        {
            var value = prop.GetValue(parameters);
            command.Parameters.AddWithValue($"@{prop.Name}", value ?? DBNull.Value);
        }
    }
}