using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Core.Models;

namespace PetitionD.Infrastructure.Network.Packets.Template;

public class DeleteTemplatePacket(ILogger<DeleteTemplatePacket> logger)
    : GmPacketBase(PacketType.G_DELETE_TEMPLATE)
{
    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var code = unpacker.GetInt32();
            var result = Template.Operations.Delete(session.AccountUid, code);

            var response = new Packer((byte)PacketType.G_DELETE_TEMPLATE_RESULT);
            response.AddUInt8((byte)result);
            session.Send(response.ToArray());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling template deletion");
        }
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_DELETE_TEMPLATE).ToArray();
}