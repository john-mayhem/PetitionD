// File: Infrastructure/Network/ISessionManager.cs

namespace PetitionD.Infrastructure.Network;

public interface ISessionManager
{
    void AddSession(ISession session);
    void RemoveSession(string sessionId);
    ISession? GetSession(string sessionId);
    IEnumerable<ISession> GetAllSessions();
    int GetActiveSessionCount();
    void BroadcastToAll(byte[] data);
    bool HasSession(string sessionId);
    void RemoveAllSessions();

    event Action<ISession> SessionCreated;
}