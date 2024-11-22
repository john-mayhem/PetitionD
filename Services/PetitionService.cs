using NC.PetitionLib;
using PetitionD.Core.Models;
using PetitionD.Core.Services;
using PetitionD.Infrastructure.Database.Repositories;
using System.Transactions;

namespace PetitionD.Services;

public class PetitionService : IPetitionService
{
    private readonly PetitionRepository _petitionRepository;
    private readonly QuotaService _quotaService;
    private readonly ILogger<PetitionService> _logger;
    private readonly PetitionList _petitionList;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public PetitionService(
        PetitionRepository petitionRepository,
        QuotaService quotaService,
        PetitionList petitionList,
        ILogger<PetitionService> logger)
    {
        _petitionRepository = petitionRepository;
        _quotaService = quotaService;
        _petitionList = petitionList;
        _logger = logger;
    }

    public async Task<(PetitionErrorCode ErrorCode, Petition? Petition)> SubmitPetitionAsync(
        int worldId,
        byte category,
        GameCharacter user,
        string content,
        GameCharacter forcedGm,
        Lineage2Info info,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _syncLock.WaitAsync(cancellationToken);

            // Validate
            if (!Category.IsValid(category))
                return (PetitionErrorCode.UnexpectedCategory, null);

            // Check quota if not forced GM petition
            if (forcedGm.CharUid == 0)
            {
                var (isValid, currentQuota) = await _quotaService.ValidateQuotaAsync(
                    user.AccountUid, cancellationToken);

                if (!isValid)
                    return (PetitionErrorCode.ExceedQuota, null);
            }

            using var scope = new TransactionScope(
                TransactionScopeAsyncFlowOption.Enabled);

            // Create petition
            var result = await _petitionRepository.CreatePetitionAsync(
                worldId, category, user, content, forcedGm, info, cancellationToken);

            if (result.ErrorCode != PetitionErrorCode.Success)
                return (result.ErrorCode, null);

            // Update quota if not forced GM petition
            if (forcedGm.CharUid == 0)
            {
                await _quotaService.UpdateQuotaAsync(user.AccountUid, 1, cancellationToken);
            }

            // Load the created petition
            var petition = await _petitionRepository.GetPetitionByIdAsync(
                result.PetitionId, cancellationToken);

            if (petition == null)
            {
                _logger.LogError("Failed to load newly created petition {PetitionId}",
                    result.PetitionId);
                return (PetitionErrorCode.DatabaseFail, null);
            }

            // Add to in-memory list
            _petitionList.AddPetition(petition);

            scope.Complete();
            return (PetitionErrorCode.Success, petition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting petition for user {UserCharName}",
                user.CharName);
            return (PetitionErrorCode.DatabaseFail, null);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<PetitionErrorCode> UpdatePetitionStateAsync(
        int petitionId,
        State newState,
        GameCharacter actor,
        string? message = null,
        byte? flag = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _syncLock.WaitAsync(cancellationToken);

            var petition = _petitionList.GetPetition(petitionId);
            if (petition == null)
                return PetitionErrorCode.UnexpectedPetitionId;

            using var scope = new TransactionScope(
                TransactionScopeAsyncFlowOption.Enabled);

            // Update in database
            var result = await _petitionRepository.UpdatePetitionStateAsync(
                petition.PetitionSeq, newState, actor, message, flag, cancellationToken);

            if (result != PetitionErrorCode.Success)
                return result;

            // Update in memory
            petition.State = newState;
            petition.LastActionTime = DateTime.Now;
            if (flag.HasValue)
                petition.Flag = flag.Value;

            // Handle quota changes
            if (newState == State.MessageCheckIn || newState == State.ChatCheckIn)
            {
                if ((flag ?? 0 & 2) != 0 && petition.ForcedGm.CharUid == 0)
                {
                    await _quotaService.UpdateQuotaAsync(
                        petition.User.AccountUid, -1, cancellationToken);
                }
                _petitionList.MoveToCompletion(petitionId);
            }

            scope.Complete();
            return PetitionErrorCode.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating petition {PetitionId} state to {NewState}",
                petitionId, newState);
            return PetitionErrorCode.DatabaseFail;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<PetitionErrorCode> AddMemoAsync(
        int petitionId,
        GameCharacter gm,
        string content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var petition = _petitionList.GetPetition(petitionId);
            if (petition == null)
                return PetitionErrorCode.UnexpectedPetitionId;

            if (petition.State != State.CheckOut && petition.State != State.EndChat)
                return PetitionErrorCode.InvalidState;

            if (petition.CheckOutGm.CharUid != gm.CharUid)
                return PetitionErrorCode.NoRightToAccess;

            return await _petitionRepository.AddMemoAsync(
                petition.PetitionSeq, content, gm, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding memo to petition {PetitionId}", petitionId);
            return PetitionErrorCode.DatabaseFail;
        }
    }

    public async Task<IEnumerable<Petition>> GetActivePetitionsForWorldAsync(
        int worldId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _petitionRepository.GetActivePetitionsForWorldAsync(
                worldId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active petitions for world {WorldId}", worldId);
            return Enumerable.Empty<Petition>();
        }
    }

    public async Task<PetitionErrorCode> CancelPetitionAsync(
        int petitionId,
        GameCharacter requester,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _syncLock.WaitAsync(cancellationToken);

            var petition = _petitionList.GetPetition(petitionId);
            if (petition == null)
                return PetitionErrorCode.UnexpectedPetitionId;

            using var scope = new TransactionScope(
                TransactionScopeAsyncFlowOption.Enabled);

            // Cancel in database
            var result = await _petitionRepository.UpdatePetitionStateAsync(
                petition.PetitionSeq,
                State.UserCancel,
                requester,
                cancellationToken: cancellationToken);

            if (result != PetitionErrorCode.Success)
                return result;

            // Update quota if needed
            if (petition.ForcedGm.CharUid == 0)
            {
                await _quotaService.UpdateQuotaAsync(
                    petition.User.AccountUid, -1, cancellationToken);
            }

            // Remove from active list
            _petitionList.RemoveActivePetition(petitionId);

            scope.Complete();
            return PetitionErrorCode.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling petition {PetitionId}", petitionId);
            return PetitionErrorCode.DatabaseFail;
        }
        finally
        {
            _syncLock.Release();
        }
    }
}