// File: Core/Services/AssignLogic.cs
using NC.PetitionLib;
using PetidionD.Core.Models;
using PetitionD.Configuration;

namespace PetidionD.Core.Services;

public static class AssignLogic
{
    private static PetitionList? _petitionList;
    private static WorldSessionManager? _worldSessionManager;

    public static void Initialize(PetitionList petitionList, WorldSessionManager worldSessionManager)
    {
        _petitionList = petitionList;
        _worldSessionManager = worldSessionManager;
    }

    public static List<Petition> Assign(GmCharacter gmChar, out int assignCount)
    {
        var result = new List<Petition>();
        assignCount = 0;

        if (!Config.mEnableAssignment || gmChar.Grade > Grade.GMS)
            return result;

        if (_petitionList == null)
            return result;

        lock (_petitionList)
        {
            var activePetitions = _petitionList.GetActivePetitionList(gmChar.WorldId);
            var sortedPetitions = new SortedList<int, Petition>();

            foreach (var petition in activePetitions)
            {
                if (petition.Grade <= Grade.GMS)
                {
                    if (petition.CheckOutGm.CharUid != 0)
                    {
                        if (petition.CheckOutGm.CharUid == gmChar.CharUid)
                        {
                            petition.AssignedGm = gmChar.ToGameCharacter();
                            assignCount++;
                        }
                    }
                    else if (petition.ForcedGm.CharUid != 0)
                    {
                        if (petition.ForcedGm.CharUid == gmChar.CharUid)
                        {
                            petition.AssignedGm = gmChar.ToGameCharacter();
                            assignCount++;
                            result.Add(petition);
                        }
                    }
                    else if (petition.AssignedGm.CharUid == 0)
                    {
                        sortedPetitions.Add(petition.PetitionId, petition);
                    }
                }
            }

            foreach (var petition in sortedPetitions.Values)
            {
                if (assignCount < Config.mMaxAssignmentPerGm)
                {
                    lock (petition)
                    {
                        petition.AssignedGm = gmChar.ToGameCharacter();
                        result.Add(petition);
                        assignCount++;
                    }
                }
                else break;
            }
        }

        return result;
    }

    public static bool CanCheckOut(Petition petition, GmCharacter gmChar)
    {
        if (!Config.mEnableAssignment)
            return true;

        return petition.AssignedGm.CharUid == 0
            || petition.AssignedGm.CharUid == gmChar.CharUid
            || gmChar.Grade > Grade.GMS;
    }

    public static bool CheckOut(Petition petition, GmCharacter gmChar)
    {
        if (!Config.mEnableAssignment)
        {
            Reset(petition);
        }

        var needsReassign = gmChar.Grade > Grade.GMS && petition.Grade == Grade.GMS;
        if (needsReassign)
        {
            Reset(petition);
        }

        return needsReassign;
    }

    public static List<Petition> Reset(GmCharacter gmChar)
    {
        var result = new List<Petition>();
        if (!Config.mEnableAssignment)
            return result;

        if (_petitionList == null)
            return result;

        lock (_petitionList)
        {
            var activePetitions = _petitionList.GetActivePetitionList(gmChar.WorldId);
            foreach (var petition in activePetitions)
            {
                if ((petition.State == State.Submit || petition.State == State.Undo)
                    && petition.AssignedGm.CharUid == gmChar.CharUid)
                {
                    lock (petition)
                    {
                        petition.AssignedGm = new GameCharacter();
                        result.Add(petition);
                    }
                }
            }
        }

        return result;
    }

    public static bool Reset(Petition petition)
    {
        if (!Config.mEnableAssignment)
            return false;

        if (_worldSessionManager == null)
            return false;

        lock (petition)
        {
            var worldSession = _worldSessionManager.GetSession(petition.WorldId);
            if (worldSession != null)
            {
                foreach (var gmSession in worldSession.GetGmSessions())
                {
                    var character = gmSession.GetCharacter(petition.WorldId);
                    if (character.CharUid == petition.AssignedGm.CharUid)
                    {
                        character.AssignCount--;
                        gmSession.SetCharacter(petition.WorldId, character);
                        break;
                    }
                }
            }

            petition.AssignedGm = new GameCharacter();
        }

        return true;
    }

    public static GameCharacter GetAvailableGm(Petition petition, int worldId)
    {
        if (!Config.mEnableAssignment || _worldSessionManager == null)
            return new GameCharacter();

        var worldSession = _worldSessionManager.GetSession(worldId);
        if (worldSession == null)
            return new GameCharacter();

        var minAssignCount = Config.mMaxAssignmentPerGm;
        GmCharacter? selectedGm = null;
        GmSession? selectedSession = null;

        foreach (var gmSession in worldSession.GetGmSessions())
        {
            var character = gmSession.GetCharacter(worldId);
            if (character.AssignCount >= Config.mMaxAssignmentPerGm)
                continue;

            if (petition.ForcedGm.CharUid != 0)
            {
                if (petition.ForcedGm.CharUid == character.CharUid)
                {
                    minAssignCount = character.AssignCount;
                    selectedGm = character;
                    selectedSession = gmSession;
                    break;
                }
            }
            else if (character.Grade == Grade.GMS && minAssignCount > character.AssignCount)
            {
                minAssignCount = character.AssignCount;
                selectedGm = character;
                selectedSession = gmSession;
            }
        }

        if (selectedGm == null)
            return new GameCharacter();

        selectedGm.AssignCount++;
        selectedSession?.SetCharacter(worldId, selectedGm);

        return selectedGm.ToGameCharacter();
    }
}