// File: Infrastructure/Network/Packets/Petition/ModifyCategoryPacket.cs
using NC.PetitionLib;
using NC.ToolNet.Net;
using PetidionD.Core.Models;
using PetidionD.Infrastructure.Network.Packets.Base;

namespace PetidionD.Infrastructure.Network.Packets.Petition;

public class ModifyCategoryPacket(
    ILogger<ModifyCategoryPacket> logger,
    PetitionList petitionList,
    WorldSessionManager worldSessionManager) : GmPacketBase(PacketType.G_MODIFY_CATEGORY)
{
    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var petitionId = unpacker.GetInt32();
            var categoryId = unpacker.GetInt32();

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
            var result = petition.ModifyCategory(gmCharacter, categoryId);

            SendResponse(session, petitionId, result);

            if (result == PetitionErrorCode.Success)
            {
                var notification = new Packer((byte)PacketType.G_NOTIFY_MODIFIED_CATEGORY);
                notification.AddInt32(petitionId);
                notification.AddInt32(categoryId);
                worldSession.BroadcastToGmExcept(notification.ToArray(), session);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling category modification");
        }
    }

    private static void SendResponse(GmSession session, int petitionId, PetitionErrorCode errorCode)
    {
        var response = new Packer((byte)PacketType.G_ACCEPT_MODIFY_CATEGORY);
        response.AddInt32(petitionId);
        response.AddUInt8((byte)errorCode);
        session.Send(response.ToArray());
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_MODIFY_CATEGORY).ToArray();
}