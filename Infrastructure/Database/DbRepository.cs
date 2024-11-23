// File: Infrastructure/Database/DbRepository.cs
namespace PetitionD.Infrastructure.Database;

public class DbRepository : IDbRepository
{
    private readonly DbContext _dbContext;
    private readonly ILogger<DbRepository> _logger;

    public DbRepository(
        DbContext dbContext,
        ILogger<DbRepository> logger)
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

            return await _dbContext.ExecuteStoredProcAsync<(bool, int, Grade)>(
                "up_Server_ValidateGM",
                parameters,
                async (reader, token) =>
                {
                    if (!await reader.ReadAsync(token))
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

    public async Task<(PetitionErrorCode ErrorCode, string PetitionSeq)> CreatePetitionAsync(
        Petition petition,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new
            {
                Seq = petition.PetitionSeq,
                Category = (byte)petition.Category,
                WorldId = (byte)petition.WorldId,
                petition.User.AccountName,
                petition.User.AccountUid,
                petition.User.CharName,
                petition.User.CharUid,
                petition.Content,
                ForcedGmAccountName = petition.ForcedGm.AccountName,
                ForcedGmAccountUid = petition.ForcedGm.AccountUid,
                ForcedGmCharName = petition.ForcedGm.CharName,
                ForcedGmCharUid = petition.ForcedGm.CharUid,
                QuotaAtSubmit = (byte)petition.QuotaAtSubmit,
                Time = petition.SubmitTime
            };

            await _dbContext.ExecuteStoredProcAsync(
                "up_Server_InsertPetition",
                parameters,
                cancellationToken);

            return (PetitionErrorCode.Success, petition.PetitionSeq);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create petition");
            return (PetitionErrorCode.DatabaseFail, string.Empty);
        }
    }

    public async Task<PetitionErrorCode> UpdatePetitionStateAsync(
        string petitionSeq,
        State newState,
        GameCharacter actor,
        string? message = null,
        byte? flag = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new
            {
                Seq = petitionSeq,
                State = (byte)newState,
                actor.AccountName,
                actor.AccountUid,
                actor.CharName,
                actor.CharUid,
                Message = message ?? string.Empty,
                Flag = flag ?? 0,
                Time = DateTime.UtcNow
            };

            await _dbContext.ExecuteStoredProcAsync(
                "up_Server_UpdatePetitionState",
                parameters,
                cancellationToken);

            return PetitionErrorCode.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update petition state");
            return PetitionErrorCode.DatabaseFail;
        }
    }

    public async Task<PetitionErrorCode> UpdateQuotaAsync(
        int accountUid,
        int delta,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new
            {
                AccountUid = accountUid,
                Delta = delta
            };

            await _dbContext.ExecuteStoredProcAsync(
                "up_Server_UpdateQuota",
                parameters,
                cancellationToken);

            return PetitionErrorCode.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update quota");
            return PetitionErrorCode.DatabaseFail;
        }
    }

    // Implement other interface methods as needed...
    // For now, let's implement the minimum required for AuthService to work

    public Task<IEnumerable<Template>> GetTemplatesForGmAsync(int gmAccountUid, CancellationToken cancellationToken = default)
    {
        // Temporary implementation
        return Task.FromResult(Enumerable.Empty<Template>());
    }

    public Task<PetitionErrorCode> UpdateTemplateAsync(Template template, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PetitionErrorCode.Success);
    }

    public Task<PetitionErrorCode> DeleteTemplateAsync(int templateId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PetitionErrorCode.Success);
    }

    public Task<Petition?> GetPetitionByIdAsync(int petitionId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Petition?>(null);
    }

    public Task<IEnumerable<Petition>> GetActivePetitionsForWorldAsync(int worldId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<Petition>());
    }
}