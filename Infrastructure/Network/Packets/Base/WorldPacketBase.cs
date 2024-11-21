using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Infrastructure.Network.Sessions;
using PetitionD.Core.Models;

namespace PetitionD.Infrastructure.Network.Packets.Base;

public abstract class WorldPacketBase
{
    public PacketType PacketType { get; }

    protected WorldPacketBase(PacketType packetType)
    {
        PacketType = packetType;
    }

    public abstract void Handle(WorldSession session, Unpacker unpacker);
    public abstract byte[] Serialize();
}