using NC.PetitionLib;
using PetitionD.Core.Models;
using System.Transactions;

namespace PetitionD.Infrastructure.Database.Repositories;

public class TemplateRepository(
    DbContext dbContext,
    ILogger<TemplateRepository> logger)
{
    private readonly DbContext _dbContext = dbContext;
    private readonly ILogger<TemplateRepository> _logger = logger;

    public async Task<IEnumerable<Template>> GetTemplatesForGmAsync(
        int gmAccountUid,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new { GmAccountUid = gmAccountUid };

            return await _dbContext.ExecuteStoredProcAsync<List<Template>>( // Specify List<Template>
                "up_Server_GetTemplateList",
                parameters,
                async (reader, token) =>
                {
                    var templates = new List<Template>();
                    while (await reader.ReadAsync(token))
                    {
                        templates.Add(new Template
                        {
                            Code = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Type = (Template.TemplateType)reader.GetByte(2),
                            Content = reader.GetString(3),
                            Category = reader.GetByte(4),
                            SortOrder = reader.GetInt16(5)
                        });
                    }
                    return templates;
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get templates for GM {GmAccountUid}", gmAccountUid);
            return new List<Template>();
        }
    }

    public async Task<(PetitionErrorCode ErrorCode, int ResultCode)> UpdateTemplateAsync(
        Template template,
        int gmAccountUid,
        string gmAccount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new
            {
                GmAccountUid = gmAccountUid,
                GmAccount = gmAccount,
                template.Code,
                template.Name,
                Type = (byte)template.Type,
                template.Content,
                Category = (byte)template.Category,
                template.SortOrder
            };

            var result = await _dbContext.ExecuteStoredProcAsync<int>( // Specify int
                "up_Server_UpdateTemplate",
                parameters,
                async (reader, token) =>
                {
                    await reader.ReadAsync(token);
                    return reader.GetInt32(0);
                },
                cancellationToken);

            return (PetitionErrorCode.Success, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template {Code}", template.Code);
            return (PetitionErrorCode.DatabaseFail, 0);
        }
    }

    public async Task<PetitionErrorCode> DeleteTemplateAsync(
        int templateCode,
        int gmAccountUid,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new
            {
                Code = templateCode,
                GmAccountUid = gmAccountUid
            };

            await _dbContext.ExecuteStoredProcAsync(
                "up_Server_DeleteTemplate",
                parameters,
                cancellationToken);

            return PetitionErrorCode.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete template {TemplateCode}", templateCode);
            return PetitionErrorCode.DatabaseFail;
        }
    }

    public async Task<PetitionErrorCode> UpdateTemplateOrderAsync(
        int templateCode,
        int offset,
        int gmAccountUid,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new
            {
                Code = templateCode,
                Offset = offset,
                GmAccountUid = gmAccountUid
            };

            await _dbContext.ExecuteStoredProcAsync(
                "up_Server_UpdateTemplateOrder",
                parameters,
                cancellationToken);

            return PetitionErrorCode.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template order for {TemplateCode}", templateCode);
            return PetitionErrorCode.DatabaseFail;
        }
    }
}