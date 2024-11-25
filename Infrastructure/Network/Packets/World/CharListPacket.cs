// File: Infrastructure/Network/Packets/World/CharListPacket.cs
using NC.PetitionLib;
using NC.ToolNet.Networking.Protocol;
using PetitionD.Core.Models;
using PetitionD.Infrastructure.Network.Packets.Base;

namespace PetitionD.Infrastructure.Network.Packets.World;

public class CharListPacket(
    ILogger<CharListPacket> logger,
    ISessionManager sessionManager) : WorldPacketBase(PacketType.W_CHAR_LIST2, logger)
{
    private readonly ISessionManager _sessionManager = sessionManager;

    public override void Handle(WorldSession worldSession, Unpacker unpacker)
    {
        try
        {
            var accountUid = unpacker.GetInt32();
            var charCount = unpacker.GetUInt8();

            var gmSession = ((IEnumerable<ISession>)_sessionManager
                .GetAllSessions())
                .OfType<GmSession>()
                .FirstOrDefault(s => s.AccountUid == accountUid);

            if (gmSession == null)
            {
                logger.LogWarning("Character list received for unknown GM account {AccountUid}", accountUid);
                return;
            }

            gmSession.ClearCharacter(worldSession.WorldId);

            // Create response packet
            var response = new Packer((byte)PacketType.G_WORLD_CHAR_LIST);
            response.AddInt32(worldSession.WorldId);
            response.AddInt32(charCount);

            for (int i = 0; i < charCount; i++)
            {
                var character = new GmCharacter
                {
                    WorldId = worldSession.WorldId,
                    AccountUid = accountUid,
                    CharName = unpacker.GetString(MaxLen.CharName),
                    CharUid = unpacker.GetInt32(),
                    Grade = (Grade)unpacker.GetUInt8()
                };

                gmSession.AddCharacter(character);

                response.AddString(character.CharName);
                response.AddInt32(character.CharUid);
                response.AddUInt8((byte)character.Grade);
            }

            gmSession.Send(response.ToArray());

            // Handle pending world entry if exists
            var tryLoginCharUid = gmSession.GetTryLoginCharUid(worldSession.WorldId);
            if (tryLoginCharUid != 0)
            {
                var result = gmSession.EnterWorld(worldSession.WorldId, tryLoginCharUid);
                var enterResponse = new Packer((byte)PacketType.G_ACCEPT_ENTERANCE);
                enterResponse.AddInt32(worldSession.WorldId);
                enterResponse.AddUInt8((byte)result);
                gmSession.Send(enterResponse.ToArray());
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling character list");
        }
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.W_CHAR_LIST2).ToArray();
}
