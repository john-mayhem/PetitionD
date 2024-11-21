// File: Services/PetitionService.cs
using NC.PetitionLib;
using PetitionD.Core.Interfaces;
using PetitionD.Core.Models;
using PetitionD.Infrastructure.Database.Repositories;

public class PetitionService : IPetitionService
{
    private readonly PetitionRepository _petitionRepository;
    private readonly GmRepository _gmRepository;
    private readonly ILogger<PetitionService> _logger;

    public PetitionService(
        PetitionRepository petitionRepository,
        GmRepository gmRepository,
        ILogger<PetitionService> logger)
    {
        _petitionRepository = petitionRepository;
        _gmRepository = gmRepository;
        _logger = logger;
    }

    public async Task<PetitionErrorCode> SubmitPetitionAsync(Petition petition)
    {
        try
        {
            // Validate and set initial state
            petition.mState = State.Submit;
            petition.mSubmitTime = DateTime.Now;

            // Create petition
            var result = await _petitionRepository.CreatePetitionAsync(petition);
            if (result == PetitionErrorCode.Success)
            {
                // Update quota if needed
                if (petition.mForcedGm.CharUid == 0)
                {
                    await UpdateQuotaAsync(petition.mUser.AccountUid, 1);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting petition");
            return PetitionErrorCode.DatabaseFail;
        }
    }
}