using NC.PetitionLib;
using NC.ToolNet.Networking.Protocol;
using PetitionD.Core.Models;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Configuration;
using PetitionD.Core.Services;
using System.Text;

namespace PetitionD.Infrastructure.Network.Packets.World;

public class SubmitPetitionPacket : WorldPacketBase
{
    private readonly ILogger<SubmitPetitionPacket> _logger;
    private readonly PetitionList _petitionList;

    public SubmitPetitionPacket(
        ILogger<SubmitPetitionPacket> logger,
        PetitionList petitionList) : base(PacketType.W_SUBMIT_PETITION6, logger)
    {
        _logger = logger;
        _petitionList = petitionList;
    }

    public override void Handle(WorldSession session, Unpacker unpacker)
    {
        try
        {
            var requestId = unpacker.GetInt32();
            var category = unpacker.GetUInt8();

            _logger.LogTrace("Received petition submission - RequestId: {RequestId}, Category: {Category}",
                requestId, category);

            // Get user info
            var accountName = unpacker.GetASCIIString(MaxLen.Account);
            var accountUid = unpacker.GetInt32();
            var charName = unpacker.GetString(MaxLen.CharName);
            var charUid = unpacker.GetInt32();

            _logger.LogDebug("User info - Account: {Account}({Uid}), Char: {Char}({CharUid})",
                accountName, accountUid, charName, charUid);

            var user = new GameCharacter
            {
                AccountName = accountName,
                AccountUid = accountUid,
                CharName = charName,
                CharUid = charUid,
                WorldId = session.WorldId
            };

            // Content parsing - this is where the issue was
            var contentLength = unpacker.GetUInt8();
            _logger.LogTrace("Content length from packet: {Length}", contentLength);

            var content = unpacker.GetString(contentLength); // This reads Unicode string properly

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Empty petition content received after decode. Length was: {Length}", contentLength);
                SendErrorResponse(session, requestId, PetitionErrorCode.UnexpectedPetitionId,
                    new GameCharacter(), user);
                return;
            }

            _logger.LogDebug("Petition content: {Content}", content);

            // Forced GM info
            var forcedGm = new GameCharacter
            {
                AccountName = unpacker.GetASCIIString(MaxLen.Account),
                AccountUid = unpacker.GetInt32(),
                CharName = unpacker.GetString(MaxLen.CharName),
                CharUid = unpacker.GetInt32(),
                WorldId = session.WorldId
            };

            // L2 Info
            var info = new Lineage2Info();
            info.Unpack(unpacker);

            _logger.LogInformation("Processing petition - User: {User}, Category: {Category}, Content: {Content}",
                user.CharName, category, content);

            var result = _petitionList.CreatePetition(
                session.WorldId,
                category,
                user,
                content,
                forcedGm,
                info,
                out var newPetition);

            if (result == PetitionErrorCode.Success)
            {
                _logger.LogInformation("Petition created successfully - ID: {Id}", newPetition.PetitionId);
                SendSuccessResponse(session, requestId, newPetition);
                NotifyGms(session, newPetition);
            }
            else
            {
                _logger.LogWarning("Petition creation failed - Error: {Error}", result);
                SendErrorResponse(session, requestId, result, forcedGm, user);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling petition submission");
            SendErrorResponse(session, 0, PetitionErrorCode.InternalServerFail,
                new GameCharacter(), new GameCharacter());
        }
    }

    private bool IsValidCategory(byte category)
    {
        // Based on your packet captures, let's map these correctly:
        return category switch
        {
            1 => true,   // Immobility
            2 => true,   // Recovery related
            3 => true,   // Bug report
            4 => true,   // Quest related
            5 => true,   // Suggestions
            6 => true,   // Report a bad user
            8 => true,   // Recovery related
            9 => true,   // Operation related
            72 => true,  // Special category from WorldServer
            255 => true, // Other
            _ => false
        };
    }

    private static void SendSuccessResponse(WorldSession session, int requestId, Petition petition)
    {
        var response = new Packer((byte)PacketType.W_SUBMIT_PETITION_OK4);
        response.AddInt32(requestId);
        response.AddInt32(petition.PetitionId);
        response.AddASCIIString(petition.PetitionSeq, MaxLen.PetitionSeq);
        response.AddUInt16((ushort)1);  // Active petition count
        response.AddString(petition.ForcedGm.CharName, MaxLen.CharName);
        response.AddInt32(petition.ForcedGm.CharUid);
        response.AddUInt8((byte)Config.MaxQuota);
        response.AddUInt8((byte)(Config.MaxQuota - petition.QuotaAtSubmit - 1));
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
        response.AddUInt8((byte)Config.MaxQuota);
        response.AddUInt8(0);  // Current quota
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