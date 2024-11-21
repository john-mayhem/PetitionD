using Microsoft.Data.SqlClient;
using NC.PetitionLib;
using PetitionD.Core.Models;
using System.Data;
using System.Transactions;

namespace PetitionD.Infrastructure.Database.Repositories;

public class GmRepository
{
    private readonly DbContext _dbContext;
    private readonly ILogger<GmRepository> _logger;

    public GmRepository(
        DbContext dbContext,
        ILogger<GmRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(bool IsValid, int AccountUid, Grade Grade)> ValidateGmCredentialsAsync(
        string account,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new
            {
                Account = account,
                Password = password
            };

            return await _dbContext.ExecuteStoredProcAsync(
                "up_Server_ValidateGM",
                parameters,
                async reader =>
                {
                    if (!await reader.ReadAsync(cancellationToken))
                        return (false, 0, Grade.User);

                    return (
                        true,
                        reader.GetInt32(0),  // AccountUid
                        (Grade)reader.GetByte(1)  // Grade
                    );
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate GM credentials for {Account}", account);
            return (false, 0, Grade.User);
        }
    }

    public async Task UpdateGmStatusAsync(
        int worldId,
        string gmCharName,
        GmStatusAction action,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new
            {
                Action = (byte)action,
                WorldId = (byte)worldId,
                GmCharName = gmCharName
            };

            await _dbContext.ExecuteStoredProcAsync(
                "up_Server_UpdateGmStatus",
                parameters,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update GM status for {GmCharName} in world {WorldId}",
                gmCharName, worldId);
        }
    }

    public async Task<List<GmCharacter>> GetGmCharactersAsync(
        int accountUid,
        int worldId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new
            {
                AccountUid = accountUid,
                WorldId = worldId
            };

            return await _dbContext.ExecuteStoredProcAsync(
                "up_Server_GetGmCharList",
                parameters,
                async reader =>
                {
                    var characters = new List<GmCharacter>();
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        characters.Add(new GmCharacter
                        {
                            WorldId = worldId,
                            AccountUid = accountUid,
                            CharName = reader.GetString(0),
                            CharUid = reader.GetInt32(1),
                            Grade = (Grade)reader.GetByte(2)
                        });
                    }
                    return characters;
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get GM characters for account {AccountUid} in world {WorldId}",
                accountUid, worldId);
            return new List<GmCharacter>();
        }
    }
}

public enum GmStatusAction : byte
{
    Clear = 0,
    Add = 1,
    Remove = 2
}