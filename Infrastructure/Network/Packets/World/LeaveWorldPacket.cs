// File: Infrastructure/Network/Packets/World/LeaveWorldPacket.cs
namespace PetitionD.Infrastructure.Network.Packets.World;

using Microsoft.Extensions.Logging;
using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Infrastructure.Network.Sessions;

public class LeaveWorldPacket(ILogger<LeaveWorldPacket> logger) : GmPacketBase(PacketType.G_LEAVE_WORLD)
{
    private readonly ILogger<LeaveWorldPacket> _logger = logger;

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