using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Configuration;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace PetitionD.Infrastructure.Network.Sessions;

public class WorldSession : BaseSession
{
    private readonly ILogger<WorldSession> _logger;
    private readonly PetitionList _petitionList;
    private readonly AppSettings _settings;
    private readonly GmPacketFactory _packetFactory;

    public int WorldId { get; internal set; }
    public string WorldName { get; internal set; } = "Unknown";
    public WorldSessionState State { get; internal set; } = WorldSessionState.Init;
    public bool IsNormalShutdown { get; private set; }
    public string Notice { get; private set; } = string.Empty;
    public byte[]? OneTimeKey { get; private set; }
    public DateTime LastOnlineCheckTime { get; internal set; }

    private readonly ConcurrentDictionary<int, GmSession> _gmSessions = new();

    public WorldSession(
        ILogger<WorldSession> logger,
        PetitionList petitionList,
        AppSettings settings,
        GmPacketFactory packetFactory) : base(logger)
    {
        _logger = logger;
        _petitionList = petitionList;
        _settings = settings;
        _packetFactory = packetFactory;
        GenerateOneTimeKey();
    }

    private void GenerateOneTimeKey()
    {
        OneTimeKey = new byte[8];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(OneTimeKey);
    }

    public void SetNotice(string notice)
    {
        Notice = notice;
        _logger.LogInformation("Notice updated for world {WorldId}: {Notice}", WorldId, notice);
    }

    public void AddGmSession(GmSession gmSession)
    {
        var character = gmSession.GetCharacter(WorldId);
        if (character != null)
        {
            _gmSessions.TryAdd(character.CharUid, gmSession);
            _logger.LogInformation("GM {CharName} added to world {WorldId}", character.CharName, WorldId);
        }
    }

    public void RemoveGmSession(GmSession gmSession)
    {
        var character = gmSession.GetCharacter(WorldId);
        if (character != null && _gmSessions.TryRemove(character.CharUid, out _))
        {
            _logger.LogInformation("GM {CharName} removed from world {WorldId}", character.CharName, WorldId);
        }
    }

    public GmSession? GetGmSession(int charUid)
    {
        _gmSessions.TryGetValue(charUid, out var session);
        return session;
    }

    public int GetGmCount() => _gmSessions.Count;

    public void BroadcastToGm(byte[] data)
    {
        foreach (var session in _gmSessions.Values)
        {
            try
            {
                session.Send(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast to GM {SessionId}", session.Id);
            }
        }
    }

    public void BroadcastToGmExcept(byte[] data, GmSession except)
    {
        foreach (var session in _gmSessions.Values)
        {
            if (session != except)
            {
                try
                {
                    session.Send(data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to broadcast to GM {SessionId}", session.Id);
                }
            }
        }
    }

    protected override void OnReceived(byte[] packet)
    {
        try
        {
            var packetType = (PacketType)packet[0];
            _logger.LogDebug("Received World packet: {PacketType}", packetType);

            var handler = _packetFactory.CreatePacket(packetType);
            if (handler == null)
            {
                _logger.LogWarning("No handler for packet type: {PacketType}", packetType);
                return;
            }

            var unpacker = new Unpacker(packet);
            // TODO: Implement world packet handling
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing world packet");
        }
    }

    protected override void OnSessionStarted()
    {
        _logger.LogInformation("World Session started: {Id}", Id);

        var packer = new Packer((byte)PacketType.W_SERVER_VER);
        packer.AddInt32(_settings.ServerBuildNumber);
        packer.AddBytes(OneTimeKey ?? []);
        Send(packer.ToArray());
    }

    protected override void OnSessionStopped()
    {
        _logger.LogInformation("World Session stopped: {Id} (Normal shutdown: {IsNormalShutdown})",
            Id, IsNormalShutdown);

        // Clean up GM sessions
        foreach (var gmSession in _gmSessions.Values)
        {
            RemoveGmSession(gmSession);
        }
    }

    public override string ToString() => $"[World: {WorldName}({WorldId})]";

    protected override void OnSendFailed(Exception e)
    {
        _logger.LogError(e, "Failed to send world packet");
    }
}

public enum WorldSessionState
{
    Init,
    Connected
}
