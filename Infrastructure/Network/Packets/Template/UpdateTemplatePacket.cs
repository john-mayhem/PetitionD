// File: Infrastructure/Network/Packets/Template/UpdateTemplatePacket.cs
using Microsoft.Extensions.Logging;
using NC.PetitionLib;
using NC.ToolNet.Networking.Protocol;
using PetitionD.Core.Models;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Infrastructure.Network.Sessions;

namespace PetitionD.Infrastructure.Network.Packets.Template
{
    public class UpdateTemplatePacket : GmPacketBase
    {
        private readonly ILogger<UpdateTemplatePacket> _logger;

        public UpdateTemplatePacket(ILogger<UpdateTemplatePacket> logger)
            : base(PacketType.G_UPDATE_TEMPLATE)
        {
            _logger = logger;
        }

        public override void Handle(GmSession session, Unpacker unpacker)
        {
            try
            {
                var code = unpacker.GetInt32();
                var name = unpacker.GetStringMax(MaxLen.TemplateName);
                var templateType = (PetitionD.Core.Models.Template.TemplateType)unpacker.GetUInt8();  // Fully qualified
                var content = unpacker.GetStringMax(MaxLen.TemplateContent);
                var order = unpacker.GetInt32();

                var result = PetitionD.Core.Models.Template.Operations.Update(  // Fully qualified
                    session.AccountUid,
                    session.Account,
                    code,
                    name,
                    templateType,
                    content,
                    order,
                    out int resultCode);

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
}