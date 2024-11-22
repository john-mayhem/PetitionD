// File: Infrastructure/Network/Extensions/WorldSessionExtensions.cs 
namespace PetitionD.Infrastructure.Network.Extensions;

public static class WorldSessionExtensions
{
    public static IEnumerable<GmSession> GetGmSessions(this WorldSession session)
    {
        return session._gmSessions.Values;
    }
}