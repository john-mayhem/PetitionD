// File: Infrastructure/Database/DbContext.cs
namespace PetitionD.Infrastructure.Database;

using Microsoft.Data.SqlClient;
using PetitionD.Infrastructure.Resilience;
using System.Data;

public class DbContext : IAsyncDisposable
{
    private readonly DbConnectionPool _connectionPool;
    private readonly ILogger<DbContext> _logger;
    private readonly ResiliencePolicy _resiliencePolicy;
    private IDbConnection? _currentConnection;

    public DbContext(
        DbConnectionPool connectionPool,
        ILogger<DbContext> logger,
        ResiliencePolicy resiliencePolicy)
    {
        _connectionPool = connectionPool;
        _logger = logger;
        _resiliencePolicy = resiliencePolicy;
    }

    public async Task<T> ExecuteWithResilienceAsync<T>(
        Func<IDbConnection, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        return await _resiliencePolicy.ExecuteAsync(
            async token =>
            {
                var connection = await GetConnectionAsync(token);
                return await operation(connection, token);
            },
            ResiliencePolicy.ShouldRetryDatabaseOperation,
            cancellationToken);
    }

    private async Task<IDbConnection> GetConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_currentConnection != null)
            return _currentConnection;

        _currentConnection = await _connectionPool.GetConnectionAsync(cancellationToken);
        return _currentConnection;
    }

    public async Task<T> ExecuteStoredProcAsync<T>(
        string procedureName,
        object parameters,
        Func<SqlDataReader, CancellationToken, Task<T>> mapper,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        using var command = new SqlCommand(procedureName, (SqlConnection)connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30  // TODO: Make configurable
        };

        AddParameters(command, parameters);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await mapper(reader, cancellationToken);
    }

    public async Task ExecuteStoredProcAsync(
        string procedureName,
        object parameters,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        using var command = new SqlCommand(procedureName, (SqlConnection)connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddParameters(command, parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(SqlCommand command, object parameters)
    {
        foreach (var prop in parameters.GetType().GetProperties())
        {
            var value = prop.GetValue(parameters);
            command.Parameters.AddWithValue($"@{prop.Name}", value ?? DBNull.Value);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentConnection != null)
        {
            await _connectionPool.ReleaseConnectionAsync(_currentConnection);
            _currentConnection = null;
        }
    }
}