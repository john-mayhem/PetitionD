// File: Infrastructure/Network/WorldService.cs
using System.Net.Sockets;
using NC.ToolNet.Networking.Server;
using PetitionD.Configuration;

namespace PetitionD.Infrastructure.Network;

public class WorldService : NetworkBase
{
    private readonly ILogger<WorldService> _logger;
    private readonly IWorldSessionManager _sessionManager;  // Changed to interface
    private readonly ILoggerFactory _loggerFactory;
    private readonly AppSettings _settings;
    private readonly PetitionList _petitionList;
    private readonly WorldPacketFactory _packetFactory;  // Change from GmPacketFactory

    public WorldService(
        ILogger<WorldService> logger,
        IWorldSessionManager sessionManager,
        ILoggerFactory loggerFactory,
        AppSettings settings,
        PetitionList petitionList,
        WorldPacketFactory packetFactory)  // Change parameter type
        : base(settings.WorldServicePort)
    {
        _logger = logger;
        _sessionManager = sessionManager;
        _loggerFactory = loggerFactory;
        _settings = settings;
        _petitionList = petitionList;
        _packetFactory = packetFactory;
    }

    protected override void OnSocketAccepted(ListenerSocket listener, Socket socket)
    {
        try
        {
            _logger.LogInformation("New World connection from {Endpoint}", socket.RemoteEndPoint);

            var worldLogger = _loggerFactory.CreateLogger<WorldSession>();
            var session = new WorldSession(
                worldLogger,
                _petitionList,
                _settings,
                _packetFactory);

            session.Start(socket);
            _sessionManager.AddSession(session);  // Add this line to track the session
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting World connection");
        }
    }
}