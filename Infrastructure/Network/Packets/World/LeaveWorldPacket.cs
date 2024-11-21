// File: Infrastructure/Network/Packets/World/LeaveWorldPacket.cs
using NC.PetitionLib;
using NC.ToolNet.Net;
using PetidionD.Infrastructure.Network.Packets.Base;

namespace PetitionD.Infrastructure.Network.Packets.World;

public class LeaveWorldPacket(ILogger<LeaveWorldPacket> logger) : GmPacketBase(PacketType.G_LEAVE_WORLD)
{



    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var worldId = unpacker.GetInt32();
            var result = session.LeaveWorld(worldId);

            var packer = new Packer((byte)PacketType.G_ACCEPT_LEAVING);
            packer.AddInt32(worldId);
            packer.AddUInt8((byte)result);
            session.Send(packer.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling leave world request");
        }
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_LEAVE_WORLD).ToArray();
}