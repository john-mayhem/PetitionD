
// File: Infrastructure/Network/Packets/RequestCategoryPacket.cs
using NC.PetitionLib;
using NC.ToolNet.Networking.Protocol;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Core.Models;

namespace PetitionD.Infrastructure.Network.Packets.PetitionHandlers;

public class RequestCategoryPacket(ILogger<RequestCategoryPacket> logger) : GmPacketBase(PacketType.G_REQUEST_CATEGORY)
{
    private readonly ILogger<RequestCategoryPacket> _logger = logger;

    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var response = new Packer((byte)PacketType.G_CATEGORY_LIST);
            // TODO: Add category data once we implement the category system
            response.AddInt32(0); // Number of categories
            session.Send(response.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling category request");
        }
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_REQUEST_CATEGORY).ToArray();
}
