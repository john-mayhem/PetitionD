// File: Infrastructure/Network/GmService.cs
namespace PetitionD.Infrastructure.Network;

using global::PetitionD.Configuration;
using Microsoft.Extensions.Logging;
using NC.ToolNet.Networking.Server;
using PetitionD.Configuration;
using PetitionD.Core.Interfaces;
using PetitionD.Infrastructure.Network.Packets;
using PetitionD.Infrastructure.Network.Sessions;
using System.Net.Sockets;
public class GmService : NetworkBase
{
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<GmService> _logger;
    private readonly IAuthService _authService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly AppSettings _settings;
    private readonly GmPacketFactory _packetFactory;

    public GmService(
        ISessionManager sessionManager,
        ILogger<GmService> logger,
        IAuthService authService,
        ILoggerFactory loggerFactory,
        AppSettings settings,
        GmPacketFactory packetFactory)
        : base(settings.GmServicePort)  // Pass the port from settings
    {
        _sessionManager = sessionManager;
        _logger = logger;
        _authService = authService;
        _loggerFactory = loggerFactory;
        _settings = settings;
        _packetFactory = packetFactory;
    }

    protected override void OnSocketAccepted(ListenerSocket listener, Socket socket)
    {
        _logger.LogInformation("New GM connection from {Endpoint}", socket.RemoteEndPoint);

        var gmLogger = _loggerFactory.CreateLogger<GmSession>();
        var session = new GmSession(gmLogger, _authService, _settings, _packetFactory);

        session.Start(socket);
        _sessionManager.AddSession(session);
    }
}