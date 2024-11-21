using System.Net.Sockets;
using NC.ToolNet.Net.Server;
using PetitionD.Configuration;

namespace PetitionD.Infrastructure.Network;

public class WorldService : NetworkBase
{
    private readonly ILogger<WorldService> _logger;
    private readonly WorldSessionManager _sessionManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly AppSettings _settings;
    private readonly PetitionList _petitionList;
    private readonly GmPacketFactory _packetFactory;

    public WorldService(
        ILogger<WorldService> logger,
        WorldSessionManager sessionManager,
        ILoggerFactory loggerFactory,
        AppSettings settings,
        PetitionList petitionList,
        GmPacketFactory packetFactory)
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting World connection");
        }
    }
}