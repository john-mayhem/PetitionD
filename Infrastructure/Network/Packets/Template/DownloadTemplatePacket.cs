using NC.PetitionLib;
using NC.ToolNet.Net;
using PetidionD.Infrastructure.Network.Packets.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetidionD.Infrastructure.Network.Packets.Template
{
    public class DownloadTemplatePacket(
        ILogger<DownloadTemplatePacket> logger)
        : GmPacketBase(PacketType.G_DOWNLOAD_TEMPLATE)
    {
        private readonly ILogger<DownloadTemplatePacket> _logger = logger;

        public override void Handle(GmSession session, Unpacker unpacker)
        {
            try
            {
                var code = unpacker.GetInt32();

                int resultCode;
                var result = Template.Download(
                    session.AccountUid,
                    session.Account,
                    code,
                    out resultCode);

                var response = new Packer((byte)PacketType.G_DOWNLOAD_TEMPLATE_RESULT);
                response.AddUInt8((byte)result);
                response.AddInt32(resultCode);
                session.Send(response.ToArray());

                if (result == PetitionErrorCode.Success)
                {
                    _logger.LogInformation("Template {Code} downloaded by GM {AccountName}",
                        code, session.Account);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling template download");
            }
        }

        public override byte[] Serialize() =>
            new Packer((byte)PacketType.G_DOWNLOAD_TEMPLATE).ToArray();
    }
}
