﻿using NC.PetitionLib;
using NC.ToolNet.Net;
using PetidionD.Core.Models;
using PetidionD.Infrastructure.Network.Packets.Base;
using PetitionD.Configuration;

namespace PetidionD.Infrastructure.Network.Packets.World;

public class SubmitPetitionPacket(
    ILogger<SubmitPetitionPacket> logger,
    PetitionList petitionList) : WorldPacketBase(PacketType.W_SUBMIT_PETITION6)
{
    public override void Handle(WorldSession worldSession, Unpacker unpacker)
    {
        try
        {
            var requestId = unpacker.GetInt32();
            var category = unpacker.GetUInt8();

            var user = new GameCharacter
            {
                AccountName = unpacker.GetASCIIString(MaxLen.Account),
                AccountUid = unpacker.GetInt32(),
                CharName = unpacker.GetString(MaxLen.CharName),
                CharUid = unpacker.GetInt32(),
                WorldId = worldSession.WorldId
            };

            var content = unpacker.GetShortStringMax(MaxLen.PetContent);

            var forcedGm = new GameCharacter
            {
                AccountName = unpacker.GetASCIIString(MaxLen.Account),
                AccountUid = unpacker.GetInt32(),
                CharName = unpacker.GetString(MaxLen.CharName),
                CharUid = unpacker.GetInt32(),
                WorldId = worldSession.WorldId
            };

            var info = new Lineage2Info();
            info.Unpack(unpacker);

            Petition newPetition;
            var result = petitionList.CreatePetition(
                worldSession.WorldId,
                category,
                user,
                content,
                forcedGm,
                info,
                out newPetition);

            if (result == PetitionErrorCode.Success)
            {
                SendSuccessResponse(worldSession, requestId, newPetition);
                NotifyGms(worldSession, newPetition);
            }
            else
            {
                SendErrorResponse(worldSession, requestId, result, forcedGm, user);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling petition submission");
        }
    }

    private static void SendSuccessResponse(WorldSession session, int requestId, Petition petition)
    {
        var response = new Packer((byte)PacketType.W_SUBMIT_PETITION_OK4);
        response.AddInt32(requestId);
        response.AddInt32(petition.mPetitionId);
        response.AddASCIIString(petition.mPetitionSeq, MaxLen.PetitionSeq);
        response.AddUInt16((ushort)petition.GetActivePetitionCount());
        response.AddString(petition.mForcedGm.CharName, MaxLen.CharName);
        response.AddInt32(petition.mForcedGm.CharUid);
        response.AddUInt8((byte)Config.mMaxQuota);
        response.AddUInt8((byte)(Config.mMaxQuota - petition.mQuotaAtSubmit - 1));
        session.Send(response.ToArray());
    }

    private static void SendErrorResponse(
        WorldSession session,
    int requestId,
        PetitionErrorCode errorCode,
        GameCharacter forcedGm,
        GameCharacter user)
    {
        var response = new Packer((byte)PacketType.W_SUBMIT_PETITION_FAIL4);
        response.AddInt32(requestId);
        response.AddUInt8((byte)errorCode);
        response.AddString(forcedGm.CharName, MaxLen.CharName);
        response.AddInt32(forcedGm.CharUid);
        response.AddUInt8((byte)Config.mMaxQuota);
        response.AddUInt8((byte)Quota.GetCurrentQuota(user.AccountUid));
        session.Send(response.ToArray());
    }

    private static void NotifyGms(WorldSession session, Petition petition)
    {
        var notification = new Packer((byte)PacketType.G_NOTIFY_NEW_PETITION);
        petition.Serialize(notification);
        session.BroadcastToGm(notification.ToArray());
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.W_SUBMIT_PETITION6).ToArray();
}