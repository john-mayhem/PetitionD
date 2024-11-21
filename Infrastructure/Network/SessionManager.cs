// File: Infrastructure/Network/SessionManager.cs
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PetitionD.Infrastructure.Network.Sessions;

namespace PetitionD.Infrastructure.Network;

public class SessionManager(ILogger<SessionManager> logger) : ISessionManager
{
    private readonly ConcurrentDictionary<string, ISession> _sessions = new();

    public void AddSession(ISession session)
    {
        if (_sessions.TryAdd(session.Id, session))
        {
            logger.LogInformation("Session {Id} added", session.Id);
        }
    }

    public void RemoveSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            session.Stop();
            logger.LogInformation("Session {Id} removed", sessionId);
        }
    }

    public ISession? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public IEnumerable<ISession> GetAllSessions() => _sessions.Values;
}