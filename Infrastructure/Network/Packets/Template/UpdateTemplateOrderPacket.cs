// File: Infrastructure/Network/Packets/Template/UpdateTemplateOrderPacket.cs
using Microsoft.Extensions.Logging;
using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Core.Models;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Infrastructure.Network.Sessions;

namespace PetitionD.Infrastructure.Network.Packets.Template
{
    public class UpdateTemplateOrderPacket : GmPacketBase
    {
        private readonly ILogger<UpdateTemplateOrderPacket> _logger;

        public UpdateTemplateOrderPacket(ILogger<UpdateTemplateOrderPacket> logger)
            : base(PacketType.G_UPDATE_TEMPLATE_ORDER)
        {
            _logger = logger;
        }

        public override void Handle(GmSession session, Unpacker unpacker)
        {
            try
            {
                var code = unpacker.GetInt32();
                var offset = unpacker.GetInt32();

                var result = PetitionD.Core.Models.Template.Operations.UpdateOrder(  // Fully qualified
                    session.AccountUid,
                    code,
                    offset);

                var response = new Packer((byte)PacketType.G_UPDATE_TEMPLATE_ORDER_RESULT);
                response.AddUInt8((byte)result);
                session.Send(response.ToArray());
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