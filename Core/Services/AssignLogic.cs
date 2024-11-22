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
    private static readonly object _syncLock = new();

    public static void Initialize(PetitionList petitionList, WorldSessionManager worldSessionManager)
    {
        _petitionList = petitionList;
        _worldSessionManager = worldSessionManager;
    }

    public static List<Petition> Assign(GmCharacter gmChar, out int assignCount)
    {
        var result = new List<Petition>();
        assignCount = 0;

        if (!Config.EnableAssignment || gmChar.Grade > Grade.GMS || _petitionList == null)
            return result;

        lock (_syncLock)
        {
            var activePetitions = _petitionList.GetActivePetitionList(gmChar.WorldId);

            // First pass: Assign forced GM petitions
            foreach (var petition in activePetitions.Values)
            {
                if (petition.Grade > Grade.GMS)
                    continue;

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
            }

            // Second pass: Assign unassigned petitions
            if (assignCount < Config.MaxAssignmentPerGm)
            {
                var unassignedPetitions = activePetitions.Values
                    .Where(p => p.Grade <= Grade.GMS &&
                               p.AssignedGm.CharUid == 0 &&
                               p.ForcedGm.CharUid == 0)
                    .OrderBy(p => p.PetitionId)
                    .ToList();

                foreach (var petition in unassignedPetitions)
                {
                    if (assignCount >= Config.MaxAssignmentPerGm)
                        break;

                    petition.AssignedGm = gmChar.ToGameCharacter();
                    result.Add(petition);
                    assignCount++;
                }
            }
        }

        return result;
    }

    public static bool CanCheckOut(Petition petition, GmCharacter gmChar)
    {
        if (!Config.EnableAssignment)
            return true;

        return petition.AssignedGm.CharUid == 0 ||
               petition.AssignedGm.CharUid == gmChar.CharUid ||
               gmChar.Grade > Grade.GMS;
    }

    public static bool CheckOut(Petition petition, GmCharacter gmChar)
    {
        if (!Config.EnableAssignment)
        {
            Reset(petition);
            return false;
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
        if (!Config.EnableAssignment || _petitionList == null)
            return result;

        lock (_syncLock)
        {
            var activePetitions = _petitionList.GetActivePetitionList(gmChar.WorldId);

            foreach (var petition in activePetitions.Values)
            {
                if ((petition.State == State.Submit || petition.State == State.Undo) &&
                    petition.AssignedGm.CharUid == gmChar.CharUid)
                {
                    petition.AssignedGm = new GameCharacter();
                    result.Add(petition);
                }
            }
        }

        return result;
    }

    public static bool Reset(Petition petition)
    {
        if (!Config.EnableAssignment || _worldSessionManager == null)
            return false;

        lock (_syncLock)
        {
            var worldSession = _worldSessionManager.GetSession(petition.WorldId);
            if (worldSession == null)
                return false;

            foreach (var gmSession in worldSession.GetGmSessions())
            {
                var character = gmSession.GetCharacter(petition.WorldId);
                if (character?.CharUid == petition.AssignedGm.CharUid)
                {
                    character.AssignCount--;
                    gmSession.SetCharacter(petition.WorldId, character);
                    break;
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

        lock (_syncLock)
        {
            var minAssignCount = Config.MaxAssignmentPerGm;
            GmCharacter? selectedGm = null;
            GmSession? selectedSession = null;

            foreach (var gmSession in worldSession.GetGmSessions())
            {
                var character = gmSession.GetCharacter(worldId);
                if (character == null || character.AssignCount >= Config.MaxAssignmentPerGm)
                    continue;

                if (petition.ForcedGm.CharUid != 0)
                {
                    if (petition.ForcedGm.CharUid == character.CharUid)
                    {
                        selectedGm = character;
                        selectedSession = gmSession;
                        break;
                    }
                }
                else if (character.Grade == Grade.GMS &&
                         character.AssignCount < minAssignCount)
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
}