// File: Infrastructure/Network/Sessions/GmSession.cs
using Microsoft.Extensions.Logging;
using NC.PetitionLib;
using PetitionD.Configuration;
using PetitionD.Core.Interfaces;
using System.Security.Cryptography;
using PetitionD.Infrastructure.Network.Packets;
using NC.ToolNet.Net;
using PetidionD.Core.Models;

namespace PetitionD.Infrastructure.Network.Sessions;

public class GmSession : BaseSession
{
    private readonly ILogger<GmSession> _logger;
    private readonly IAuthService _authService;
    private readonly AppSettings _settings;
    private readonly GmPacketFactory _packetFactory;

    public string Account { get; private set; } = "Unknown";
    public int AccountUid { get; private set; }
    public byte[]? OneTimeKey { get; private set; }
    public AppSettings Settings => _settings; // Add this property


    public GmSession(
        ILogger<GmSession> logger,
        IAuthService authService,
        AppSettings settings,
        GmPacketFactory packetFactory) : base(logger)
    {
        _logger = logger;
        _authService = authService;
        _settings = settings;
        _packetFactory = packetFactory;
        GenerateOneTimeKey();
    }

    private void GenerateOneTimeKey()
    {
        OneTimeKey = new byte[8];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(OneTimeKey);
    }

    protected override void OnReceived(byte[] packet)
    {
        try
        {
            var packetType = (PacketType)packet[0];
            _logger.LogDebug("Received GM packet: {PacketType}", packetType);

            // Pre-login packet validation
            if (AccountUid == 0 && packetType != PacketType.G_LOGIN && packetType != PacketType.G_SERVER_VER)
            {
                _logger.LogWarning("Unauthorized packet received: {PacketType}", packetType);
                return;
            }

            var handler = _packetFactory.CreatePacket(packetType);
            if (handler == null)
            {
                _logger.LogWarning("No handler for packet type: {PacketType}", packetType);
                return;
            }

            var unpacker = new Unpacker(packet);
            handler.Handle(this, unpacker);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing packet");
        }
    }

    protected override void OnSendFailed(Exception e)
    {
        _logger.LogError("Failed to send GM packet: {Message}", e.Message);
    }

    protected override void OnSessionStarted()
    {
        _logger.LogInformation("GM Session started: {Id}", Id);

        var packer = new NC.ToolNet.Net.Packer((byte)PacketType.G_SERVER_VER);
        packer.AddInt32(_settings.ServerBuildNumber);
        packer.AddBytes(OneTimeKey ?? []);
        Send(packer.ToArray());
    }

    protected override void OnSessionStopped()
    {
        _logger.LogInformation("GM Session stopped: {Id}", Id);

        if (AccountUid != 0)
        {
            _authService.InvalidateSession(AccountUid);
        }
    }

    public async Task<PetitionErrorCode> AuthenticateAsync(string account, string password)
    {
        if (account.Length > MaxLen.Account || password.Length > MaxLen.Password)
        {
            return PetitionErrorCode.TooLongString;
        }

        var (errorCode, accountUid) = await _authService.AuthenticateAsync(account, password);

        if (errorCode == PetitionErrorCode.Success)
        {
            Account = account;
            AccountUid = accountUid;
        }

        return errorCode;
    }

    private readonly Dictionary<int, int> _tryLoginList = [];
    private readonly Dictionary<int, GmCharacter> _loginWorldList = [];
    private readonly List<GmCharacter> _characterList = [];

    public bool HasCharList(int worldId)
    {
        lock (_characterList)
        {
            return _characterList.Any(c => c.WorldId == worldId);
        }
    }

    public void AddTryLoginList(int worldId, int gmCharUid)
    {
        lock (_tryLoginList)
        {
            _tryLoginList[worldId] = gmCharUid;
        }
    }

    public GmCharacter? GetCharacter(int worldId)
    {
        lock (_loginWorldList)
        {
            return _loginWorldList.TryGetValue(worldId, out var character) ? character : null;
        }
    }

    public PetitionErrorCode EnterWorld(int worldId, int gmCharUid)
    {
        lock (_characterList)
        {
            lock (_loginWorldList)
            {
                var character = _characterList.FirstOrDefault(c =>
                    c.WorldId == worldId && c.CharUid == gmCharUid);

                if (character == null)
                    return PetitionErrorCode.NonExistingChar;

                if (character.Grade < Grade.GMS)
                    return PetitionErrorCode.NotGMAccount;

                if (_loginWorldList.ContainsKey(worldId))
                    return PetitionErrorCode.GMAlreadyEntered;

                _loginWorldList[worldId] = character;
                GmStatus.Add(worldId, character.CharName);
                return PetitionErrorCode.Success;
            }
        }
    }

    public PetitionErrorCode LeaveWorld(int worldId)
    {
        lock (_loginWorldList)
        {
            if (!_loginWorldList.ContainsKey(worldId))
                return PetitionErrorCode.GMAlreadyLeaved;

            var character = _loginWorldList[worldId];
            _loginWorldList.Remove(worldId);
            GmStatus.Remove(worldId, character.CharName);
            return PetitionErrorCode.Success;
        }
    }
}