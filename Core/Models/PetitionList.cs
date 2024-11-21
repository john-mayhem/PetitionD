// File: Core/Models/PetitionList.cs
using NC.PetitionLib;
using PetitionD.Core.Services;
using PetitionD.Configuration;

namespace PetitionD.Core.Models;

public class PetitionList
{
    private readonly Dictionary<int, Petition> _activePetitions = new();
    private readonly Dictionary<int, Dictionary<int, Petition>> _worldPetitions = new();
    private readonly Dictionary<string, Petition> _completedPetitions = new();
    private readonly ILogger<PetitionList> _logger;
    private static int _lastPetitionId;
    private static DateTime _lastPetitionSeqDate = DateTime.Today;
    private static int _lastPetitionSeqSerial;

    private readonly object _lock = new();

    public PetitionList(ILogger<PetitionList> logger)
    {
        _logger = logger;
    }

    public static void InitializeSequence()
    {
        _lastPetitionSeqDate = DateTime.Today;
        _lastPetitionSeqSerial = 0;
    }

    private string GeneratePetitionSequence()
    {
        lock (_lock)
        {
            if (_lastPetitionSeqDate != DateTime.Today)
            {
                _lastPetitionSeqDate = DateTime.Today;
                _lastPetitionSeqSerial = 1;
            }
            else
            {
                _lastPetitionSeqSerial++;
            }

            return $"{_lastPetitionSeqDate:yyyyMMdd}{_lastPetitionSeqSerial:000000}";
        }
    }

    public PetitionErrorCode CreatePetition(
        int worldId,
        byte category,
        GameCharacter user,
        string content,
        GameCharacter forcedGm,
        Lineage2Info info,
        out Petition petition)
    {
        petition = new Petition();

        lock (_lock)
        {
            // Validate
            if (!Category.IsValid(category))
                return PetitionErrorCode.UnexpectedCategory;

            if (_activePetitions.Count >= Config.mMaxActivePetition)
                return PetitionErrorCode.TooManyPetitions;

            if (GetPetition(worldId, user.CharUid) != null)
                return PetitionErrorCode.CharAlreadySubmitted;

            // Set up petition
            petition.PetitionSeq = GeneratePetitionSequence();
            petition.WorldId = worldId;
            petition.Category = category;
            petition.User = user;
            petition.Content = content;
            petition.ForcedGm = forcedGm;
            petition.Info = info;
            petition.State = State.Submit;
            petition.Grade = Grade.GMS;
            petition.SubmitTime = DateTime.Now;
            petition.PetitionId = ++_lastPetitionId;

            // Set quota
            petition.QuotaAtSubmit = Quota.GetCurrentQuota(user.AccountUid);
            petition.QuotaAfterTreat = petition.QuotaAtSubmit + 1;

            if (petition.QuotaAtSubmit >= Config.mMaxQuota)
                return PetitionErrorCode.ExceedQuota;

            // Add to tracking collections
            _activePetitions.Add(petition.PetitionId, petition);

            if (!_worldPetitions.TryGetValue(worldId, out var worldPetitions))
            {
                worldPetitions = new Dictionary<int, Petition>();
                _worldPetitions[worldId] = worldPetitions;
            }

            worldPetitions[petition.PetitionId] = petition;

            // Update quota if not forced GM petition
            if (forcedGm.CharUid == 0)
            {
                Quota.UpdateQuota(user.AccountUid, 1);
            }

            return PetitionErrorCode.Success;
        }
    }

    public Petition? GetPetition(int petitionId)
    {
        lock (_lock)
        {
            return _activePetitions.TryGetValue(petitionId, out var petition) ? petition : null;
        }
    }

    public Petition? GetPetition(int worldId, int userCharUid)
    {
        lock (_lock)
        {
            if (_worldPetitions.TryGetValue(worldId, out var worldPetitions))
            {
                return worldPetitions.Values.FirstOrDefault(p => p.User.CharUid == userCharUid);
            }
            return null;
        }
    }

    public Dictionary<int, Petition> GetActivePetitionList(int worldId)
    {
        lock (_lock)
        {
            if (_worldPetitions.TryGetValue(worldId, out var worldPetitions))
            {
                return new Dictionary<int, Petition>(worldPetitions);
            }
            return new Dictionary<int, Petition>();
        }
    }

    public int GetActivePetitionCount(int worldId = 0)
    {
        lock (_lock)
        {
            if (worldId == 0)
                return _activePetitions.Count;

            return _worldPetitions.TryGetValue(worldId, out var worldPetitions)
                ? worldPetitions.Count
                : 0;
        }
    }

    public void MoveToCompletion(int petitionId)
    {
        lock (_lock)
        {
            if (_activePetitions.TryGetValue(petitionId, out var petition))
            {
                _activePetitions.Remove(petitionId);

                if (_worldPetitions.TryGetValue(petition.WorldId, out var worldPetitions))
                {
                    worldPetitions.Remove(petitionId);
                }

                _completedPetitions[petition.PetitionSeq] = petition;
                _logger.LogInformation("Petition {PetitionId} moved to completion", petitionId);
            }
        }
    }

    public void RemoveWorld(int worldId)
    {
        lock (_lock)
        {
            if (_worldPetitions.TryGetValue(worldId, out var worldPetitions))
            {
                foreach (var petition in worldPetitions.Values)
                {
                    _activePetitions.Remove(petition.PetitionId);
                }
                _worldPetitions.Remove(worldId);
                _logger.LogInformation("Removed all petitions for world {WorldId}", worldId);
            }
        }
    }

    public Petition? GetCompletedPetition(string petitionSeq)
    {
        lock (_lock)
        {
            return _completedPetitions.TryGetValue(petitionSeq, out var petition) ? petition : null;
        }
    }

    public void RemoveCompletedPetition(int petitionId)
    {
        lock (_lock)
        {
            var petition = _completedPetitions.Values.FirstOrDefault(p => p.PetitionId == petitionId);
            if (petition != null)
            {
                _completedPetitions.Remove(petition.PetitionSeq);
                _logger.LogInformation("Removed completed petition {PetitionId}", petitionId);
            }
        }
    }

    public void CleanupOldPetitions(TimeSpan age)
    {
        lock (_lock)
        {
            var now = DateTime.Now;
            var oldPetitions = _completedPetitions.Values
                .Where(p => now - p.CheckInTime > age)
                .ToList();

            foreach (var petition in oldPetitions)
            {
                _completedPetitions.Remove(petition.PetitionSeq);
            }

            if (oldPetitions.Any())
            {
                _logger.LogInformation("Cleaned up {Count} old petitions", oldPetitions.Count);
            }
        }
    }
}