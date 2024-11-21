using System.Collections.Concurrent;
using NC.PetitionLib;

namespace PetitionD.Core.Services;

public class GmStatusService
{
    private readonly ConcurrentDictionary<int, HashSet<string>> _worldGms = new();
    private readonly ILogger<GmStatusService> _logger;
    private readonly object _syncLock = new();

    public GmStatusService(ILogger<GmStatusService> logger)
    {
        _logger = logger;
    }

    public void Add(int worldId, string gmCharName)
    {
        var gms = _worldGms.GetOrAdd(worldId, _ => new HashSet<string>());
        lock (_syncLock)
        {
            if (gms.Add(gmCharName))
            {
                _logger.LogInformation("GM {GmCharName} added to world {WorldId}",
                    gmCharName, worldId);
            }
        }
    }

    public void Remove(int worldId, string gmCharName)
    {
        if (_worldGms.TryGetValue(worldId, out var gms))
        {
            lock (_syncLock)
            {
                if (gms.Remove(gmCharName))
                {
                    _logger.LogInformation("GM {GmCharName} removed from world {WorldId}",
                        gmCharName, worldId);
                }
            }
        }
    }

    public void Clear()
    {
        _worldGms.Clear();
        _logger.LogInformation("Cleared all GM statuses");
    }

    public bool IsGmOnline(int worldId, string gmCharName)
    {
        return _worldGms.TryGetValue(worldId, out var gms) && gms.Contains(gmCharName);
    }

    public IReadOnlyCollection<string> GetOnlineGms(int worldId)
    {
        if (_worldGms.TryGetValue(worldId, out var gms))
        {
            lock (_syncLock)
            {
                return gms.ToList().AsReadOnly();
            }
        }
        return Array.Empty<string>();
    }

    public int GetOnlineGmCount(int worldId)
    {
        return _worldGms.TryGetValue(worldId, out var gms) ? gms.Count : 0;
    }
}