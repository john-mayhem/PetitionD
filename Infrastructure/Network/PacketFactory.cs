using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Infrastructure.Network.Packets.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetitionD.Infrastructure.Network
{
    // File: Infrastructure/Network/Packets/PacketFactory.cs
    public class PacketFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<PacketType, Type> _packetTypes = new();
        private readonly ILogger<PacketFactory> _logger;

        public PacketFactory(IServiceProvider serviceProvider, ILogger<PacketFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            RegisterPacketTypes();
        }

        private void RegisterPacketTypes()
        {
            RegisterPacketType<ConnectWorldPacket>(PacketType.W_CONNECT_WORLD2);
            RegisterPacketType<CharListPacket>(PacketType.W_CHAR_LIST2);
            // ... Register other packets
        }

        private void RegisterPacketType<T>(PacketType type) where T : PacketBase
        {
            _packetTypes[type] = typeof(T);
        }

        public PacketBase? CreatePacket(PacketType type)
        {
            if (!_packetTypes.TryGetValue(type, out var packetType))
            {
                _logger.LogWarning("No handler registered for packet type: {PacketType}", type);
                return null;
            }

            return (PacketBase)ActivatorUtilities.CreateInstance(_serviceProvider, packetType);
        }
    }
}
