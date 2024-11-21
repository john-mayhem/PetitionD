// File: Infrastructure/Network/Packets/Base/GmPacketBase.cs
using NC.PetitionLib;
using PetitionD.Core.Models;
using NC.ToolNet.Net;

public abstract class GmPacketBase(PacketType packetType)
{
    public PacketType PacketType { get; } = packetType;

    public abstract void Handle(GmSession session, Unpacker unpacker);
    public abstract byte[] Serialize();
}