// File: Core/Models/PetitionList.cs
using NC.PetitionLib;
using PetitionD.Core.Services;
using PetitionD.Configuration;
using PetitionD.Core.Enums;
using Microsoft.Data.SqlClient;

namespace PetitionD.Core.Models;

public class PetitionList
{
    private readonly ILogger<PetitionList> _logger;
    private readonly Dictionary<int, Petition> _activePetitions = new();
    private readonly Dictionary<int, Dictionary<int, Petition>> _worldPetitions = new();
    private readonly Dictionary<string, Petition> _completedPetitions = new();
    private static int _lastPetitionId;
    private static DateTime _lastPetitionSeqDate = DateTime.Today;
    private static int _lastPetitionSeqSerial;
    private readonly object _lock = new();

    public PetitionList(ILogger<PetitionList> logger)
    {
        _logger = logger;
    }

    public static bool InitializeSequence()
    {
        try
        {
            _lastPetitionSeqDate = DateTime.Today;
            _lastPetitionSeqSerial = 0;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
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

            var seq = $"{_lastPetitionSeqDate:yyyyMMdd}{_lastPetitionSeqSerial:000000}";
            _logger.LogDebug("Generated petition sequence: {Seq}", seq);
            return seq;
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

        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("Empty petition content from user {User}", user.CharName);
            return PetitionErrorCode.UnexpectedPetitionId;
        }

        lock (_lock)
        {
            try
            {
                // Basic validation
                if (!Category.IsValid(category))
                {
                    _logger.LogWarning("Invalid category: {Category}", category);
                    return PetitionErrorCode.UnexpectedCategory;
                }

                if (_activePetitions.Count >= Config.MaxActivePetition)
                {
                    _logger.LogWarning("Max active petitions reached");
                    return PetitionErrorCode.TooManyPetitions;
                }

                if (GetPetition(worldId, user.CharUid) != null)
                {
                    _logger.LogWarning("User already has active petition - CharUid: {CharUid}", user.CharUid);
                    return PetitionErrorCode.CharAlreadySubmitted;
                }

                // Generate sequence
                if (_lastPetitionSeqDate == DateTime.Today)
                {
                    _lastPetitionSeqSerial++;
                }
                else
                {
                    _lastPetitionSeqDate = DateTime.Today;
                    _lastPetitionSeqSerial = 1;
                }

                var sequenceStr = _lastPetitionSeqDate.ToString("yyyyMMdd") +
                                _lastPetitionSeqSerial.ToString("000000");

                // Set up petition
                petition.PetitionSeq = sequenceStr;
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

                // Get quota if not forced GM petition
                if (forcedGm.CharUid == 0)
                {
                    petition.QuotaAtSubmit = Quota.GetCurrentQuota(user.AccountUid);
                    petition.QuotaAfterTreat = petition.QuotaAtSubmit + 1;

                    if (petition.QuotaAtSubmit >= Config.MaxQuota)
                    {
                        _logger.LogWarning("User exceeded quota limit: {User}", user.CharName);
                        return PetitionErrorCode.ExceedQuota;
                    }
                }

                // Store in memory
                _activePetitions.Add(petition.PetitionId, petition);

                if (!_worldPetitions.TryGetValue(worldId, out var worldPetitions))
                {
                    worldPetitions = new Dictionary<int, Petition>();
                    _worldPetitions[worldId] = worldPetitions;
                }

                worldPetitions[petition.PetitionId] = petition;

                _logger.LogInformation(
                    "Created petition - ID: {PetitionId}, Seq: {Seq}, User: {User}, Category: {Category}, Content: {ContentPreview}",
                    petition.PetitionId,
                    petition.PetitionSeq,
                    petition.User.CharName,
                    petition.Category,
                    content.Length > 50 ? content[..47] + "..." : content
                );

                return PetitionErrorCode.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating petition");
                return PetitionErrorCode.DatabaseFail;
            }
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
            return [];
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

            if (oldPetitions.Count != 0)
            {
                _logger.LogInformation("Cleaned up {Count} old petitions", oldPetitions.Count);
            }
        }
    }

    public void AddPetition(Petition petition)
    {
        lock (_lock)
        {
            _activePetitions[petition.PetitionId] = petition;

            if (!_worldPetitions.TryGetValue(petition.WorldId, out var worldPetitions))
            {
                worldPetitions = [];
                _worldPetitions[petition.WorldId] = worldPetitions;
            }

            worldPetitions[petition.PetitionId] = petition;
        }
    }

    public Petition RemoveActivePetition(int petitionId)
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

                return petition;
            }
            return null;
        }
    }
}