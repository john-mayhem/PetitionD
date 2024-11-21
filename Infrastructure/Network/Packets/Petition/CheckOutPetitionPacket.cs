using NC.PetitionLib;
using NC.ToolNet.Net;
using PetidionD.Infrastructure.Network.Packets.Base;

namespace PetidionD.Infrastructure.Network.Packets.Petition;

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

            var worldSession = worldSessionManager.GetSession(petition.mWorldId);
            if (worldSession == null)
            {
                SendResponse(session, petitionId, DateTime.Now, PetitionErrorCode.WorldDown);
                return;
            }

            var gmCharacter = session.GetCharacter(petition.mWorldId);
            bool unAssigned;
            var result = petition.CheckOut(gmCharacter, out unAssigned);

            SendResponse(session, petitionId, petition.mCheckOutTime, result);

            if (result == PetitionErrorCode.Success)
            {
                // Notify world server
                var worldResponse = new Packer((byte)PacketType.W_NOTIFY_CHECK_OUT);
                worldResponse.AddInt32(petitionId);
                worldResponse.AddString(petition.mCheckOutGm.CharName, MaxLen.CharName);
                worldResponse.AddString(petition.mUser.CharName, MaxLen.CharName);
                worldResponse.AddInt32(petition.mCheckOutGm.CharUid);
                worldResponse.AddInt32(petition.mUser.CharUid);
                worldSession.Send(worldResponse.ToArray());

                // Notify other GMs
                var gmNotification = new Packer((byte)PacketType.G_NOTIFY_CHECK_OUT);
                gmNotification.AddInt32(petitionId);
                gmNotification.AddString(petition.mCheckOutGm.CharName);
                gmNotification.AddDateTime(petition.mCheckOutTime);
                worldSession.BroadcastToGmExcept(gmNotification.ToArray(), session);

                if (unAssigned)
                {
                    NotifyUnassignment(worldSession, petition);
                }

                // Handle reassignments
                var reassignedPetitions = AssignLogic.Assign(petition.mWorldId);
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
        notification.AddInt32(petition.mPetitionId);
        worldSession.BroadcastToGm(notification.ToArray());
    }

    private static void NotifyReassignment(WorldSession worldSession, Petition petition)
    {
        var notification = new Packer((byte)PacketType.G_NOTIFY_ASSIGN);
        notification.AddInt32(petition.mPetitionId);
        notification.AddString(petition.mAssignedGm.CharName);
        notification.AddInt32(petition.mAssignedGm.CharUid);
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