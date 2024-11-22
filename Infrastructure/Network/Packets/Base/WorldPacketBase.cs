// File: Infrastructure/Network/Packets/Base/WorldPacketBase.cs
namespace PetitionD.Infrastructure.Network.Packets.Base;

using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Infrastructure.Network.Sessions;

public abstract class WorldPacketBase(PacketType packetType)
{
    public PacketType PacketType { get; } = packetType;

    public abstract void Handle(WorldSession session, Unpacker unpacker);
    public abstract byte[] Serialize();
}