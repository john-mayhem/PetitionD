using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Core.Models;


namespace PetitionD.Infrastructure.Network.Packets.PetitionHandlers;

public class CheckOutPetitionPacket(
    ILogger<CheckOutPetitionPacket> logger,
    PetitionList petitionList,
    WorldSessionManager worldSessionManager) : GmPacketBase(PacketType.G_CHECK_OUT_PETITION)
{
    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var petitionId = unpacker.GetInt32();
            var petition = petitionList.GetPetition(petitionId);

            if (petition == null)
            {
                SendResponse(session, petitionId, DateTime.Now, PetitionErrorCode.UnexpectedPetitionId);
                return;
            }

            var worldSession = worldSessionManager.GetSession(petition.WorldId);
            if (worldSession == null)
            {
                SendResponse(session, petitionId, DateTime.Now, PetitionErrorCode.WorldDown);
                return;
            }

            var gmCharacter = session.GetCharacter(petition.WorldId);
            var result = petition.CheckOut(gmCharacter, out bool unAssigned);

            SendResponse(session, petitionId, petition.CheckOutTime, result);

            if (result == PetitionErrorCode.Success)
            {
                // Notify world server
                var worldResponse = new Packer((byte)PacketType.W_NOTIFY_CHECK_OUT);
                worldResponse.AddInt32(petitionId);
                worldResponse.AddString(petition.CheckOutGm.CharName, MaxLen.CharName);
                worldResponse.AddString(petition.User.CharName, MaxLen.CharName);
                worldResponse.AddInt32(petition.CheckOutGm.CharUid);
                worldResponse.AddInt32(petition.User.CharUid);
                worldSession.Send(worldResponse.ToArray());

                // Notify other GMs
                var gmNotification = new Packer((byte)PacketType.G_NOTIFY_CHECK_OUT);
                gmNotification.AddInt32(petitionId);
                gmNotification.AddString(petition.CheckOutGm.CharName);
                gmNotification.AddDateTime(petition.CheckOutTime);
                worldSession.BroadcastToGmExcept(gmNotification.ToArray(), session);

                if (unAssigned)
                {
                    NotifyUnassignment(worldSession, petition);
                }

                // Handle reassignments
                var reassignedPetitions = AssignLogic.Assign(petition.WorldId);
                foreach (var reassigned in reassignedPetitions)
                {
                    NotifyReassignment(worldSession, reassigned);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling petition check-out");
        }
    }

    private static void NotifyUnassignment(WorldSession worldSession, Petition petition)
    {
        var notification = new Packer((byte)PacketType.G_NOTIFY_UNASSIGN);
        notification.AddInt32(petition.PetitionId);
        worldSession.BroadcastToGm(notification.ToArray());
    }

    private static void NotifyReassignment(WorldSession worldSession, Petition petition)
    {
        var notification = new Packer((byte)PacketType.G_NOTIFY_ASSIGN);
        notification.AddInt32(petition.PetitionId);
        notification.AddString(petition.AssignedGm.CharName);
        notification.AddInt32(petition.AssignedGm.CharUid);
        worldSession.BroadcastToGm(notification.ToArray());
    }

    private static void SendResponse(GmSession session, int petitionId, DateTime checkOutTime, PetitionErrorCode errorCode)
    {
        var response = new Packer((byte)PacketType.G_ACCEPT_CHECK_OUT);
        response.AddInt32(petitionId);
        response.AddDateTime(checkOutTime);
        response.AddUInt8((byte)errorCode);
        session.Send(response.ToArray());
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_CHECK_OUT_PETITION).ToArray();
}
