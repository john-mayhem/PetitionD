// File: Infrastructure/Network/Sessions/NoticeSession.cs
using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Configuration;

namespace PetitionD.Infrastructure.Network.Sessions;

public class NoticeSession : BaseSession
{
    private readonly ILogger<NoticeSession> _logger;
    private readonly WorldSessionManager _worldSessionManager;
    private readonly AppSettings _settings;

    public string RemoteIp { get; private set; } = string.Empty;

    public NoticeSession(
        ILogger<NoticeSession> logger,
        WorldSessionManager worldSessionManager,
        AppSettings settings) : base(logger)
    {
        _logger = logger;
        _worldSessionManager = worldSessionManager;
        _settings = settings;
    }

    protected override void OnReceived(byte[] packet)
    {
        try
        {
            var packetType = (PacketType)packet[0];
            _logger.LogDebug("Received Notice packet: {PacketType}", packetType);

            if (packetType != PacketType.N_SUBMIT_NOTICE)
            {
                _logger.LogWarning("Unexpected notice packet type: {PacketType}", packetType);
                return;
            }

            HandleSubmitNotice(new Unpacker(packet));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notice packet");
        }
    }

    private void HandleSubmitNotice(Unpacker unpacker)
    {
        var noticeType = unpacker.GetUInt8();
        var worldId = unpacker.GetUInt8();
        var flag = unpacker.GetUInt8();
        var content = unpacker.GetShortStringMax(MaxLen.NoticeContent);

        var worldSession = _worldSessionManager.GetSession(worldId);
        if (worldSession == null)
        {
            SendResponse(PetitionErrorCode.WorldDown, 0);
            return;
        }

        if (flag == 0)
        {
            worldSession.SetNotice(content);
        }

        // Broadcast to GMs
        var notification = new Packer((byte)PacketType.G_NOTIFY_NOTICE);
        notification.AddUInt8(flag);
        notification.AddInt32(worldId);
        notification.AddString(content);
        var gmCount = worldSession.BroadcastToGm(notification.ToArray());

        SendResponse(PetitionErrorCode.Success, gmCount);
    }

    private void SendResponse(PetitionErrorCode errorCode, int gmCount)
    {
        var response = new Packer(
            errorCode == PetitionErrorCode.Success
                ? (byte)PacketType.N_SUBMIT_NOTICE_OK
                : (byte)PacketType.N_SUBMIT_NOTICE_FAIL);

        if (errorCode == PetitionErrorCode.Success)
        {
            response.AddInt32(gmCount);
        }
        else
        {
            response.AddUInt8((byte)errorCode);
        }

        Send(response.ToArray());
    }

    protected override void OnSessionStarted()
    {
        if (RemoteEndPoint != null)
        {
            RemoteIp = RemoteEndPoint.ToString() ?? string.Empty;
            _logger.LogInformation("Notice Session started from {RemoteIp}", RemoteIp);
        }

        var packer = new Packer((byte)PacketType.N_SERVER_VER);
        packer.AddInt32(_settings.ServerBuildNumber);
        Send(packer.ToArray());
    }

    protected override void OnSessionStopped()
    {
        _logger.LogInformation("Notice Session stopped: {Id} from {RemoteIp}", Id, RemoteIp);
    }

    public override string ToString() => $"[Notice {RemoteIp}]";
}