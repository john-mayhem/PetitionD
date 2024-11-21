// File: Infrastructure/Database/IDbRepository.cs
using NC.PetitionLib;
using PetitionD.Core.Enums;
using PetitionD.Core.Models;

namespace PetitionD.Infrastructure.Database;

public interface IDbRepository
{
    // Authentication
    Task<(bool IsValid, int AccountUid)> ValidateGmCredentialsAsync(string account, string password);

    // Petition Operations
    Task<PetitionErrorCode> CreatePetitionAsync(Petition petition);
    Task<PetitionErrorCode> UpdatePetitionStateAsync(int petitionId, PetitionState newState);
    Task<Petition?> GetPetitionByIdAsync(int petitionId);
    Task<IEnumerable<Petition>> GetActivePetitionsForWorldAsync(int worldId);

    // GM Operations
    Task<PetitionErrorCode> AddMemoAsync(int petitionId, string content, string gmName);
    Task<PetitionErrorCode> ModifyCategoryAsync(int petitionId, int newCategory);
    Task<PetitionErrorCode> ForwardPetitionAsync(int petitionId, Grade newGrade);

    // Template Operations
    Task<IEnumerable<Template>> GetTemplatesForGmAsync(int gmAccountUid);
    Task<PetitionErrorCode> UpdateTemplateAsync(Template template);
    Task<PetitionErrorCode> DeleteTemplateAsync(int templateId);
}
