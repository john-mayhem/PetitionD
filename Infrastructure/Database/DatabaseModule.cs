using Microsoft.Extensions.DependencyInjection;
using PetitionD.Infrastructure.Database.Repositories;
using System.Data;
using Microsoft.Data.SqlClient;
using PetitionD.Configuration;

namespace PetitionD.Infrastructure.Database;

public static class DatabaseModule
{
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        AppSettings settings)
    {
        // Register connection factory
        services.AddSingleton<ISqlConnectionFactory>(_ =>
            new SqlConnectionFactory(settings.DatabaseConnString));

        // Register connection pool
        services.AddSingleton(sp =>
            new DbConnectionPool(
                sp.GetRequiredService<ISqlConnectionFactory>(),
                settings.DatabaseConnCount));

        // Register DbContext
        services.AddScoped<DbContext>();

        // Register repositories
        services.AddScoped<PetitionRepository>();
        services.AddScoped<GmRepository>();
        services.AddScoped<TemplateRepository>();

        return services;
    }
}