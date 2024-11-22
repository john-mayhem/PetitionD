// File: Core/Services/AssignLogic.cs
using NC.PetitionLib;
using PetitionD.Core.Extensions;
using PetitionD.Core.Models;
using PetitionD.Configuration;
using PetitionD.Core.Enums;
using PetitionD.Infrastructure.Network.Extensions;

namespace PetitionD.Core.Services;

public static class AssignLogic
{
    private static PetitionList? _petitionList;
    private static WorldSessionManager? _worldSessionManager;

    public static void Initialize(PetitionList petitionList, WorldSessionManager worldSessionManager)
    {
        _petitionList = petitionList;
        _worldSessionManager = worldSessionManager;
    }

    public static List<Petition> Assign(int worldId)
    {
        var result = new List<Petition>();
        if (!Config.EnableAssignment || _worldSessionManager == null)
            return result;

        var worldSession = _worldSessionManager.GetSession(worldId);
        if (worldSession == null)
            return result;

        var availableGms = new List<GmCharacter>();
        foreach (var gmSession in worldSession.GetGmSessions())
        {
            var character = gmSession.GetCharacter(worldId);
            if (character.Grade <= Grade.GMS && character.AssignCount < Config.MaxAssignmentPerGm)
            {
                availableGms.Add(character);
            }
        }

        if (_petitionList == null)
            return result;

        var activePetitions = _petitionList.GetActivePetitionList(worldId);
        var unassignedPetitions = new List<Petition>();

        foreach (var petition in activePetitions)
        {
            if ((petition.State == State.Submit || petition.State == State.Undo)
                && petition.Grade <= Grade.GMS)
            {
                if (petition.ForcedGm.CharUid != 0)
                {
                    var gmSession = worldSession.GetGmSession(petition.ForcedGm.CharUid);
                    if (gmSession != null)
                    {
                        var character = gmSession.GetCharacter(petition.WorldId);
                        if (character.AssignCount < Config.MaxAssignmentPerGm)
                        {
                            character.AssignCount++;
                            gmSession.SetCharacter(petition.WorldId, character);
                            petition.AssignedGm = character.ToGameCharacter();
                            result.Add(petition);
                        }
                    }
                }
                else if (petition.AssignedGm.CharUid == 0)
                {
                    unassignedPetitions.Add(petition);
                }
            }
        }

        // Sort GMs by assignment count
        availableGms.Sort((a, b) => a.AssignCount.CompareTo(b.AssignCount));

        // Assign remaining petitions
        foreach (var petition in unassignedPetitions)
        {
            if (availableGms.Count == 0)
                break;

            var selectedGm = availableGms[0];
            if (selectedGm.AssignCount >= Config.MaxAssignmentPerGm)
                break;

            selectedGm.AssignCount++;
            var gmSession = worldSession.GetGmSession(selectedGm.CharUid);
            if (gmSession != null)
            {
                gmSession.SetCharacter(worldId, selectedGm);
                petition.AssignedGm = selectedGm.ToGameCharacter();
                result.Add(petition);

                // Re-sort GMs if needed
                if (availableGms.Count > 1 && selectedGm.AssignCount > availableGms[1].AssignCount)
                {
                    availableGms.Sort((a, b) => a.AssignCount.CompareTo(b.AssignCount));
                }
            }
        }

        return result;
    }

    public static bool CanCheckOut(Petition petition, GmCharacter gmChar)
    {
        if (!Config.EnableAssignment)
            return true;

        return petition.AssignedGm.CharUid == 0
            || petition.AssignedGm.CharUid == gmChar.CharUid
            || gmChar.Grade > Grade.GMS;
    }

    public static bool CheckOut(Petition petition, GmCharacter gmChar)
    {
        if (!Config.EnableAssignment)
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
        if (!Config.EnableAssignment)
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
        if (!Config.EnableAssignment)
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
        if (!Config.EnableAssignment || _worldSessionManager == null)
            return new GameCharacter();

        var worldSession = _worldSessionManager.GetSession(worldId);
        if (worldSession == null)
            return new GameCharacter();

        var minAssignCount = Config.MaxAssignmentPerGm;
        GmCharacter? selectedGm = null;
        GmSession? selectedSession = null;

        foreach (var gmSession in worldSession.GetGmSessions())
        {
            var character = gmSession.GetCharacter(worldId);
            if (character.AssignCount >= Config.MaxAssignmentPerGm)
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