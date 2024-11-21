// File: Core/Services/QuotaService.cs
namespace PetitionD.Core.Services;

using Microsoft.Extensions.Logging;
using PetitionD.Core.Models;
using PetitionD.Infrastructure.Database.Repositories;
using System.Collections.Concurrent;

public class QuotaService
{
    private readonly ConcurrentDictionary<int, int> _accountQuotas = new();
    private readonly ILogger<QuotaService> _logger;
    private readonly PetitionRepository _petitionRepository;

    public QuotaService(
        ILogger<QuotaService> logger,
        PetitionRepository petitionRepository)
    {
        _logger = logger;
        _petitionRepository = petitionRepository;
    }

    public int GetCurrentQuota(int accountUid)
    {
        return _accountQuotas.GetValueOrDefault(accountUid, 0);
    }

    public async Task UpdateQuotaAsync(int accountUid, int delta)
    {
        try
        {
            _accountQuotas.AddOrUpdate(
                accountUid,
                delta,
                (_, current) => current + delta);

            await _petitionRepository.UpdateQuotaAsync(accountUid, delta);

            _logger.LogInformation(
                "Updated quota for account {AccountUid}: Delta={Delta}",
                accountUid, delta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update quota for account {AccountUid}", accountUid);
        }
    }
}