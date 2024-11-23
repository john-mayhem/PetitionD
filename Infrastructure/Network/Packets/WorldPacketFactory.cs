// File: Infrastructure/Network/Packets/WorldPacketFactory.cs
using NC.PetitionLib;
using Microsoft.Extensions.DependencyInjection;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Infrastructure.Network.Packets.World;

namespace PetitionD.Infrastructure.Network.Packets;

using NC.PetitionLib;
using Microsoft.Extensions.DependencyInjection;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Infrastructure.Network.Packets.World;

public class WorldPacketFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<PacketType, Type> _packetTypes = [];
    private readonly ILogger<WorldPacketFactory> _logger;

    public WorldPacketFactory(IServiceProvider serviceProvider, ILogger<WorldPacketFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        RegisterPacketTypes();
    }

    private void RegisterPacketTypes()
    {
        // Move world-specific packets here
        RegisterPacket<ConnectWorldPacket>(PacketType.W_CONNECT_WORLD2);
        RegisterPacket<CharListPacket>(PacketType.W_CHAR_LIST2);
        RegisterPacket<ConnectedCharsPacket>(PacketType.W_CONNECTED_CHARS2);
        RegisterPacket<SubmitPetitionPacket>(PacketType.W_SUBMIT_PETITION6);
        RegisterPacket<CancelPetitionPacket>(PacketType.W_CANCEL_PETITION3);
    }

    private void RegisterPacket<T>(PacketType type) where T : WorldPacketBase
    {
        _packetTypes[type] = typeof(T);
    }

    public WorldPacketBase? CreatePacket(PacketType type)
    {
        if (!_packetTypes.TryGetValue(type, out var packetType))
        {
            _logger.LogWarning("No handler registered for packet type: {PacketType}", type);
            return null;
        }

        return (WorldPacketBase)ActivatorUtilities.CreateInstance(_serviceProvider, packetType);
    }
}