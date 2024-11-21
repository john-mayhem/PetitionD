using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Infrastructure.Network.Packets.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PetitionD.Core.Models;

namespace PetitionD.Infrastructure.Network.Packets.Template
{
    public class UpdateTemplateOrderPacket(
        ILogger<UpdateTemplateOrderPacket> logger)
        : GmPacketBase(PacketType.G_UPDATE_TEMPLATE_ORDER)
    {
        private readonly ILogger<UpdateTemplateOrderPacket> _logger = logger;

        public override void Handle(GmSession session, Unpacker unpacker)
        {
            try
            {
                var code = unpacker.GetInt32();
                var offset = unpacker.GetInt32();

                var result = Template.UpdateOrder(session.AccountUid, code, offset);

                var response = new Packer((byte)PacketType.G_UPDATE_TEMPLATE_ORDER_RESULT);
                response.AddUInt8((byte)result);
                session.Send(response.ToArray());

                _logger.LogInformation("Template {Code} order updated by GM {AccountName}",
                    code, session.Account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling template order update");
            }
        }

        public override byte[] Serialize() =>
            new Packer((byte)PacketType.G_UPDATE_TEMPLATE_ORDER).ToArray();
    }
}

