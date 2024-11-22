// File: Infrastructure/Network/Packets/Chat/MessagingCheckInPacket.cs
namespace PetitionD.Infrastructure.Network.Packets.Chat;

using Microsoft.Extensions.Logging;
using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Core.Models;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Infrastructure.Network.Sessions;

public class MessagingCheckInPacket(
    ILogger<MessagingCheckInPacket> logger,
    PetitionList petitionList) : GmPacketBase(PacketType.G_MESSAGING_CHECK_IN)
{
    private readonly ILogger<MessagingCheckInPacket> _logger = logger;
    private readonly PetitionList _petitionList = petitionList;

    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var petitionId = unpacker.GetInt32();
            var flag = unpacker.GetUInt8();
            var message = unpacker.GetStringMax(MaxLen.PetMessage);

            var petition = _petitionList.GetPetition(petitionId);
            if (petition == null)
            {
                SendResponse(session, petitionId, PetitionErrorCode.UnexpectedPetitionId);
                return;
            }

            var gmCharacter = session.GetCharacter(petition.WorldId);
            if (gmCharacter == null)
            {
                SendResponse(session, petitionId, PetitionErrorCode.NoRightToAccess);
                return;
            }

            var result = petition.BeginMessageCheckIn(gmCharacter, message, flag);

            if (result == PetitionErrorCode.Success)
            {
                var worldResponse = new Packer((byte)PacketType.W_LEAVE_MESSAGE);
                worldResponse.AddInt32(petitionId);
                worldResponse.AddString(message);
                // TODO: Send to world session when WorldSessionManager is implemented
            }
            else
            {
                SendResponse(session, petitionId, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling messaging check-in");
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