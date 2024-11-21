// File: Infrastructure/Network/Packets/World/ConnectWorldPacket.cs
using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Core.Models;

namespace PetitionD.Infrastructure.Network.Packets.World;

public class ConnectWorldPacket(
    ILogger<ConnectWorldPacket> logger,
    WorldSessionManager worldSessionManager,
    PetitionList petitionList) : WorldPacketBase(PacketType.W_CONNECT_WORLD2)
{
    public override void Handle(WorldSession worldSession, Unpacker unpacker)
    {
        try
        {
            unpacker.GetInt32(); // buildNumber
            var serviceBuildNumber = unpacker.GetUInt8();
            var worldId = unpacker.GetUInt8();
            var worldName = unpacker.GetShortStringMax(MaxLen.WorldName);
            var maxPlayer = unpacker.GetUInt8();
            var oneTimeKeyResponse = unpacker.GetBytes(16);

            if (worldSessionManager.GetSession(worldId) != null)
            {
                SendResponse(worldSession, PetitionErrorCode.WorldAlreadyConnected);
                worldSession.Stop();
                return;
            }

            worldSession.WorldId = worldId;
            worldSession.WorldName = worldName;
            worldSession.State = WorldSessionState.Connected;
            worldSession.LastOnlineCheckTime = DateTime.Now;

            var result = worldSessionManager.AddSession(worldSession);

            if (result != PetitionErrorCode.Success)
            {
                SendResponse(worldSession, result);
                return;
            }

            // Load existing petitions for this world
            var activePetitions = petitionList.GetActivePetitionCount(worldId);
            logger.LogInformation("{Count} petitions loaded for world {WorldId}",
                activePetitions, worldId);

            SendResponse(worldSession, PetitionErrorCode.Success);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling world connection");
            worldSession.Stop();
        }
    }

    private static void SendResponse(WorldSession session, PetitionErrorCode errorCode)
    {
        var response = new Packer((byte)PacketType.W_ACCEPT_WORLD);
        response.AddUInt8((byte)errorCode);
        session.Send(response.ToArray());
    }

    public override byte[] Serialize() =>
        new Packer((byte)PacketType.W_CONNECT_WORLD2).ToArray();
}
