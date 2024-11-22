using NC.PetitionLib;
using PetitionD.Core.Models;
using PetitionD.Infrastructure.Database.Repositories;
using System.Collections.Concurrent;

namespace PetitionD.Core.Services;

public class TemplateService(
    TemplateRepository templateRepository,
    ILogger<TemplateService> logger)
{
    private readonly TemplateRepository _templateRepository = templateRepository;
    private readonly ILogger<TemplateService> _logger = logger;
    private readonly ConcurrentDictionary<int, List<Template>> _templateCache = new();

    public async Task<IEnumerable<Template>> GetTemplatesForGmAsync(
        int gmAccountUid,
        bool useCache = true,
        CancellationToken cancellationToken = default)
    {
        if (useCache && _templateCache.TryGetValue(gmAccountUid, out var cachedTemplates))
        {
            return cachedTemplates;
        }

        var templates = await _templateRepository.GetTemplatesForGmAsync(
            gmAccountUid, cancellationToken);

        if (useCache)
        {
            _templateCache.AddOrUpdate(
                gmAccountUid,
                new List<Template>(templates),
                (_, _) => new List<Template>(templates));
        }

        return templates;
    }

    public async Task<PetitionErrorCode> UpdateTemplateAsync(
        Template template,
        int gmAccountUid,
        string gmAccount,
        CancellationToken cancellationToken = default)
    {
        var (errorCode, _) = await _templateRepository.UpdateTemplateAsync(
            template, gmAccountUid, gmAccount, cancellationToken);

        if (errorCode == PetitionErrorCode.Success)
        {
            _templateCache.TryRemove(gmAccountUid, out _);
            _logger.LogInformation(
                "Template {TemplateCode} updated by GM {GmAccount}",
                template.Code, gmAccount);
        }

        return errorCode;
    }

    public async Task<PetitionErrorCode> DeleteTemplateAsync(
        int templateCode,
        int gmAccountUid,
        CancellationToken cancellationToken = default)
    {
        var result = await _templateRepository.DeleteTemplateAsync(
            templateCode, gmAccountUid, cancellationToken);

        if (result == PetitionErrorCode.Success)
        {
            _templateCache.TryRemove(gmAccountUid, out _);
            _logger.LogInformation(
                "Template {TemplateCode} deleted by GM {GmAccountUid}",
                templateCode, gmAccountUid);
        }

        return result;
    }

    public void InvalidateCache(int gmAccountUid)
    {
        _templateCache.TryRemove(gmAccountUid, out _);
    }

    public void InvalidateAllCaches()
    {
        _templateCache.Clear();
    }
}