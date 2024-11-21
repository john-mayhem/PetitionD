// File: Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PetitionD.UI.Forms;
using PetitionD.Infrastructure.Network;
using PetitionD.Infrastructure.Network.Sessions;
using PetitionD.Infrastructure.Network.Packets;
using PetitionD.Core.Interfaces;
using PetitionD.Services;
using PetitionD.Configuration;
using PetitionD.Infrastructure.Database;
using PetidionD.Core.Interfaces;
using PetidionD.Infrastructure.Database.Repositories;
using PetidionD.Infrastructure.Database;

namespace PetitionD;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var services = new ServiceCollection();
        ConfigureServices(services);

        using var serviceProvider = services.BuildServiceProvider();
        var mainForm = serviceProvider.GetRequiredService<MainForm>();
        Application.Run(mainForm);
    }

    // File: Program.cs
    private static void ConfigureServices(IServiceCollection services)
    {
        // Load configuration
        var settings = ConfigurationManager.LoadConfiguration();
        services.AddSingleton(settings);
        services.AddSingleton<GmPacketFactory>();

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Core services
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddSingleton<PetitionList>();


        // Network services
        services.AddSingleton<PacketHandler>();
        services.AddSingleton<WorldSessionManager>();
        services.AddSingleton<ISessionManager, SessionManager>();

        //Service Endpoints
        services.AddSingleton(sp => new GmService(
            sp.GetRequiredService<AppSettings>().GmServicePort,
            sp.GetRequiredService<ISessionManager>(),
            sp.GetRequiredService<ILogger<GmService>>(),
            sp.GetRequiredService<IAuthService>(),
            sp.GetRequiredService<ILoggerFactory>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<GmPacketFactory>()
        ));


        services.AddSingleton<WorldService>();  // We'll create this next
        services.AddSingleton<DbContext>();
        services.AddSingleton<PetitionRepository>();
        services.AddSingleton<TemplateRepository>();
        services.AddSingleton<GmRepository>();

        services.AddSingleton<IPetitionService, PetitionService>();

        services.AddSingleton(sp => new NoticeService(
            sp.GetRequiredService<ILogger<NoticeService>>(),
            sp.GetRequiredService<WorldSessionManager>(),
            sp.GetRequiredService<ILoggerFactory>(),
            sp.GetRequiredService<AppSettings>()
        ));

        // Mock services for initial testing 
        services.AddSingleton<IDbRepository>(sp =>
            new MockDbRepository(sp.GetRequiredService<ILogger<MockDbRepository>>()));

        // UI Components
        services.AddSingleton<MainForm>();
    }
}

// Temporary mock class for testing
// TODO: Move to separate file in Infrastructure/Database
public class MockDbRepository(ILogger<MockDbRepository> logger) : IDbRepository
{
    public Task<(bool IsValid, int AccountUid)> ValidateGmCredentialsAsync(string account, string password)
    {
        logger.LogInformation("Mock validation for account: {Account}", account);

        // For testing, accept any login with password "test"
        var isValid = password == "test";
        var accountUid = isValid ? 1 : 0;

        return Task.FromResult((isValid, accountUid));
    }
}