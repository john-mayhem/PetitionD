// File: Infrastructure/Network/Packets/Chat/MessagingCheckInPacket.cs
using NC.PetitionLib;
using NC.ToolNet.Net;
using PetidionD.Core.Models;

public class MessagingCheckInPacket(
    ILogger<MessagingCheckInPacket> logger,
    PetitionList petitionList) : GmPacketBase(PacketType.G_MESSAGING_CHECK_IN)
{
    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var petitionId = unpacker.GetInt32();
            var flag = unpacker.GetUInt8();
            var message = unpacker.GetStringMax(MaxLen.PetMessage);

            var petition = petitionList.GetPetition(petitionId);
            if (petition == null)
            {
                SendResponse(session, petitionId, PetitionErrorCode.UnexpectedPetitionId);
                return;
            }

            var gmCharacter = session.GetCharacter(petition.mWorldId);
            var result = petition.BeginMessageCheckIn(gmCharacter, message, flag);

            if (result == PetitionErrorCode.Success)
            {
                var worldResponse = new Packer((byte)PacketType.W_LEAVE_MESSAGE);
                worldResponse.AddInt32(petitionId);
                worldResponse.AddString(message);
                // Send to world session
            }
            else
            {
                SendResponse(session, petitionId, result);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling messaging check-in");
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

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_MESSAGING_CHECK_IN).ToArray();
}