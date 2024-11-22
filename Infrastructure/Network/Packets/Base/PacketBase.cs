using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetitionD.Infrastructure.Network.Packets.Base
{
    public abstract class PacketBase
    {
        public PacketType PacketType { get; }

        protected PacketBase(PacketType packetType)
        {
            PacketType = packetType;
        }

        public abstract byte[] Serialize();
    }
}
