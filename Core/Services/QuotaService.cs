using NC.PetitionLib;
using System.Collections.Concurrent;
using PetitionD.Infrastructure.Database.Repositories;
using PetitionD.Configuration;

namespace PetitionD.Core.Services;

public class QuotaService(
    ILogger<QuotaService> logger,
    PetitionRepository petitionRepository)
{
    private readonly ConcurrentDictionary<int, int> _accountQuotas = new();
    private readonly ILogger<QuotaService> _logger = logger;
    private readonly PetitionRepository _petitionRepository = petitionRepository;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public async Task<(bool IsValid, int CurrentQuota)> ValidateQuotaAsync(
        int accountUid,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _syncLock.WaitAsync(cancellationToken);

            var currentQuota = await GetOrLoadQuotaAsync(accountUid, cancellationToken);
            var isValid = currentQuota < Config.MaxQuota;

            _logger.LogDebug(
                "Quota validation for account {AccountUid}: Current={Current}, Max={Max}, Valid={Valid}",
                accountUid, currentQuota, Config.MaxQuota, isValid);

            return (isValid, currentQuota);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating quota for account {AccountUid}", accountUid);
            return (false, Config.MaxQuota);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task<int> GetOrLoadQuotaAsync(
        int accountUid,
        CancellationToken cancellationToken)
    {
        if (_accountQuotas.TryGetValue(accountUid, out var quota))
            return quota;

        // Load from database if not in memory
        var dbQuota = await _petitionRepository.GetCurrentQuotaAsync(
            accountUid, cancellationToken);

        _accountQuotas.TryAdd(accountUid, dbQuota);
        return dbQuota;
    }

    public async Task<PetitionErrorCode> UpdateQuotaAsync(
        int accountUid,
        int delta,
        CancellationToken cancellationToken = default)
    {
        if (!Config.EnableQuota)
            return PetitionErrorCode.Success;

        try
        {
            await _syncLock.WaitAsync(cancellationToken);

            // Update memory first
            var newQuota = _accountQuotas.AddOrUpdate(
                accountUid,
                delta,
                (_, current) => current + delta);

            // Validate new quota
            if (newQuota < 0)
            {
                // Rollback if quota would go negative
                _accountQuotas.AddOrUpdate(
                    accountUid,
                    0,
                    (_, current) => current - delta);

                _logger.LogWarning(
                    "Prevented negative quota for account {AccountUid}. Delta={Delta}",
                    accountUid, delta);

                return PetitionErrorCode.InvalidState;
            }

            if (newQuota > Config.MaxQuota)
            {
                // Rollback if quota would exceed max
                _accountQuotas.AddOrUpdate(
                    accountUid,
                    Config.MaxQuota,
                    (_, _) => Config.MaxQuota);

                _logger.LogWarning(
                    "Quota update would exceed maximum for account {AccountUid}. Delta={Delta}, Max={Max}",
                    accountUid, delta, Config.MaxQuota);

                return PetitionErrorCode.ExceedQuota;
            }

            // Update database
            var result = await _petitionRepository.UpdateQuotaAsync(
                accountUid, delta, cancellationToken);

            if (result != PetitionErrorCode.Success)
            {
                // Rollback memory on database failure
                _accountQuotas.AddOrUpdate(
                    accountUid,
                    Math.Max(0, newQuota - delta),
                    (_, _) => Math.Max(0, newQuota - delta));

                _logger.LogError(
                    "Failed to persist quota update for account {AccountUid}. Rolling back.",
                    accountUid);

                return result;
            }

            _logger.LogInformation(
                "Updated quota for account {AccountUid}: Delta={Delta}, New={NewQuota}",
                accountUid, delta, newQuota);

            return PetitionErrorCode.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating quota for account {AccountUid}. Delta={Delta}",
                accountUid, delta);
            return PetitionErrorCode.DatabaseFail;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task ResetQuotaAsync(
        int accountUid,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _syncLock.WaitAsync(cancellationToken);

            _accountQuotas.TryRemove(accountUid, out _);
            await _petitionRepository.ResetQuotaAsync(accountUid, cancellationToken);

            _logger.LogInformation("Reset quota for account {AccountUid}", accountUid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting quota for account {AccountUid}", accountUid);
            throw;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task ResetAllQuotasAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _syncLock.WaitAsync(cancellationToken);
            _accountQuotas.Clear();

            await _petitionRepository.ResetAllQuotasAsync(cancellationToken);

            _logger.LogInformation("Reset all account quotas");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting all quotas");
            throw;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task InitializeQuotasAsync(
        IEnumerable<(int AccountUid, int Quota)> quotas,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _syncLock.WaitAsync(cancellationToken);
            _accountQuotas.Clear();

            foreach (var (accountUid, quota) in quotas)
            {
                _accountQuotas.TryAdd(accountUid, Math.Min(quota, Config.MaxQuota));
            }

            _logger.LogInformation("Initialized quotas for {Count} accounts", quotas.Count());
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<int> GetQuotaUsageAsync(
        int accountUid,
        CancellationToken cancellationToken = default)
    {
        if (!Config.EnableQuota)
            return 0;

        try
        {
            var currentQuota = await GetOrLoadQuotaAsync(accountUid, cancellationToken);
            return Config.MaxQuota - currentQuota;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quota usage for account {AccountUid}", accountUid);
            return 0;
        }
    }

    public bool IsQuotaEnabled()
    {
        return Config.EnableQuota;
    }
}