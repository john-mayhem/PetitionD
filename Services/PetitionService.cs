// File: Services/PetitionService.cs
namespace PetitionD.Services;

using Microsoft.Extensions.Logging;
using NC.PetitionLib;
using PetitionD.Core.Interfaces;
using PetitionD.Core.Models;
using PetitionD.Core.Services;
using PetitionD.Infrastructure.Database.Repositories;

public class PetitionService : IPetitionService
{
    private readonly PetitionRepository _petitionRepository;
    private readonly QuotaService _quotaService;
    private readonly ILogger<PetitionService> _logger;

    public PetitionService(
        PetitionRepository petitionRepository,
        QuotaService quotaService,
        ILogger<PetitionService> logger)
    {
        _petitionRepository = petitionRepository;
        _quotaService = quotaService;
        _logger = logger;
    }

    public async Task<PetitionErrorCode> SubmitPetitionAsync(Petition petition)
    {
        try
        {
            // Validate and set initial state
            petition.State = State.Submit;
            petition.SubmitTime = DateTime.Now;

            // Create petition
            var (errorCode, _) = await _petitionRepository.CreatePetitionAsync(petition);
            if (errorCode == PetitionErrorCode.Success)
            {
                // Update quota if needed
                if (petition.ForcedGm.CharUid == 0)
                {
                    await _quotaService.UpdateQuotaAsync(petition.User.AccountUid, 1);
                }
            }

            return errorCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting petition");
            return PetitionErrorCode.DatabaseFail;
        }
    }
}