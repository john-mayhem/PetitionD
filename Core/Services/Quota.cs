// File: Core/Services/Quota.cs
namespace PetitionD.Core.Services;

public static class Quota
{
    private static readonly Dictionary<int, int> _quotas = new();
    private static readonly object _lock = new();

    public static int GetCurrentQuota(int accountUid)
    {
        lock (_lock)
        {
            return _quotas.TryGetValue(accountUid, out var quota) ? quota : 0;
        }
    }

    public static void UpdateQuota(int accountUid, int delta)
    {
        lock (_lock)
        {
            if (!_quotas.ContainsKey(accountUid))
                _quotas[accountUid] = 0;
            _quotas[accountUid] += delta;
        }
    }
}