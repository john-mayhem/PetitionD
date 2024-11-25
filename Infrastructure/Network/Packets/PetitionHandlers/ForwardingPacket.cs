// File: Infrastructure/Network/Packets/Petition/ForwardingPacket.cs
using NC.PetitionLib;
using NC.ToolNet.Networking.Protocol;
using PetitionD.Core.Models;
using PetitionD.Infrastructure.Network.Packets.Base;

namespace PetitionD.Infrastructure.Network.Packets.PetitionHandlers
{
    public class ForwardingPacket(
        ILogger<ForwardingPacket> logger,
        PetitionList petitionList,
        WorldSessionManager worldSessionManager) : GmPacketBase(PacketType.G_FORWARDING)
    {
        public override void Handle(GmSession session, Unpacker unpacker)
        {
            try
            {
                var petitionId = unpacker.GetInt32();
                var flag = unpacker.GetUInt8();
                var newGrade = (Grade)unpacker.GetUInt8();

                var petition = petitionList.GetPetition(petitionId);
                if (petition == null)
                {
                    SendResponse(session, petitionId, PetitionErrorCode.UnexpectedPetitionId);
                    return;
                }

                var gmCharacter = session.GetCharacter(petition.WorldId);
                var result = petition.ForwardCheckIn(gmCharacter, newGrade, flag);

                SendResponse(session, petitionId, result);

                if (result == PetitionErrorCode.Success)
                {
                    var notification = new Packer((byte)PacketType.G_NOTIFY_FORWARDING);
                    notification.AddInt32(petitionId);
                    notification.AddUInt8(flag);
                    notification.AddString(gmCharacter.CharName);
                    notification.AddDateTime(DateTime.Now);
                    notification.AddUInt8((byte)newGrade);
                    worldSessionManager.GetSession(petition.WorldId)?.BroadcastToGmExcept(notification.ToArray(), session);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling petition forwarding");
            }
        }

        private static void SendResponse(GmSession session, int petitionId, PetitionErrorCode errorCode)
        {
            var response = new Packer((byte)PacketType.G_ACCEPT_FORWARDING);
            response.AddInt32(petitionId);
            response.AddDateTime(DateTime.Now);
            response.AddUInt8((byte)errorCode);
            session.Send(response.ToArray());
        }

        public override byte[] Serialize() =>
            new Packer((byte)PacketType.G_FORWARDING).ToArray();
    }
}