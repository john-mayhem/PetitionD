// File: Infrastructure/Network/Packets/GmPacketFactory.cs
using NC.PetitionLib;
using Microsoft.Extensions.DependencyInjection;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Infrastructure.Network.Packets.Auth;
using PetitionD.Infrastructure.Network.Packets.Petition;
using PetitionD.Infrastructure.Network.Packets.World;
using PetitionD.Infrastructure.Network.Packets.Chat;
using PetitionD.Infrastructure.Network.Packets.Template;

namespace PetitionD.Infrastructure.Network.Packets;

public class GmPacketFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<PacketType, Type> _packetTypes = [];
    private readonly ILogger<GmPacketFactory> _logger;

    public GmPacketFactory(IServiceProvider serviceProvider, ILogger<GmPacketFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        RegisterPacketTypes();
    }

    private void RegisterPacketTypes()
    {
        // Auth
        RegisterPacket<LoginPacket>(PacketType.G_LOGIN);

        // World
        RegisterPacket<RequestWorldListPacket>(PacketType.G_REQUEST_WORLD_LIST);
        RegisterPacket<RequestWorldCharPacket>(PacketType.G_REQUEST_WORLD_CHAR);
        RegisterPacket<EnterWorldPacket>(PacketType.G_ENTER_WORLD);
        RegisterPacket<LeaveWorldPacket>(PacketType.G_LEAVE_WORLD);
        RegisterPacket<ConnectWorldPacket>(PacketType.W_CONNECT_WORLD2);
        RegisterPacket<CharListPacket>(PacketType.W_CHAR_LIST2);
        RegisterPacket<ConnectedCharsPacket>(PacketType.W_CONNECTED_CHARS2);
        RegisterPacket<SubmitPetitionPacket>(PacketType.W_SUBMIT_PETITION6);
        RegisterPacket<CancelPetitionPacket>(PacketType.W_CANCEL_PETITION3);

        // Petition
        RegisterPacket<RequestCategoryPacket>(PacketType.G_REQUEST_CATEGORY);
        RegisterPacket<CheckOutPetitionPacket>(PacketType.G_CHECK_OUT_PETITION);
        RegisterPacket<UndoCheckOutPacket>(PacketType.G_UNDO_CHECK_OUT);
        RegisterPacket<ModifyCategoryPacket>(PacketType.G_MODIFY_CATEGORY);
        RegisterPacket<AddMemoPacket>(PacketType.G_ADD_MEMO);

        // Chat
        RegisterPacket<MessagingCheckInPacket>(PacketType.G_MESSAGING_CHECK_IN);
        RegisterPacket<ChattingCheckInPacket>(PacketType.G_CHATTING_CHECK_IN);

        // Template
        RegisterPacket<RequestTemplatePacket>(PacketType.G_REQUEST_TEMPLATE);
        RegisterPacket<UpdateTemplatePacket>(PacketType.G_UPDATE_TEMPLATE);
        RegisterPacket<DeleteTemplatePacket>(PacketType.G_DELETE_TEMPLATE);
    }

    private void RegisterPacket<T>(PacketType type) where T : GmPacketBase
    {
        _packetTypes[type] = typeof(T);
    }

    public GmPacketBase? CreatePacket(PacketType type)
    {
        if (!_packetTypes.TryGetValue(type, out var packetType))
        {
            _logger.LogWarning("No handler registered for packet type: {PacketType}", type);
            return null;
        }

        return (GmPacketBase)ActivatorUtilities.CreateInstance(_serviceProvider, packetType);
    }
}