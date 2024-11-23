// File: Infrastructure/Network/IWorldSessionManager.cs
namespace PetitionD.Infrastructure.Network;

public interface IWorldSessionManager
{
    event Action<WorldSession> SessionCreated;
    void AddSession(WorldSession session);
    void RemoveSession(int worldId);
    WorldSession? GetSession(int worldId);
    IEnumerable<WorldSession> GetAllSessions();
}