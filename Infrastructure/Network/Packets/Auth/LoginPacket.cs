// File: Infrastructure/Network/Packets/LoginPacket.cs
using NC.PetitionLib;
using NC.ToolNet.Net;
using PetitionD.Infrastructure.Network.Packets.Base;
using PetitionD.Core.Interfaces;
using PetitionD.Core.Models;

namespace PetitionD.Infrastructure.Network.Packets.Auth
{
    public class LoginPacket(IAuthService authService, ILogger<LoginPacket> logger) : GmPacketBase(PacketType.G_LOGIN)
    {
        private readonly IAuthService _authService = authService;
        private readonly ILogger<LoginPacket> _logger = logger;

        public override async void Handle(GmSession session, Unpacker unpacker)
        {
            try
            {
                var buildNumber = unpacker.GetInt32();
                var account = unpacker.GetStringMax(MaxLen.Account);
                var password = unpacker.GetStringMax(MaxLen.Password);
                var oneTimeKeyResponse = unpacker.GetBytes(8);

                if (session.OneTimeKey == null)
                {
                    SendResult(session, PetitionErrorCode.UnmatchedSession);
                    return;
                }

                // Validate build number
                if (buildNumber < session.Settings.MinimumGmClientBuildNumber)
                {
                    SendResult(session, PetitionErrorCode.ClientVersionNotMatch);
                    return;
                }

                // Validate one-time key
                if (!VerifyOneTimeKey(session.OneTimeKey, oneTimeKeyResponse))
                {
                    SendResult(session, PetitionErrorCode.UnmatchedSession);
                    return;
                }

                var (ErrorCode, AccountUid) = await _authService.AuthenticateAsync(account, password);
                SendResult(session, ErrorCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling login packet");
                SendResult(session, PetitionErrorCode.InternalServerFail);
            }
        }

        private static void SendResult(GmSession session, PetitionErrorCode errorCode)
        {
            var packer = new Packer((byte)PacketType.G_LOGIN_RESULT);
            packer.AddUInt8((byte)errorCode);
            session.Send(packer.ToArray());
        }

        private static bool VerifyOneTimeKey(byte[] sessionKey, byte[] responseKey)
        {
            if (sessionKey.Length != 8 || responseKey.Length != 8)
                return false;

            return sessionKey.SequenceEqual(responseKey);
        }

        public override byte[] Serialize()
        {
            throw new NotImplementedException("Login packet is incoming only");
        }
    }
}
