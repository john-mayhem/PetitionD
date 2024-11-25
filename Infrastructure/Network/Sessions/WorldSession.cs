using NC.PetitionLib;
using NC.ToolNet.Networking;
using PetitionD.Configuration;
using PetitionD.Infrastructure.Network.Packets.Base;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace PetitionD.Infrastructure.Network.Sessions;

public class WorldSession : BaseSession
{
    private readonly ILogger<WorldSession> _logger;
    private readonly PetitionList _petitionList;
    private readonly AppSettings _settings;
    private readonly WorldPacketFactory _packetFactory;  // Change this from GmPacketFactory

    public int WorldId { get; internal set; }
    public string WorldName { get; internal set; } = "Unknown";
    public WorldSessionState State { get; internal set; } = WorldSessionState.Init;
    public bool IsNormalShutdown { get; private set; }
    public string Notice { get; private set; } = string.Empty;
    public byte[]? OneTimeKey { get; private set; }
    public DateTime LastOnlineCheckTime { get; internal set; }

    public readonly ConcurrentDictionary<int, GmSession> _gmSessions = new();
    public WorldSession(
        ILogger<WorldSession> logger,
        PetitionList petitionList,
        AppSettings settings,
        WorldPacketFactory packetFactory)  // Change this parameter type
        : base(logger)
    {
        _logger = logger;
        _petitionList = petitionList;
        _settings = settings;
        _packetFactory = packetFactory;
        GenerateOneTimeKey();
    }

    protected override void HandlePacket(byte[] packet)
    {
        try
        {
            var packetType = (PacketType)packet[0];
            _logger.LogDebug("Received World packet: {PacketType}", packetType);

            var unpacker = new Unpacker(packet);
            OnReceived(packet); // Call the existing OnReceived method
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing world packet");
        }
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

    public int BroadcastToGm(byte[] data)
    {
        int gmCount = 0;

        foreach (var session in _gmSessions.Values)
        {
            try
            {
                session.Send(data);
                gmCount++; // Increment the count for each successful send
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast to GM {SessionId}", session.Id);
            }
        }

        return gmCount; // Return the total number of GMs notified
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

            OnPacketLogged(packet, false);

            var handler = _packetFactory.CreatePacket(packetType);
            if (handler == null)
            {
                _logger.LogWarning("No handler for packet type: {PacketType}", packetType);
                return;
            }

            var unpacker = new Unpacker(packet);
            if (handler is WorldPacketBase worldPacketHandler)
            {
                // Add packet validation
                if (ValidatePacket(packet))
                {
                    worldPacketHandler.Handle(this, unpacker);
                }
                else
                {
                    _logger.LogWarning("Invalid packet received: {PacketType}", packetType);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing packet");
        }
    }

    private bool ValidatePacket(byte[] packet)
    {
        // Minimum packet size is 3 (type + length)
        if (packet == null || packet.Length < 3)
            return false;

        try
        {
            // First byte is packet type
            var packetType = (PacketType)packet[0];

            // For world packets, skip strict size validation
            // as they have variable length payloads
            if ((int)packetType >= 100 && (int)packetType < 200)
            {
                return true;
            }

            // For other packets, validate size
            if (packet.Length >= 3)
            {
                var size = BitConverter.ToUInt16(packet, 1);
                return packet.Length >= size + 3;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Packet validation failed");
            return false;
        }
    }

    public override void Send(byte[] data)
    {
        try
        {
            OnPacketLogged(data, true); // Use protected method
            base.Send(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send packet");
        }
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
