using NC.PetitionLib;
using NC.ToolNet.Net;
using PetidionD.Infrastructure.Network.Packets.Base;

namespace PetidionD.Infrastructure.Network.Packets.Template;

public class UpdateTemplatePacket(ILogger<UpdateTemplatePacket> logger)
    : GmPacketBase(PacketType.G_UPDATE_TEMPLATE)
{
    private readonly ILogger<UpdateTemplatePacket> _logger = logger;

    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var code = unpacker.GetInt32();
            var name = unpacker.GetStringMax(MaxLen.TemplateName);
            var type = (Template.Type)unpacker.GetUInt8();
            var content = unpacker.GetStringMax(MaxLen.TemplateContent);
            var order = unpacker.GetInt32();

            int resultCode;
            var result = Template.Update(
                session.AccountUid,
                session.Account,
                code,
                name,
                type,
                content,
                order,
                out resultCode);

            var response = new Packer((byte)PacketType.G_UPDATE_TEMPLATE_RESULT);
            response.AddUInt8((byte)result);
            response.AddInt32(resultCode);
            session.Send(response.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling template update");
        }
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_UPDATE_TEMPLATE).ToArray();
}