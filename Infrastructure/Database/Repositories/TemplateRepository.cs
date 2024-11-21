using NC.PetitionLib;
using PetitionD.Core.Models;
using System.Transactions;

namespace PetitionD.Infrastructure.Database.Repositories;

public class TemplateRepository
{
    private readonly DbContext _dbContext;
    private readonly ILogger<TemplateRepository> _logger;

    public TemplateRepository(
        DbContext dbContext,
        ILogger<TemplateRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IEnumerable<Template>> GetTemplatesForGmAsync(
        int gmAccountUid,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new { GmAccountUid = gmAccountUid };

            return await _dbContext.ExecuteStoredProcAsync(
                "up_Server_GetTemplateList",
                parameters,
                async reader =>
                {
                    var templates = new List<Template>();
                    while (await reader.ReadAsync(cancellationToken))
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
            return [];
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
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var parameters = new
            {
                GmAccountUid = gmAccountUid,
                GmAccount = gmAccount,
                Code = template.Code,
                Name = template.Name,
                Type = (byte)template.Type,
                Content = template.Content,
                Category = template.Category,
                SortOrder = template.SortOrder
            };

            var result = await _dbContext.ExecuteStoredProcAsync(
                "up_Server_UpdateTemplate",
                parameters,
                async reader =>
                {
                    await reader.ReadAsync(cancellationToken);
                    return reader.GetInt32(0);
                },
                cancellationToken);

            scope.Complete();
            return (PetitionErrorCode.Success, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template {TemplateCode}", template.Code);
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