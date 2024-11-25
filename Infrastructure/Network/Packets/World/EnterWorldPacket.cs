// File: Infrastructure/Network/Packets/World/EnterWorldPacket.cs
using NC.PetitionLib;
using NC.ToolNet.Networking.Protocol;
using PetitionD.Infrastructure.Network.Packets.Base;

namespace PetitionD.Infrastructure.Network.Packets.World;

public class EnterWorldPacket(
    ILogger<EnterWorldPacket> logger,
    ISessionManager sessionManager) : GmPacketBase(PacketType.G_ENTER_WORLD)
{
    private readonly ILogger<EnterWorldPacket> _logger = logger;
    private readonly ISessionManager _sessionManager = sessionManager;

    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var worldId = unpacker.GetInt32();
            var charUid = unpacker.GetInt32();

            if (!session.HasCharList(worldId))
            {
                session.AddTryLoginList(worldId, charUid);

                // Request character list from world
                var packer = new Packer((byte)PacketType.W_REQUEST_CHAR_LIST2);
                packer.AddInt32(session.AccountUid);

                // TODO: Send to world server when implemented
            }
            else
            {
                // Handle entering world with existing character list
                HandleEnterWorld(session, worldId, charUid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling enter world request");
        }
    }

    private static void HandleEnterWorld(GmSession session, int worldId, int charUid)
    {
        var result = session.EnterWorld(worldId, charUid);

        var packer = new Packer((byte)PacketType.G_ACCEPT_ENTERANCE);
        packer.AddInt32(worldId);
        packer.AddUInt8((byte)result);
        session.Send(packer.ToArray());
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_ENTER_WORLD).ToArray();
}