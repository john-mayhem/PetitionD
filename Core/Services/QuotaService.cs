using PetitionD.Configuration;
using System.Collections.Concurrent;

namespace PetitionD.Core.Services;

public class QuotaService
{
    private readonly ConcurrentDictionary<int, int> _accountQuotas = new();
    private readonly ILogger<QuotaService> _logger;

    public QuotaService(ILogger<QuotaService> logger)
    {
        _logger = logger;
    }

    public int GetCurrentQuota(int accountUid)
    {
        return _accountQuotas.GetValueOrDefault(accountUid, 0);
    }

    public void UpdateQuota(int accountUid, int delta)
    {
        _accountQuotas.AddOrUpdate(
            accountUid,
            delta,
            (_, current) => current + delta);

        _logger.LogInformation(
            "Updated quota for account {AccountUid}: Delta={Delta}",
            accountUid, delta);
    }

    public bool HasAvailableQuota(int accountUid)
    {
        var currentQuota = GetCurrentQuota(accountUid);
        return currentQuota < Config.mMaxQuota;
    }

    public void ResetQuota(int accountUid)
    {
        _accountQuotas.TryRemove(accountUid, out _);
        _logger.LogInformation("Reset quota for account {AccountUid}", accountUid);
    }

    public void ResetAllQuotas()
    {
        _accountQuotas.Clear();
        _logger.LogInformation("Reset all quotas");
    }

    // Optional: Add persistence methods if needed
    public async Task PersistQuotasAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement quota persistence to database
        throw new NotImplementedException();
    }

    public async Task LoadQuotasAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement quota loading from database
        throw new NotImplementedException();
    }
}