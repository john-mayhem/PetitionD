// File: Infrastructure/Network/WorldSessionManager.cs
namespace PetitionD.Infrastructure.Network;

using System.Collections.Concurrent;

public class WorldSessionManager : IWorldSessionManager
{
    private readonly ILogger<WorldSessionManager> _logger;
    private readonly ConcurrentDictionary<int, WorldSession> _sessions = new();

    public event Action<WorldSession> SessionCreated = delegate { };

    public WorldSessionManager(ILogger<WorldSessionManager> logger)
    {
        _logger = logger;
    }

    public void AddSession(WorldSession session)
    {
        if (_sessions.TryAdd(session.WorldId, session))
        {
            _logger.LogInformation("World session {WorldId} added", session.WorldId);
            SessionCreated(session); // Raise the event when a session is added
        }
    }

    public void RemoveSession(int worldId)
    {
        if (_sessions.TryRemove(worldId, out var session))
        {
            session.Stop();
            _logger.LogInformation("World session {WorldId} removed", worldId);
        }
    }

    public WorldSession? GetSession(int worldId)
    {
        _sessions.TryGetValue(worldId, out var session);
        return session;
    }

    public IEnumerable<WorldSession> GetAllSessions() => _sessions.Values;
}