﻿using NC.PetitionLib;
using NC.ToolNet.Net;
using PetidionD.Core.Models;
using PetidionD.Infrastructure.Network.Packets.Base;
using PetitionD.Configuration;
using PetidionD.Core.Services; // Add this for AssignLogic

namespace PetidionD.Infrastructure.Network.Packets.World;

public class CancelPetitionPacket(
    ILogger<CancelPetitionPacket> logger,
    PetitionList petitionList) : WorldPacketBase(PacketType.W_CANCEL_PETITION3)  // Changed base class
{
    public override void Handle(WorldSession worldSession, Unpacker unpacker)
    {
        try
        {
            var requesterName = unpacker.GetString(MaxLen.CharName);
            var requesterCharUid = unpacker.GetInt32();
            var userName = unpacker.GetString(MaxLen.CharName);
            var userCharUid = unpacker.GetInt32();

            var petition = petitionList.GetPetition(worldSession.WorldId, userCharUid);
            if (petition == null)
            {
                SendErrorResponse(worldSession, requesterName, requesterCharUid,
                    PetitionErrorCode.CharNoPetition, null, userName, userCharUid);
                return;
            }

            var result = petition.CancelPetition(worldSession.WorldId, requesterCharUid);

            if (result == PetitionErrorCode.Success)
            {
                petitionList.RemoveActivePetition(petition.mPetitionId);
                SendSuccessResponse(worldSession, requesterName, requesterCharUid,
                    userName, userCharUid, petition);

                // Notify GMs
                NotifyGmsPetitionCancelled(worldSession, petition);

                // Handle reassignments if necessary
                var reassignedPetitions = AssignLogic.Assign(petition.mWorldId);
                foreach (var reassigned in reassignedPetitions)
                {
                    NotifyGmsPetitionReassigned(worldSession, reassigned);
                }
            }
            else
            {
                SendErrorResponse(worldSession, requesterName, requesterCharUid,
                    result, petition.mForcedGm, userName, userCharUid);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling petition cancellation");
        }
    }

    private static void SendSuccessResponse(
        WorldSession session,
        string requesterName,
        int requesterCharUid,
        string userName,
        int userCharUid,
        Petition petition)
    {
        var response = new Packer((byte)PacketType.W_CANCEL_PETITION_OK3);
        response.AddString(requesterName, MaxLen.CharName);
        response.AddInt32(requesterCharUid);
        response.AddString(userName, MaxLen.CharName);
        response.AddInt32(userCharUid);
        response.AddInt32(petition.mPetitionId);
        response.AddASCIIString(petition.mPetitionSeq, MaxLen.PetitionSeq);
        response.AddString(petition.mForcedGm.CharName, MaxLen.CharName);
        response.AddInt32(petition.mForcedGm.CharUid);
        response.AddUInt8((byte)Config.mMaxQuota);
        response.AddUInt8((byte)(Config.mMaxQuota - petition.mQuotaAfterTreat));
        session.Send(response.ToArray());
    }

    private static void SendErrorResponse(
        WorldSession session,
        string requesterName,
    int requesterCharUid,
        PetitionErrorCode errorCode,
        GameCharacter? forcedGm,
        string userName,
        int userCharUid)
    {
        var response = new Packer((byte)PacketType.W_CANCEL_PETITION_FAIL3);
        response.AddString(requesterName, MaxLen.CharName);
        response.AddInt32(requesterCharUid);
        response.AddUInt8((byte)errorCode);
        response.AddString(userName, MaxLen.CharName);
        response.AddInt32(userCharUid);
        response.AddString(forcedGm?.CharName ?? string.Empty, MaxLen.CharName);
        response.AddInt32(forcedGm?.CharUid ?? 0);
        response.AddUInt8((byte)Config.mMaxQuota);
        response.AddUInt8(0); // Current quota
        session.Send(response.ToArray());
    }

    private static void NotifyGmsPetitionCancelled(WorldSession session, Petition petition)
    {
        var notification = new Packer((byte)PacketType.G_NOTIFY_FINISH);
        notification.AddInt32(petition.mPetitionId);
        session.BroadcastToGm(notification.ToArray());
    }

    private static void NotifyGmsPetitionReassigned(WorldSession session, Petition petition)
    {
        var notification = new Packer((byte)PacketType.G_NOTIFY_ASSIGN);
        notification.AddInt32(petition.mPetitionId);
        notification.AddString(petition.mAssignedGm.CharName);
        notification.AddInt32(petition.mAssignedGm.CharUid);
        session.BroadcastToGm(notification.ToArray());
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.W_CANCEL_PETITION3).ToArray();
}