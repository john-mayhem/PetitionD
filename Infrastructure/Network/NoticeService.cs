// File: Infrastructure/Network/NoticeService.cs
using System.Net;
using System.Net.Sockets;
using NC.ToolNet.Net.Server;
using PetitionD.Configuration;

namespace PetitionD.Infrastructure.Network;

public class NoticeService(
    ILogger<NoticeService> logger,
    WorldSessionManager worldSessionManager,
    ILoggerFactory loggerFactory,
    AppSettings settings) : NetworkBase(settings.NoticeServicePort)
{
    private readonly ILogger<NoticeService> _logger = logger;
    private readonly WorldSessionManager _worldSessionManager = worldSessionManager;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly AppSettings _settings = settings;
    private readonly HashSet<string> _allowedIps = new HashSet<string>(settings.NoticeServiceAllowIpList);

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