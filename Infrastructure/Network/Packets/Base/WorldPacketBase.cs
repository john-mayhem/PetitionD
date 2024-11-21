// File: Infrastructure/Network/Packets/Base/WorldPacketBase.cs
using NC.PetitionLib;
using NC.ToolNet.Net;

namespace PetidionD.Infrastructure.Network.Packets.Base;

public abstract class WorldPacketBase(PacketType packetType)
{
    public PacketType PacketType { get; } = packetType;

    public abstract void Handle(WorldSession session, Unpacker unpacker);
    public abstract byte[] Serialize();
}