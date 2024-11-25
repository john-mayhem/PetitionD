// File: Program.cs
namespace PetitionD;

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
using PetitionD.Infrastructure.Database.Repositories;
using PetitionD.Core.Models;
using PetitionD.Core.Services;
using PetitionD.Infrastructure.Resilience;
using PetitionD.Infrastructure.Logging;

static class Program
{
    [STAThread]
    static async Task Main()  // Change to async Task Main
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var services = new ServiceCollection();
        ConfigureServices(services);

        await using var serviceProvider = services.BuildServiceProvider(); // Use await using
        var mainForm = serviceProvider.GetRequiredService<MainForm>();

        // Wire up packet logging
        ConfigurePacketLogging(serviceProvider, mainForm);

        Application.Run(mainForm);
    }

    private static void ConfigurePacketLogging(IServiceProvider serviceProvider, MainForm mainForm)
    {
        var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();
        var worldSessionManager = serviceProvider.GetRequiredService<IWorldSessionManager>();

        void LogPacket(ISession session, byte[] data, bool isOutgoing)
        {
            var packetType = (PacketType)data[0];
            mainForm.LogPacket(
                DateTime.Now,
                isOutgoing ? "OUT" : "IN",
                session.Id,
                packetType.ToString(),
                data.Length,
                data
            );
        }

        sessionManager.SessionCreated += session =>
        {
            session.PacketLogged += (data, isOutgoing) => LogPacket(session, data, isOutgoing);
            mainForm.UpdateConnection(
                session.Id,
                session is GmSession ? "GM" : "Notice",
                session.RemoteEndPoint?.ToString() ?? "Unknown",
                DateTime.Now,
                "Connected"
            );
        };

        worldSessionManager.SessionCreated += session =>
        {
            session.PacketLogged += (data, isOutgoing) => LogPacket(session, data, isOutgoing);
            mainForm.UpdateConnection(
                session.Id,
                "World",
                session.RemoteEndPoint?.ToString() ?? "Unknown",
                DateTime.Now,
                "Connected"
            );
        };
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Load configuration
        var settings = ConfigurationManager.LoadConfiguration();
        services.AddSingleton(settings);

        // Initialize NC.ToolNet logging first!
        NC.ToolNet.Networking.Services.LoggingService.Initialize(settings.LogDirectory);

        // Configure logging
        services.AddLogging(builder =>
        {
            LoggingConfiguration.ConfigureLogging(builder, settings.LogDirectory);
        });

        // Core services
        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddSingleton<PetitionList>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IPetitionService, PetitionService>();
        services.AddSingleton<QuotaService>();
        services.AddSingleton<GmStatusService>();
        services.AddSingleton<TemplateService>();

        // Network services
        services.AddSingleton<PacketHandler>();
        services.AddSingleton<IWorldSessionManager, WorldSessionManager>();
        services.AddSingleton<GmPacketFactory>();
        services.AddSingleton<WorldPacketFactory>();  // Add this if it's not already there

        // Service Endpoints
        services.AddSingleton<GmService>();
        services.AddSingleton<WorldService>();
        services.AddSingleton<NoticeService>();

        // Register ServerService after all network services
        services.AddSingleton<ServerService>();

        // Database services
        services.AddScoped<IDbRepository, DbRepository>();
        services.AddDatabaseServices(settings);
        services.AddSingleton<DbContext>();
        services.AddSingleton<PetitionRepository>();
        services.AddSingleton<TemplateRepository>();
        services.AddSingleton<GmRepository>();

        // Resilience services
        services.AddSingleton<CircuitBreaker>();
        services.AddSingleton<RetryPolicy>();
        services.AddSingleton<ResiliencePolicy>();

        // UI Components
        services.AddSingleton<MainForm>();

        // Initialize services
        PetitionList.InitializeSequence();
        GmStatus.Clear();
    }
}