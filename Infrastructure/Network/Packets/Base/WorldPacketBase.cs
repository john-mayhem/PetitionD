// File: Infrastructure/Network/Packets/Base/WorldPacketBase.cs
namespace PetitionD.Infrastructure.Network.Packets.Base;
using NC.PetitionLib;
using NC.ToolNet.Networking.Protocol;
using PetitionD.Infrastructure.Network.Sessions;
using Microsoft.Extensions.Logging;



public abstract class WorldPacketBase(PacketType packetType, ILogger logger) : PacketBase(packetType)
{
    protected readonly ILogger Logger = logger;

    public abstract void Handle(WorldSession session, Unpacker unpacker);
}