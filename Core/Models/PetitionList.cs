using NC.PetitionLib;

namespace PetidionD.Core.Models;

public class PetitionList
{
    private readonly Dictionary<int, Petition> _activePetitions = new();
    private readonly Dictionary<int, Dictionary<int, Petition>> _worldPetitions = new();

    public Petition? GetPetition(int worldId, int userCharUid)
    {
        if (_worldPetitions.TryGetValue(worldId, out var worldPetitions))
        {
            return worldPetitions.Values.FirstOrDefault(p => p.mUser.CharUid == userCharUid);
        }
        return null;
    }

    public Petition? GetPetition(int petitionId)
    {
        return _activePetitions.TryGetValue(petitionId, out var petition) ? petition : null;
    }

    public void RemoveActivePetition(int petitionId)
    {
        _activePetitions.Remove(petitionId);
    }

    public int GetActivePetitionCount(int worldId = 0)
    {
        if (worldId == 0)
            return _activePetitions.Count;

        return _worldPetitions.TryGetValue(worldId, out var petitions) ? petitions.Count : 0;
    }
}