// File: Infrastructure/Network/Packets/Base/WorldPacketBase.cs
using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Infrastructure.Network.Sessions;
using Microsoft.Extensions.Logging;

namespace PetitionD.Infrastructure.Network.Packets.Base
{
    public abstract class WorldPacketBase(PacketType packetType, ILogger logger) : PacketBase(packetType)
    {
        protected readonly ILogger Logger = logger;

        public abstract void Handle(WorldSession session, Unpacker unpacker);
    }
}