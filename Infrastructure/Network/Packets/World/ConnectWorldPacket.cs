// File: Infrastructure/Network/Packets/World/ConnectWorldPacket.cs
using Microsoft.Extensions.Logging;
using NC.PetitionLib;
using NC.ToolNet.Networking.Protocol;
using PetitionD.Core.Models;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Infrastructure.Network.Sessions;

namespace PetitionD.Infrastructure.Network.Packets.World
{
    public class ConnectWorldPacket : WorldPacketBase
    {
        private readonly WorldSessionManager _worldSessionManager;
        private readonly PetitionList _petitionList;

        public ConnectWorldPacket(
            ILogger<ConnectWorldPacket> logger,
            WorldSessionManager worldSessionManager,
            PetitionList petitionList)
            : base(PacketType.W_CONNECT_WORLD2, logger)
        {
            _worldSessionManager = worldSessionManager;
            _petitionList = petitionList;
        }

        public override void Handle(WorldSession worldSession, Unpacker unpacker)
        {
            try
            {
                var buildNumber = unpacker.GetInt32();
                var serviceBuildNumber = unpacker.GetUInt8();
                var worldId = unpacker.GetUInt8();
                var worldName = unpacker.GetShortStringMax(MaxLen.WorldName);
                var maxPlayer = unpacker.GetUInt8();
                var oneTimeKeyResponse = unpacker.GetBytes(16);

                if (_worldSessionManager.GetSession(worldId) != null)
                {
                    SendResponse(worldSession, PetitionErrorCode.WorldAlreadyConnected);
                    worldSession.Stop();
                    return;
                }

                worldSession.WorldId = worldId;
                worldSession.WorldName = worldName;
                worldSession.State = WorldSessionState.Connected;
                worldSession.LastOnlineCheckTime = DateTime.Now;

                _worldSessionManager.AddSession(worldSession);

                SendResponse(worldSession, PetitionErrorCode.Success);

                Logger.LogInformation("World {WorldId} ({WorldName}) connected", worldId, worldName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error handling world connection");
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
}