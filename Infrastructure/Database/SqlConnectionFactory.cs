// File: Infrastructure/Database/SqlConnectionFactory.cs
using Microsoft.Data.SqlClient;
using System.Data;

namespace PetidionD.Infrastructure.Database;

public interface ISqlConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync();
}

public class SqlConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    private readonly string _connectionString = connectionString;

    public async Task<IDbConnection> CreateConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}

