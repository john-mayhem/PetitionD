// File: Infrastructure/Database/DbRepository.cs
using Dapper;
using NC.PetitionLib;
using PetidionD.Core.Models;

namespace PetidionD.Infrastructure.Database.Repositories;

public class DbRepository : IDbRepository
{
    private readonly DbContext _dbContext;
    private readonly ILogger<DbRepository> _logger;

    public DbRepository(DbContext dbContext, ILogger<DbRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(bool IsValid, int AccountUid)> ValidateGmCredentialsAsync(string account, string password)
    {
        try
        {
            var parameters = new { Account = account, Password = password };
            var result = await _dbContext.ExecuteStoredProcedureAsync("up_Server_ValidateGM",
                parameters,
                reader =>
                {
                    if (!reader.Read()) return (false, 0);
                    return (true, reader.GetInt32(0));
                });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating GM credentials for account {Account}", account);
            return (false, 0);
        }
    }

    public async Task<PetitionErrorCode> CreatePetitionAsync(Petition petition)
    {
        try
        {
            var parameters = new
            {
                WorldId = petition.mWorldId,
                Category = petition.Category,
                UserAccountName = petition.mUser.AccountName,
                UserAccountUid = petition.mUser.AccountUid,
                UserCharName = petition.mUser.CharName,
                UserCharUid = petition.mUser.CharUid,
                Content = petition.Content,
                ForcedGmAccountName = petition.mForcedGm.AccountName,
                ForcedGmAccountUid = petition.mForcedGm.AccountUid,
                ForcedGmCharName = petition.mForcedGm.CharName,
                ForcedGmCharUid = petition.mForcedGm.CharUid
            };

            await _dbContext.ExecuteStoredProcedureAsync("up_Server_CreatePetition",
                parameters,
                reader =>
                {
                    reader.Read();
                    return (PetitionErrorCode)reader.GetByte(0);
                });

            return PetitionErrorCode.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating petition");
            return PetitionErrorCode.DatabaseFail;
        }
    }

    // ... More implementations ...
}