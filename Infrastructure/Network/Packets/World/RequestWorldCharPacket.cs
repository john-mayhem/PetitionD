// File: Infrastructure/Network/Packets/World/RequestWorldCharPacket.cs
using NC.PetitionLib;
using NC.ToolNet.Networking.Protocol;
using PetitionD.Infrastructure.Network.Packets.Base;

namespace PetitionD.Infrastructure.Network.Packets.World;

public class RequestWorldCharPacket(
    ILogger<RequestWorldCharPacket> logger,
    ISessionManager sessionManager) : GmPacketBase(PacketType.G_REQUEST_WORLD_CHAR)
{
    private readonly ILogger<RequestWorldCharPacket> _logger = logger;
    private readonly ISessionManager _sessionManager = sessionManager;

    public override void Handle(GmSession session, Unpacker unpacker)
    {
        try
        {
            var worldId = unpacker.GetInt32();

            // Send character list request to world server
            var packer = new Packer((byte)PacketType.W_REQUEST_CHAR_LIST2);
            packer.AddInt32(session.AccountUid);

            // TODO: Send to world server when implemented
            _logger.LogInformation("Requesting characters for world {WorldId}", worldId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling world character request");
        }
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.G_REQUEST_WORLD_CHAR).ToArray();
}