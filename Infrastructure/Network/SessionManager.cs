﻿// File: Infrastructure/Network/SessionManager.cs

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PetitionD.Infrastructure.Network.Sessions;

namespace PetitionD.Infrastructure.Network;

public class SessionManager(ILogger<SessionManager> logger) : ISessionManager
{
    private readonly ConcurrentDictionary<string, ISession> _sessions = new();
    private readonly ILogger<SessionManager> _logger = logger;
    public event Action<ISession> SessionCreated;

    public void AddSession(ISession session)
    {
        if (_sessions.TryAdd(session.Id, session))
        {
            SessionCreated?.Invoke(session);
            _logger.LogInformation("Session {Id} added", session.Id);
        }
        else
        {
            _logger.LogWarning("Failed to add session {Id} - already exists", session.Id);
        }
    }

    public void RemoveSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            session.Stop();
            _logger.LogInformation("Session {Id} removed", sessionId);
        }
    }

    public ISession? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public IEnumerable<ISession> GetAllSessions() => _sessions.Values;

    public int GetActiveSessionCount() => _sessions.Count;

    public void BroadcastToAll(byte[] data)
    {
        foreach (var session in _sessions.Values)
        {
            try
            {
                session.Send(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast to session {SessionId}", session.Id);
            }
        }
    }

    public bool HasSession(string sessionId) => _sessions.ContainsKey(sessionId);

    public void RemoveAllSessions()
    {
        foreach (var session in _sessions.Values)
        {
            try
            {
                session.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping session {SessionId}", session.Id);
            }
        }
        _sessions.Clear();
        _logger.LogInformation("All sessions removed");
    }
}