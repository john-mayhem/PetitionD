// File: Core/Services/Quota.cs
namespace PetitionD.Core.Services;

public static class Quota
{
    private static readonly Dictionary<int, int> _accountQuotas = new();

    public static int GetCurrentQuota(int accountUid)
    {
        lock (_accountQuotas)
        {
            return _accountQuotas.TryGetValue(accountUid, out var quota) ? quota : 0;
        }
    }

    public static void UpdateQuota(int accountUid, int delta)
    {
        lock (_accountQuotas)
        {
            if (!_accountQuotas.ContainsKey(accountUid))
                _accountQuotas[accountUid] = 0;
            _accountQuotas[accountUid] += delta;
        }
    }
}
