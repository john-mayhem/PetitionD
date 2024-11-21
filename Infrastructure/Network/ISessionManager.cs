// File: Infrastructure/Network/ISessionManager.cs
namespace PetitionD.Infrastructure.Network;

public interface ISessionManager
{
    void AddSession(ISession session);
    void RemoveSession(string sessionId);
    ISession? GetSession(string sessionId);
    IEnumerable<ISession> GetAllSessions();
}