using NC.PetitionLib;
using NC.ToolNet.Net;
using PetidionD.Infrastructure.Network.Packets.Base;

namespace PetidionD.Infrastructure.Network.Packets.Template;

public class RequestTemplatePacket(ILogger<RequestTemplatePacket> logger)
    : GmPacketBase(PacketType.G_REQUEST_TEMPLATE)
{
    private readonly ILogger<RequestTemplatePacket> _logger = logger;

    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var domain = (Template.Domain)unpacker.GetUInt8();
            var templateList = Template.GetTemplateList(domain == Template.Domain.PUBLIC ? 0 : session.AccountUid);

            // Send each template
            foreach (var template in templateList)
            {
                var templatePacker = new Packer((byte)PacketType.G_TEMPLATE_LIST);
                templatePacker.AddUInt8((byte)domain);
                template.Serialize(templatePacker);
                session.Send(templatePacker.ToArray());
            }

            // Send end marker
            var endPacker = new Packer((byte)PacketType.G_TEMPLATE_LIST_END);
            endPacker.AddUInt8((byte)domain);
            endPacker.AddInt32(templateList.Count);
            session.Send(endPacker.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling template request");
        }
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_REQUEST_TEMPLATE).ToArray();
}