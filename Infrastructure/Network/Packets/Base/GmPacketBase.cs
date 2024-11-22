// File: Infrastructure/Network/Packets/Base/GmPacketBase.cs

using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Infrastructure.Network.Sessions;

namespace PetitionD.Infrastructure.Network.Packets.Base;

public abstract class GmPacketBase : PacketBase
{
    protected GmPacketBase(PacketType packetType) : base(packetType) { }
    public abstract void Handle(GmSession session, Unpacker unpacker);
}