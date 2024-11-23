// File: Infrastructure/Network/Packets/Template/RequestTemplatePacket.cs
namespace PetitionD.Infrastructure.Network.Packets.Template;

using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Core.Models;

public class RequestTemplatePacket(ILogger<RequestTemplatePacket> logger)
    : GmPacketBase(PacketType.G_REQUEST_TEMPLATE)
{
    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var domain = (Template.Domain)unpacker.GetUInt8();
            var templateList = Template.GetTemplateList(
                domain == Template.Domain.Public ? 0 : session.AccountUid);

            foreach (var template in templateList)
            {
                var templatePacker = new Packer((byte)PacketType.G_TEMPLATE_LIST);
                templatePacker.AddUInt8((byte)domain);
                template.Serialize(templatePacker);
                session.Send(templatePacker.ToArray());
            }

            var endPacker = new Packer((byte)PacketType.G_TEMPLATE_LIST_END);
            endPacker.AddUInt8((byte)domain);
            endPacker.AddInt32(templateList.Count());
            session.Send(endPacker.ToArray());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling template request");
        }
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_REQUEST_TEMPLATE).ToArray();
}