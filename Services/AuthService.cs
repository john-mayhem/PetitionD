// File: Services/AuthService.cs
using Microsoft.Extensions.Logging;
using NC.PetitionLib;
using PetitionD.Core.Interfaces;
using PetitionD.Infrastructure.Database;
using System.Security.Cryptography;

namespace PetitionD.Services;

public class AuthService(ILogger<AuthService> logger, IDbRepository repository) : IAuthService
{
    private readonly Dictionary<int, string> _activeSessions = [];

    public async Task<(PetitionErrorCode ErrorCode, int AccountUid)> AuthenticateAsync(string account, string password)
    {
        try
        {
            var (IsValid, AccountUid) = await repository.ValidateGmCredentialsAsync(account, password);

            if (IsValid)
            {
                var sessionToken = GenerateSessionToken();
                _activeSessions[AccountUid] = sessionToken;
                return (PetitionErrorCode.Success, AccountUid);
            }

            return (PetitionErrorCode.IncorrectPassword, 0);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Authentication failed");
            return (PetitionErrorCode.DatabaseFail, 0);
        }
    }

    public Task<bool> ValidateSessionAsync(int accountUid, string sessionToken)
    {
        return Task.FromResult(_activeSessions.TryGetValue(accountUid, out var storedToken)
            && storedToken == sessionToken);
    }

    public void InvalidateSession(int accountUid)
    {
        _activeSessions.Remove(accountUid);
    }

    private static string GenerateSessionToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}