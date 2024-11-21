// File: Infrastructure/Database/DbContext.cs
using Microsoft.Data.SqlClient;
using System.Data;

namespace PetidionD.Infrastructure.Database;

public class DbContext
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<DbContext> _logger;

    public DbContext(
        ISqlConnectionFactory connectionFactory,
        ILogger<DbContext> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<T> ExecuteStoredProcAsync<T>(
        string procedureName,
        object parameters,
        Func<SqlDataReader, Task<T>> mapper,
        int commandTimeout = 30)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = new SqlCommand(procedureName, (SqlConnection)connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = commandTimeout
        };

        AddParameters(command, parameters);

        try
        {
            using var reader = await command.ExecuteReaderAsync();
            return await mapper(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure {ProcedureName} with parameters {@Parameters}",
                procedureName, parameters);
            throw new DatabaseException($"Error executing {procedureName}", ex);
        }
    }

    public async Task<int> ExecuteNonQueryAsync(
        string procedureName,
        object parameters,
        int commandTimeout = 30)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = new SqlCommand(procedureName, (SqlConnection)connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = commandTimeout
        };

        AddParameters(command, parameters);

        try
        {
            return await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure {ProcedureName} with parameters {@Parameters}",
                procedureName, parameters);
            throw new DatabaseException($"Error executing {procedureName}", ex);
        }
    }

    private static void AddParameters(SqlCommand command, object parameters)
    {
        foreach (var prop in parameters.GetType().GetProperties())
        {
            var value = prop.GetValue(parameters);
            var parameter = command.Parameters.AddWithValue($"@{prop.Name}", value ?? DBNull.Value);

            // Handle specific SQL types if needed
            if (value is DateTime)
                parameter.SqlDbType = SqlDbType.DateTime;
            else if (value is byte)
                parameter.SqlDbType = SqlDbType.TinyInt;
        }
    }
}
