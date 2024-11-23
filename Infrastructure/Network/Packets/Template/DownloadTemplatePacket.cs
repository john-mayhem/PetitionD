namespace PetitionD.Infrastructure.Network.Packets.Template;

using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Infrastructure.Network.Packets.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PetitionD.Core.Models;


public class DownloadTemplatePacket(ILogger<DownloadTemplatePacket> logger)
    : GmPacketBase(PacketType.G_DOWNLOAD_TEMPLATE)
{
    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var code = unpacker.GetInt32();
            var result = Template.Download(
                session.AccountUid,
                session.Account,
                code,
                out int resultCode);

            var response = new Packer((byte)PacketType.G_DOWNLOAD_TEMPLATE_RESULT);
            response.AddUInt8((byte)result);
            response.AddInt32(resultCode);
            session.Send(response.ToArray());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling template download");
        }
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_DOWNLOAD_TEMPLATE).ToArray();
}