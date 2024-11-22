// File: Infrastructure/Network/Packets/Base/WorldPacketBase.cs
namespace PetitionD.Infrastructure.Network.Packets.Base;

using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Infrastructure.Network.Sessions;


public abstract class WorldPacketBase : PacketBase
{
    protected WorldPacketBase(PacketType packetType) : base(packetType) { }
    public abstract void Handle(WorldSession session, Unpacker unpacker);
}