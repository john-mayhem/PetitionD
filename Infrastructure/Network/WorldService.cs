using System.Net.Sockets;
using NC.ToolNet.Net.Server;
using PetitionD.Configuration;

namespace PetitionD.Infrastructure.Network;

public class WorldService(
    ILogger<WorldService> logger,
    WorldSessionManager sessionManager,
    ILoggerFactory loggerFactory,
    AppSettings settings,
    PetitionList petitionList,
    GmPacketFactory packetFactory) : NetworkBase(settings.WorldServicePort)
{
    private readonly ILogger<WorldService> _logger = logger;
    private readonly WorldSessionManager _sessionManager = sessionManager;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly AppSettings _settings = settings;
    private readonly PetitionList _petitionList = petitionList;
    private readonly GmPacketFactory _packetFactory = packetFactory;

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting World connection");
        }
    }
}