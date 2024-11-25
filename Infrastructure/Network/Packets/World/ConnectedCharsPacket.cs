// File: Infrastructure/Network/Packets/World/ConnectedCharsPacket.cs
using NC.PetitionLib;
using NC.ToolNet.Networking.Protocol;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Core.Models;

namespace PetitionD.Infrastructure.Network.Packets.World;

public class ConnectedCharsPacket(
    ILogger<ConnectedCharsPacket> logger) : WorldPacketBase(PacketType.W_CONNECTED_CHARS2, logger)
{
    public override void Handle(WorldSession worldSession, Unpacker unpacker)
    {
        try
        {
            var charCount = unpacker.GetUInt16();
            var response = new Packer((byte)PacketType.G_NOTIFY_CONNECTED_CHARS);
            response.AddInt32(worldSession.WorldId);
            response.AddInt32(charCount);

            for (int i = 0; i < charCount; i++)
            {
                var charUid = unpacker.GetInt32();
                response.AddInt32(charUid);
            }

            worldSession.BroadcastToGm(response.ToArray());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling connected characters update");
        }
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.W_CONNECTED_CHARS2).ToArray();
}
