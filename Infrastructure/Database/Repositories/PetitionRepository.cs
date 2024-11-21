// File: Infrastructure/Database/Repositories/PetitionRepository.cs
using Microsoft.Data.SqlClient;
using NC.PetitionLib;
using PetidionD.Core.Models;
using PetidionD.Core.Services;

namespace PetidionD.Infrastructure.Database.Repositories;

public class PetitionRepository
{
    private readonly DbContext _dbContext;
    private readonly ILogger<PetitionRepository> _logger;

    public PetitionRepository(
        DbContext dbContext,
        ILogger<PetitionRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PetitionErrorCode> CreatePetitionAsync(Petition petition)
    {
        try
        {
            var parameters = new
            {
                Seq = petition.mPetitionSeq,
                Category = petition.mCategory,
                WorldId = petition.mWorldId,
                AccountName = petition.mUser.AccountName,
                AccountUid = petition.mUser.AccountUid,
                CharName = petition.mUser.CharName,
                CharUid = petition.mUser.CharUid,
                Content = petition.mContent,
                ForcedGmAccountName = petition.mForcedGm.AccountName,
                ForcedGmAccountUid = petition.mForcedGm.AccountUid,
                ForcedGmCharName = petition.mForcedGm.CharName,
                ForcedGmCharUid = petition.mForcedGm.CharUid,
                QuotaAtSubmit = petition.mQuotaAtSubmit,
                Time = petition.mSubmitTime
            };

            await _dbContext.ExecuteNonQueryAsync(
                "up_Server_InsertPetition",
                parameters);

            return PetitionErrorCode.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating petition {PetitionId}", petition.mPetitionId);
            return PetitionErrorCode.DatabaseFail;
        }
    }

    public async Task<IEnumerable<Petition>> GetActivePetitionsForWorldAsync(int worldId)
    {
        try
        {
            var parameters = new { WorldId = worldId };
            return await _dbContext.ExecuteStoredProcAsync(
                "up_Server_GetActivePetitionList",
                parameters,
                async reader =>
                {
                    var petitions = new List<Petition>();
                    while (await reader.ReadAsync())
                    {
                        var petition = new Petition();
                        await LoadPetitionFromReader(petition, reader);
                        petitions.Add(petition);
                    }
                    return petitions;
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active petitions for world {WorldId}", worldId);
            return Enumerable.Empty<Petition>();
        }
    }

    private static async Task LoadPetitionFromReader(Petition petition, SqlDataReader reader)
    {
        petition.mWorldId = reader.GetByte(0);
        petition.mPetitionSeq = reader.GetString(1);
        petition.mCategory = reader.GetByte(2);
        petition.mGrade = (Grade)reader.GetByte(3);

        petition.mUser = new GameCharacter
        {
            AccountName = reader.GetString(4),
            AccountUid = reader.GetInt32(5),
            CharName = reader.GetString(6),
            CharUid = reader.GetInt32(7)
        };

        petition.mCheckOutGm = new GameCharacter
        {
            AccountName = reader.GetString(8),
            AccountUid = reader.GetInt32(9),
            CharName = reader.GetString(10),
            CharUid = reader.GetInt32(11)
        };

        petition.mFlag = reader.GetByte(12);
        petition.mContent = reader.GetString(13);
        petition.mSubmitTime = reader.GetDateTime(14);
        petition.mState = (State)reader.GetByte(15);

        petition.mForcedGm = new GameCharacter
        {
            AccountName = reader.GetString(16),
            AccountUid = reader.GetInt32(17),
            CharName = reader.GetString(18),
            CharUid = reader.GetInt32(19)
        };

        petition.mQuotaAtSubmit = reader.GetByte(20);

        if (!reader.IsDBNull(21))
            petition.mCheckOutTime = reader.GetDateTime(21);

        // Load additional info if needed
        await petition.LoadAdditionalInfo();
    }
}