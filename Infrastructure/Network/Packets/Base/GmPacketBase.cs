// File: Infrastructure/Network/Packets/Base/GmPacketBase.cs
namespace PetitionD.Infrastructure.Network.Packets.Base;

using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Infrastructure.Network.Sessions;

public abstract class GmPacketBase(PacketType packetType)
{
    public PacketType PacketType { get; } = packetType;

    public abstract void Handle(GmSession session, Unpacker unpacker);
    public abstract byte[] Serialize();
}