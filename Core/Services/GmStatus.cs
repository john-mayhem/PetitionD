// File: Core/Services/GmStatus.cs
namespace PetitionD.Core.Services;

public static class GmStatus
{
    private static readonly Dictionary<int, HashSet<string>> _worldGms = [];
    private static readonly object _lock = new();

    public static void Add(int worldId, string gmCharName)
    {
        lock (_lock)
        {
            if (!_worldGms.TryGetValue(worldId, out var gms))
            {
                gms = [];
                _worldGms[worldId] = gms;
            }
            gms.Add(gmCharName);
        }
    }

    public static void Remove(int worldId, string gmCharName)
    {
        lock (_lock)
        {
            if (_worldGms.TryGetValue(worldId, out var gms))
            {
                gms.Remove(gmCharName);
            }
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _worldGms.Clear();
        }
    }
}