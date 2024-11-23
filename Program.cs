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
using PetitionD.Core.Interfaces;
using PetitionD.Infrastructure.Database.Repositories;
using PetitionD.Infrastructure.Database;
using PetitionD.Core.Models;
using PetitionD.Core.Services;
using PetitionD.Infrastructure.Resilience;

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
        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddSingleton<PetitionList>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IPetitionService, PetitionService>();
        services.AddDatabaseServices(settings);
        services.AddSingleton<QuotaService>();
        services.AddSingleton<GmStatusService>();
        services.AddSingleton<TemplateService>();


        // Network services
        services.AddSingleton<PacketHandler>();
        services.AddSingleton<WorldSessionManager>();

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


        services.AddSingleton<WorldService>(); 
        services.AddSingleton<DbContext>();
        services.AddSingleton<PetitionRepository>();
        services.AddSingleton<TemplateRepository>();
        services.AddSingleton<GmRepository>();
        services.AddSingleton<IPetitionService, PetitionService>();
        services.AddSingleton<CircuitBreaker>();
        services.AddSingleton<RetryPolicy>();
        services.AddSingleton<ResiliencePolicy>();

        services.AddSingleton(sp => new NoticeService(
            sp.GetRequiredService<ILogger<NoticeService>>(),
            sp.GetRequiredService<WorldSessionManager>(),
            sp.GetRequiredService<ILoggerFactory>(),
            sp.GetRequiredService<AppSettings>()
        ));



        // UI Components
        services.AddSingleton<MainForm>();
        PetitionList.InitializeSequence();
        GmStatus.Clear();
    }
}

