// File: Infrastructure/Network/Packets/Petition/AddMemoPacket.cs
using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Core.Models;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Core.Models;

namespace PetitionD.Infrastructure.Network.Packets.Petition
{
    public class AddMemoPacket(
        ILogger<AddMemoPacket> logger,
        PetitionList petitionList,
        WorldSessionManager worldSessionManager) : GmPacketBase(PacketType.G_ADD_MEMO)
    {
        public override void Handle(GmSession session, Unpacker unpacker)
        {
            try
            {
                var petitionId = unpacker.GetInt32();
                var content = unpacker.GetStringMax(MaxLen.PetMemoLen);

                var petition = petitionList.GetPetition(petitionId);
                if (petition == null)
                {
                    SendResponse(session, petitionId, PetitionErrorCode.UnexpectedPetitionId);
                    return;
                }

                var worldSession = worldSessionManager.GetSession(petition.mWorldId);
                if (worldSession == null)
                {
                    SendResponse(session, petitionId, PetitionErrorCode.WorldDown);
                    return;
                }

                var gmCharacter = session.GetCharacter(petition.mWorldId);
                var result = petition.AddMemo(gmCharacter, content);

                SendResponse(session, petitionId, result);

                if (result == PetitionErrorCode.Success)
                {
                    var notification = new Packer((byte)PacketType.G_NOTIFY_NEW_MEMO);
                    notification.AddInt32(petitionId);
                    notification.AddString(petition.mCheckOutGm.CharName);
                    notification.AddDateTime(DateTime.Now);
                    notification.AddString(content);
                    worldSession.BroadcastToGmExcept(notification.ToArray(), session);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling memo addition");
            }
        }

        private static void SendResponse(GmSession session, int petitionId, PetitionErrorCode errorCode)
        {
            var response = new Packer((byte)PacketType.G_ACCEPT_NEW_MEMO);
            response.AddInt32(petitionId);
            response.AddDateTime(DateTime.Now);
            response.AddUInt8((byte)errorCode);
            session.Send(response.ToArray());
        }

        public override byte[] Serialize() =>
            new Packer((byte)PacketType.G_ADD_MEMO).ToArray();
    }
}