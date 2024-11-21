// File: Infrastructure/Database/Repositories/GmRepository.cs
using NC.PetitionLib;

namespace PetidionD.Infrastructure.Database.Repositories;

public class GmRepository
{
    private readonly DbContext _dbContext;
    private readonly ILogger<GmRepository> _logger;

    public GmRepository(DbContext dbContext, ILogger<GmRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(bool IsValid, int AccountUid, Grade Grade)> ValidateGmCredentialsAsync(
        string account,
        string password)
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
                    if (!await reader.ReadAsync())
                        return (false, 0, Grade.User);

                    return (
                        true,
                        reader.GetInt32(0),
                        (Grade)reader.GetByte(1)
                    );
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating GM credentials for {Account}", account);
            return (false, 0, Grade.User);
        }
    }

    public async Task UpdateGmStatusAsync(int worldId, string gmCharName, GmStatusAction action)
    {
        try
        {
            var parameters = new
            {
                Action = (byte)action,
                WorldId = (byte)worldId,
                GmCharName = gmCharName
            };

            await _dbContext.ExecuteNonQueryAsync(
                "up_Server_UpdateGmStatus",
                parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating GM status for {GmCharName} in world {WorldId}",
                gmCharName, worldId);
        }
    }
}