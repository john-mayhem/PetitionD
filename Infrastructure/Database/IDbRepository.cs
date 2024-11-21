// File: Infrastructure/Database/IDbRepository.cs
namespace PetitionD.Infrastructure.Database;

using NC.PetitionLib;
using PetitionD.Core.Enums;
using PetitionD.Core.Models;

public interface IDbRepository
{
    // Authentication
    Task<(bool IsValid, int AccountUid, Grade Grade)> ValidateGmCredentialsAsync(
        string account,
        string password,
        CancellationToken cancellationToken = default);

    // Petition Operations
    Task<(PetitionErrorCode ErrorCode, string PetitionSeq)> CreatePetitionAsync(
        Petition petition,
        CancellationToken cancellationToken = default);

    Task<PetitionErrorCode> UpdatePetitionStateAsync(
        string petitionSeq,
        State newState,
        GameCharacter actor,
        string? message = null,
        byte? flag = null,
        CancellationToken cancellationToken = default);

    Task<Petition?> GetPetitionByIdAsync(
        int petitionId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Petition>> GetActivePetitionsForWorldAsync(
        int worldId,
        CancellationToken cancellationToken = default);

    // Template Operations
    Task<IEnumerable<Template>> GetTemplatesForGmAsync(
        int gmAccountUid,
        CancellationToken cancellationToken = default);

    Task<PetitionErrorCode> UpdateTemplateAsync(
        Template template,
        CancellationToken cancellationToken = default);

    Task<PetitionErrorCode> DeleteTemplateAsync(
        int templateId,
        CancellationToken cancellationToken = default);

    // Quota Operations
    Task<PetitionErrorCode> UpdateQuotaAsync(
        int accountUid,
        int delta,
        CancellationToken cancellationToken = default);
}