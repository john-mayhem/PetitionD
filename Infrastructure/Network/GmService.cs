// File: Infrastructure/Network/GmService.cs
namespace PetitionD.Infrastructure.Network;

using Microsoft.Extensions.Logging;
using NC.ToolNet.Net.Server;
using PetitionD.Configuration;
using PetitionD.Core.Interfaces;
using PetitionD.Infrastructure.Network.Packets;
using PetitionD.Infrastructure.Network.Sessions;
using System.Net.Sockets;

public class GmService(
    int port,
    ISessionManager sessionManager,
    ILogger<GmService> logger,
    IAuthService authService,
    ILoggerFactory loggerFactory,
    AppSettings settings,
    GmPacketFactory packetFactory) : NetworkBase(port)
{
    private readonly ISessionManager _sessionManager = sessionManager;
    private readonly ILogger<GmService> _logger = logger;
    private readonly IAuthService _authService = authService;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly AppSettings _settings = settings;
    private readonly GmPacketFactory _packetFactory = packetFactory;

    protected override void OnSocketAccepted(ListenerSocket listener, Socket socket)
    {
        _logger.LogInformation("New GM connection from {Endpoint}", socket.RemoteEndPoint);

        var gmLogger = _loggerFactory.CreateLogger<GmSession>();
        var session = new GmSession(gmLogger, _authService, _settings, _packetFactory);

        session.Start(socket);
        _sessionManager.AddSession(session);
    }
}