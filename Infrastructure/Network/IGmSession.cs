namespace PetitionD.Infrastructure.Network
{
    public interface IGmSession : ISession
    {
        void ClearCharacter(int worldId);
        void AddCharacter(GmCharacter character);
        int GetTryLoginCharUid(int worldId);
        void SetCharacter(int worldId, GmCharacter character);
        // ... other required methods
    }
}