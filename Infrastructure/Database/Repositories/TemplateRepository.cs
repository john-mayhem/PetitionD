// File: Infrastructure/Database/Repositories/TemplateRepository.cs
using NC.PetitionLib;

namespace PetidionD.Infrastructure.Database.Repositories;

public class TemplateRepository
{
    private readonly DbContext _dbContext;
    private readonly ILogger<TemplateRepository> _logger;

    public TemplateRepository(DbContext dbContext, ILogger<TemplateRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IEnumerable<Template>> GetTemplatesAsync(int gmAccountUid)
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
                    while (await reader.ReadAsync())
                    {
                        templates.Add(new Template
                        {
                            Code = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Type = (Template.Type)reader.GetByte(2),
                            Content = reader.GetString(3),
                            Category = reader.GetByte(4),
                            SortOrder = reader.GetInt16(5)
                        });
                    }
                    return templates;
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting templates for GM {GmAccountUid}", gmAccountUid);
            return Enumerable.Empty<Template>();
        }
    }

    public async Task<(PetitionErrorCode ErrorCode, int ResultCode)> UpdateTemplateAsync(
        int gmAccountUid,
        string gmAccount,
        Template template)
    {
        try
        {
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

            return await _dbContext.ExecuteStoredProcAsync(
                "up_Server_UpdateTemplate",
                parameters,
                async reader =>
                {
                    await reader.ReadAsync();
                    return (PetitionErrorCode.Success, reader.GetInt32(0));
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateCode}", template.Code);
            return (PetitionErrorCode.DatabaseFail, 0);
        }
    }
}