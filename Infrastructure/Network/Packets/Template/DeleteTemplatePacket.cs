using NC.PetitionLib;
using NC.ToolNet.Net;
using PetidionD.Infrastructure.Network.Packets.Base;

namespace PetidionD.Infrastructure.Network.Packets.Template;

public class DeleteTemplatePacket(ILogger<DeleteTemplatePacket> logger)
    : GmPacketBase(PacketType.G_DELETE_TEMPLATE)
{
    private readonly ILogger<DeleteTemplatePacket> _logger = logger;

    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var code = unpacker.GetInt32();
            var result = Template.Delete(session.AccountUid, code);

            var response = new Packer((byte)PacketType.G_DELETE_TEMPLATE_RESULT);
            response.AddUInt8((byte)result);
            session.Send(response.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling template deletion");
        }
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_DELETE_TEMPLATE).ToArray();
}