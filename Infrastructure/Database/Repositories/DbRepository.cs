// File: Infrastructure/Database/Repositories/DbRepository.cs
namespace PetitionD.Infrastructure.Database.Repositories;

using Microsoft.Extensions.Logging;
using NC.PetitionLib;
using PetitionD.Core.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Transactions;

public class DbRepository : IDbRepository
{
    private readonly DbContext _dbContext;
    private readonly ILogger<DbRepository> _logger;

    public DbRepository(DbContext dbContext, ILogger<DbRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(bool IsValid, int AccountUid, Grade Grade)> ValidateGmCredentialsAsync(
        string account,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new
            {
                Account = account,
                Password = password
            };

            return await _dbContext.ExecuteStoredProcAsync<(bool, int, Grade)>(
                "up_Server_ValidateGM",
                parameters,
                async (reader, token) =>
                {
                    if (!await reader.ReadAsync(token))
                        return (false, 0, Grade.User);

                    return (
                        true,
                        reader.GetInt32(0),  // AccountUid
                        (Grade)reader.GetByte(1)  // Grade
                    );
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate GM credentials for {Account}", account);
            return (false, 0, Grade.User);
        }
    }

    public async Task<(PetitionErrorCode ErrorCode, string PetitionSeq)> CreatePetitionAsync(
        Petition petition,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var parameters = new
            {
                Seq = petition.PetitionSeq,
                Category = (byte)petition.Category,
                WorldId = (byte)petition.WorldId,
                AccountName = petition.User.AccountName,
                AccountUid = petition.User.AccountUid,
                CharName = petition.User.CharName,
                CharUid = petition.User.CharUid,
                Content = petition.Content,
                ForcedGmAccountName = petition.ForcedGm.AccountName,
                ForcedGmAccountUid = petition.ForcedGm.AccountUid,
                ForcedGmCharName = petition.ForcedGm.CharName,
                ForcedGmCharUid = petition.ForcedGm.CharUid,
                QuotaAtSubmit = (byte)petition.QuotaAtSubmit,
                Time = petition.SubmitTime
            };

            var result = await _dbContext.ExecuteStoredProcAsync<(PetitionErrorCode, string)>(
                "up_Server_InsertPetition",
                parameters,
                async (reader, token) =>
                {
                    if (!await reader.ReadAsync(token))
                        return (PetitionErrorCode.DatabaseFail, string.Empty);

                    return (
                        (PetitionErrorCode)reader.GetByte(0),
                        reader.GetString(1)  // PetitionSeq
                    );
                },
                cancellationToken);

            if (result.Item1 == PetitionErrorCode.Success)
            {
                await InsertLineageInfoAsync(result.Item2, petition.Info, cancellationToken);
                scope.Complete();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create petition");
            return (PetitionErrorCode.DatabaseFail, string.Empty);
        }
    }

    private async Task InsertLineageInfoAsync(
        string petitionSeq,
        Lineage2Info info,
        CancellationToken cancellationToken)
    {
        var parameters = new
        {
            PetitionSeq = petitionSeq,
            Race = info.Race,
            Class = info.Class,
            Level = info.Level,
            Disposition = info.Disposition,
            SsPosition = info.SsPosition,
            NewChar = info.NewChar,
            Coordinate = info.Coordinate
        };

        await _dbContext.ExecuteStoredProcAsync(
            "up_Server_AddL2Info",
            parameters,
            cancellationToken);
    }

    public async Task<PetitionErrorCode> UpdatePetitionStateAsync(
        string petitionSeq,
        State newState,
        GameCharacter actor,
        string? message = null,
        byte? flag = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new
            {
                Seq = petitionSeq,
                State = (byte)newState,
                AccountName = actor.AccountName,
                AccountUid = actor.AccountUid,
                CharName = actor.CharName,
                CharUid = actor.CharUid,
                Message = message ?? string.Empty,
                Flag = flag ?? 0,
                Time = DateTime.UtcNow
            };

            var spName = DetermineStateUpdateStoredProc(newState);
            await _dbContext.ExecuteStoredProcAsync(
                spName,
                parameters,
                cancellationToken);

            return PetitionErrorCode.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update petition state to {State} for {PetitionSeq}",
                newState, petitionSeq);
            return PetitionErrorCode.DatabaseFail;
        }
    }

    private static string DetermineStateUpdateStoredProc(State state) => state switch
    {
        State.CheckOut => "up_Server_CheckOut",
        State.MessageCheckIn => "up_Server_MessageCheckIn",
        State.ChatCheckIn => "up_Server_ChattingCheckIn",
        State.Forward => "up_Server_ForwardCheckIn",
        State.Undo => "up_Server_UndoCheckOut",
        State.UserCancel => "up_Server_UserCancel",
        _ => throw new ArgumentException($"Unsupported state update: {state}")
    };

    public async Task<PetitionErrorCode> UpdateQuotaAsync(
        int accountUid,
        int delta,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new
            {
                AccountUid = accountUid,
                Delta = delta
            };

            await _dbContext.ExecuteStoredProcAsync(
                "up_Server_UpdateQuota",
                parameters,
                cancellationToken);

            return PetitionErrorCode.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update quota for account {AccountUid}", accountUid);
            return PetitionErrorCode.DatabaseFail;
        }
    }
}