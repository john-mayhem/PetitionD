// File: Infrastructure/Network/Packets/Chat/ChattingCheckInPacket.cs
using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Core.Models;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Core.Models;

namespace PetitionD.Infrastructure.Network.Packets.Chat;

public class ChattingCheckInPacket(
    ILogger<ChattingCheckInPacket> logger,
    PetitionList petitionList,
    WorldSessionManager worldSessionManager) : GmPacketBase(PacketType.G_CHATTING_CHECK_IN)
{
    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var petitionId = unpacker.GetInt32();
            var flag = unpacker.GetUInt8();
            var message = unpacker.GetString(); // Optional message

            var petition = petitionList.GetPetition(petitionId);
            if (petition == null)
            {
                SendResponse(session, petitionId, PetitionErrorCode.UnexpectedPetitionId);
                return;
            }

            var gmCharacter = session.GetCharacter(petition.mWorldId);
            var result = petition.ChattingCheckIn(gmCharacter, flag);

            SendResponse(session, petitionId, result);

            if (result == PetitionErrorCode.Success)
            {
                NotifyOtherGms(session, petition);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling chat check-in");
        }
    }

    private static void SendResponse(GmSession session, int petitionId, PetitionErrorCode errorCode)
    {
        var response = new Packer((byte)PacketType.G_ACCEPT_CHECK_IN);
        response.AddInt32(petitionId);
        response.AddDateTime(DateTime.Now);
        response.AddUInt8((byte)errorCode);
        session.Send(response.ToArray());
    }

    private static void NotifyOtherGms(GmSession session, Petition petition)
    {
        var notification = new Packer((byte)PacketType.G_NOTIFY_FINISH);
        notification.AddInt32(petition.mPetitionId);
        session.Send(notification.ToArray());
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_CHATTING_CHECK_IN).ToArray();
}
