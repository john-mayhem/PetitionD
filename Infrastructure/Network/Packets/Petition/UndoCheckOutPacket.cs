﻿using NC.PetitionLib;
using NC.ToolNet.Net;
using PetidionD.Infrastructure.Network.Packets.Base;
using PetitionD.Configuration;

namespace PetidionD.Infrastructure.Network.Packets.Petition;

public class UndoCheckOutPacket(
    ILogger<UndoCheckOutPacket> logger,
    PetitionList petitionList,
    WorldSessionManager worldSessionManager) : GmPacketBase(PacketType.G_UNDO_CHECK_OUT)
{
    private readonly ILogger<UndoCheckOutPacket> _logger = logger;
    private readonly PetitionList _petitionList = petitionList;
    private readonly WorldSessionManager _worldSessionManager = worldSessionManager;

    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var petitionId = unpacker.GetInt32();

            var petition = _petitionList.GetPetition(petitionId);
            if (petition == null)
            {
                SendResponse(session, petitionId, PetitionErrorCode.UnexpectedPetitionId);
                return;
            }

            var worldSession = _worldSessionManager.GetSession(petition.mWorldId);
            if (worldSession == null)
            {
                SendResponse(session, petitionId, PetitionErrorCode.WorldDown);
                return;
            }

            var gmCharacter = session.GetCharacter(petition.mWorldId);
            var result = petition.UndoCheckOut(gmCharacter);

            SendResponse(session, petitionId, result);

            if (result == PetitionErrorCode.Success)
            {
                // Notify World Server
                var worldResponse = new Packer((byte)PacketType.W_NOTIFY_FINISH2);
                worldResponse.AddInt32(petitionId);
                worldResponse.AddUInt8((byte)Config.mMaxQuota);
                worldResponse.AddUInt8((byte)petition.mQuotaAfterTreat);
                worldResponse.AddUInt8((byte)result);
                worldSession.Send(worldResponse.ToArray());

                // Notify other GMs
                var notification = new Packer((byte)PacketType.G_NOTIFY_UNDO);
                notification.AddInt32(petitionId);
                notification.AddString(gmCharacter.CharName);
                notification.AddDateTime(DateTime.Now);
                worldSession.BroadcastToGmExcept(notification.ToArray(), session);

                // Handle reassignments
                var reassignedPetitions = AssignLogic.Assign(petition.mWorldId);
                foreach (var reassigned in reassignedPetitions)
                {
                    NotifyReassignment(worldSession, reassigned);
                }

                _logger.LogInformation("Petition {PetitionId} check-out undone by GM {GmName}",
                    petitionId, gmCharacter.CharName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling undo check-out");
        }
    }

    private static void SendResponse(GmSession session, int petitionId, PetitionErrorCode errorCode)
    {
        var response = new Packer((byte)PacketType.G_ACCEPT_UNDO);
        response.AddInt32(petitionId);
        response.AddDateTime(DateTime.Now);
        response.AddUInt8((byte)errorCode);
        session.Send(response.ToArray());
    }

    private static void NotifyReassignment(WorldSession worldSession, Petition petition)
    {
        var notification = new Packer((byte)PacketType.G_NOTIFY_ASSIGN);
        notification.AddInt32(petition.mPetitionId);
        notification.AddString(petition.mAssignedGm.CharName);
        notification.AddInt32(petition.mAssignedGm.CharUid);
        worldSession.BroadcastToGm(notification.ToArray());
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_UNDO_CHECK_OUT).ToArray();
}