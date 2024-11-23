
namespace PetitionD.Infrastructure.Network.Packets.World;
// File: Infrastructure/Network/Packets/World/RequestWorldListPacket.cs
using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Core.Models;
using PetitionD.Infrastructure.Network.Packets.Base;



public class RequestWorldListPacket(
    ILogger<RequestWorldListPacket> logger,
    WorldSessionManager worldSessionManager,
    PetitionList petitionList) : WorldPacketBase(PacketType.G_REQUEST_WORLD_LIST, logger)
{
    public override void Handle(WorldSession session, Unpacker unpacker)
    {
        try
        {
            var response = new Packer((byte)PacketType.G_WORLD_LIST);
            var worldSessions = worldSessionManager.GetAllSessions().ToList();

            response.AddInt32(worldSessions.Count);

            foreach (var worldSession in worldSessions)
            {
                response.AddInt32(worldSession.WorldId);
                response.AddString(worldSession.WorldName);
                response.AddInt32(petitionList.GetActivePetitionCount(worldSession.WorldId));
                response.AddInt32(worldSession.GetGmCount());
            }

            session.Send(response.ToArray());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling world list request");
        }
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_REQUEST_WORLD_LIST).ToArray();
}