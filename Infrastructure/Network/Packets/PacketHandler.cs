// File: Infrastructure/Network/Packets/PacketHandler.cs
using Microsoft.Extensions.Logging;
using PetitionD.Infrastructure.Network.Sessions;

namespace PetitionD.Infrastructure.Network.Packets;

public class PacketHandler(ILogger<PacketHandler> logger)
{
    private readonly Dictionary<byte, Action<ISession, byte[]>> _handlers = [];

    public void RegisterHandler(byte packetType, Action<ISession, byte[]> handler)
    {
        _handlers[packetType] = handler;
        logger.LogInformation("Registered handler for packet type: {PacketType}", packetType);
    }

    public void HandlePacket(ISession session, byte[] packet)
    {
        try
        {
            var packetType = packet[0];
            if (_handlers.TryGetValue(packetType, out var handler))
            {
                handler(session, packet);
                logger.LogDebug("Handled packet type: {PacketType}", packetType);
            }
            else
            {
                logger.LogWarning("Unknown packet type: {PacketType}", packetType);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling packet");
        }
    }
}