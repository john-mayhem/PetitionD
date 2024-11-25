// File: Infrastructure/Network/NoticeService.cs
using System.Net;
using System.Net.Sockets;
using NC.ToolNet.Networking.Server;
using PetitionD.Configuration;

namespace PetitionD.Infrastructure.Network;

public class NoticeService : NetworkBase
{
    private readonly ILogger<NoticeService> _logger;
    private readonly IWorldSessionManager _worldSessionManager;  // Change to interface
    private readonly ILoggerFactory _loggerFactory;
    private readonly AppSettings _settings;
    private readonly HashSet<string> _allowedIps;

    public NoticeService(
        ILogger<NoticeService> logger,
        IWorldSessionManager worldSessionManager,  // Change to interface
        ILoggerFactory loggerFactory,
        AppSettings settings)
        : base(settings.NoticeServicePort)
    {
        _logger = logger;
        _worldSessionManager = worldSessionManager;
        _loggerFactory = loggerFactory;
        _settings = settings;
        _allowedIps = new HashSet<string>(settings.NoticeServiceAllowIpList);
    }

    protected override void OnSocketAccepted(ListenerSocket listener, Socket socket)
    {
        try
        {
            var remoteIp = ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString();

            if (!_allowedIps.Contains(remoteIp))
            {
                _logger.LogWarning("Rejected notice connection from unauthorized IP: {RemoteIp}", remoteIp);
                socket.Close();
                return;
            }

            _logger.LogInformation("New Notice connection from {RemoteIp}", remoteIp);

            var noticeLogger = _loggerFactory.CreateLogger<NoticeSession>();
            var session = new NoticeSession(
                noticeLogger,
                _worldSessionManager,
                _settings);

            session.Start(socket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting Notice connection");
        }
    }
}