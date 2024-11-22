using Microsoft.Data.SqlClient;
using NC.PetitionLib;
using PetitionD.Core.Models;
using System.Data;
using System.Transactions;
using Dapper;
using PetitionD.Core.Enums;

namespace PetitionD.Infrastructure.Database.Repositories;

public class PetitionRepository(
    DbContext dbContext,
    ILogger<PetitionRepository> logger)
{
    private readonly DbContext _dbContext = dbContext;
    private readonly ILogger<PetitionRepository> _logger = logger;

    public async Task<(PetitionErrorCode ErrorCode, string PetitionSeq)> CreatePetitionAsync(
        int worldId,
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
                petition.User.AccountName,
                petition.User.AccountUid,
                petition.User.CharName,
                petition.User.CharUid,
                petition.Content,
                ForcedGmAccountName = petition.ForcedGm.AccountName,
                ForcedGmAccountUid = petition.ForcedGm.AccountUid,
                ForcedGmCharName = petition.ForcedGm.CharName,
                ForcedGmCharUid = petition.ForcedGm.CharUid,
                QuotaAtSubmit = (byte)petition.QuotaAtSubmit,
                Time = petition.SubmitTime
            };

            // Insert main petition data
            await _dbContext.ExecuteStoredProcAsync(
                "up_Server_InsertPetition",
                parameters,
                cancellationToken);

            // Insert Lineage2Info
            await InsertLineageInfoAsync(petition.PetitionSeq, petition.Info, cancellationToken);

            scope.Complete();
            return (PetitionErrorCode.Success, petition.PetitionSeq);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create petition for user {UserCharName}",
                petition.User.CharName);
            return (PetitionErrorCode.DatabaseFail, string.Empty);
        }
    }

    public async Task<Petition?> GetPetitionByIdAsync(
        int petitionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new { PetitionId = petitionId };

            return await _dbContext.ExecuteStoredProcAsync<Petition?>( // Added generic type
                "up_Server_GetPetition",
                parameters,
                async (reader, token) =>
                {
                    if (!await reader.ReadAsync(token))
                        return null;

                    return new Petition
                    {
                        PetitionId = petitionId,
                        PetitionSeq = reader.GetString(0),
                        WorldId = reader.GetInt32(1),
                        Category = reader.GetInt32(2),
                        State = (State)reader.GetByte(3),
                        Grade = (Grade)reader.GetByte(4),
                        Flag = reader.GetByte(5),
                        Content = reader.GetString(6),
                        SubmitTime = reader.GetDateTime(7),
                        QuotaAtSubmit = reader.GetInt32(8)
                    };
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get petition {PetitionId}", petitionId);
            return null;
        }
    }

    private async Task<GameCharacter> LoadGameCharacterAsync(
        string petitionSeq,
        string characterType,
        CancellationToken cancellationToken)
    {
        var parameters = new
        {
            PetitionSeq = petitionSeq,
            CharacterType = characterType
        };

        return await _dbContext.ExecuteStoredProcAsync<GameCharacter>( // Added generic type
            "up_Server_GetPetitionCharacter",
            parameters,
            async (reader, token) =>
            {
                if (!await reader.ReadAsync(cancellationToken))
                    return new GameCharacter();

                return new GameCharacter
                {
                    AccountName = reader.GetString(0),
                    AccountUid = reader.GetInt32(1),
                    CharName = reader.GetString(2),
                    CharUid = reader.GetInt32(3),
                    WorldId = reader.GetInt32(4)
                };
            },
            cancellationToken);
    }

    private async Task InsertLineageInfoAsync(
        string petitionSeq,
        Lineage2Info info,
        CancellationToken cancellationToken)
    {
        var parameters = new
        {
            PetitionSeq = petitionSeq,
            info.Race,
            info.Class,
            info.Level,
            info.Disposition,
            info.SsPosition,
            info.NewChar,
            info.Coordinate
        };

        await _dbContext.ExecuteStoredProcAsync(
            "up_Server_AddL2Info",
            parameters,
            cancellationToken);
    }

    private async Task<List<PetitionHistory>> LoadPetitionHistoryAsync(
        string petitionSeq,
        CancellationToken cancellationToken)
    {
        var parameters = new { Seq = petitionSeq };

        return await _dbContext.ExecuteStoredProcAsync<List<PetitionHistory>>( // Added generic type
            "up_Server_GetHistoryList",
            parameters,
            async (reader, token) =>
            {
                var history = new List<PetitionHistory>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    history.Add(new PetitionHistory
                    {
                        Time = reader.GetDateTime(0),
                        Actor = reader.GetString(1),
                        ActionCode = (State)reader.GetByte(2)
                    });
                }
                return history;
            },
            cancellationToken);
    }

    private async Task<List<PetitionMemo>> LoadPetitionMemosAsync(
        string petitionSeq,
        CancellationToken cancellationToken)
    {
        var parameters = new { Seq = petitionSeq };

        return await _dbContext.ExecuteStoredProcAsync<List<PetitionMemo>>(
            "up_Server_GetMemoList",
            parameters,
            async (reader, token) =>
            {
                var memos = new List<PetitionMemo>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    memos.Add(new PetitionMemo
                    {
                        Time = reader.GetDateTime(0),
                        Writer = reader.GetString(1),
                        Content = reader.GetString(2)
                    });
                }
                return memos;
            },
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
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var parameters = new
            {
                Seq = petitionSeq,
                HistorySeq = 0, // Will be calculated by SP
                actor.AccountName,
                actor.AccountUid,
                actor.CharName,
                actor.CharUid,
                State = (byte)newState,
                Message = message ?? string.Empty,
                Flag = flag ?? 0,
                Time = DateTime.UtcNow
            };

            var spName = DetermineStateUpdateStoredProc(newState);
            await _dbContext.ExecuteStoredProcAsync(spName, parameters, cancellationToken);

            scope.Complete();
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

    public async Task<PetitionErrorCode> AddMemoAsync(
        string petitionSeq,
        string content,
        GameCharacter gm,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new
            {
                Seq = petitionSeq,
                MemoSeq = 0, // Will be calculated by SP
                gm.CharName,
                Content = content,
                Time = DateTime.UtcNow
            };

            await _dbContext.ExecuteStoredProcAsync(
                "up_Server_AddMemo",
                parameters,
                cancellationToken);

            return PetitionErrorCode.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add memo for petition {PetitionSeq}", petitionSeq);
            return PetitionErrorCode.DatabaseFail;
        }
    }

    public async Task<IEnumerable<Petition>> GetActivePetitionsForWorldAsync(
        int worldId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new { WorldId = (byte)worldId };
            var petitions = new List<Petition>();

            await _dbContext.ExecuteStoredProcAsync<object>( // Added generic type
                "up_Server_GetActivePetitionList",
                parameters,
                async (reader, token) =>
                {
                    while (await reader.ReadAsync(token))
                    {
                        var petition = new Petition();
                        await LoadPetitionFromReaderAsync(petition, reader, token);
                        petitions.Add(petition);
                    }
                    return Task.CompletedTask;
                },
                cancellationToken);

            return petitions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active petitions for world {WorldId}", worldId);
            return new List<Petition>();
        }
    }

    private async Task LoadPetitionFromReaderAsync(
        Petition petition,
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        petition.WorldId = reader.GetByte(0);
        petition.PetitionSeq = reader.GetString(1);
        petition.Category = reader.GetByte(2);
        petition.Grade = (Grade)reader.GetByte(3);
        petition.Flag = reader.GetByte(12);
        petition.Content = reader.GetString(13);
        petition.SubmitTime = reader.GetDateTime(14);
        petition.State = (State)reader.GetByte(15);
        petition.QuotaAtSubmit = reader.GetByte(20);

        if (!reader.IsDBNull(21))
            petition.CheckOutTime = reader.GetDateTime(21);

        // Load sub-objects
        petition.User = await LoadGameCharacterAsync(petition.PetitionSeq, "User", cancellationToken);
        petition.ForcedGm = await LoadGameCharacterAsync(petition.PetitionSeq, "ForcedGm", cancellationToken);
        petition.AssignedGm = await LoadGameCharacterAsync(petition.PetitionSeq, "AssignedGm", cancellationToken);
        petition.CheckOutGm = await LoadGameCharacterAsync(petition.PetitionSeq, "CheckOutGm", cancellationToken);

        // Load history and memos
        petition.History = await LoadPetitionHistoryAsync(petition.PetitionSeq, cancellationToken);
        petition.Memos = await LoadPetitionMemosAsync(petition.PetitionSeq, cancellationToken);

        // Load Lineage2Info
        // TODO: Implement L2Info loading when needed
    }


    public async Task<int> GetCurrentQuotaAsync(
        int accountUid,
        CancellationToken cancellationToken)
    {
        var parameters = new { AccountUid = accountUid };

        return await _dbContext.ExecuteStoredProcAsync<int>(
            "up_Server_GetCurrentQuota",
            parameters,
            async (reader, token) =>
            {
                if (await reader.ReadAsync(token))
                {
                    return reader.GetInt32(0);
                }
                return 0;
            },
            cancellationToken);
    }

    public async Task ResetQuotaAsync(
        int accountUid,
        CancellationToken cancellationToken)
    {
        var parameters = new { AccountUid = accountUid };

        await _dbContext.ExecuteStoredProcAsync<object>(
            "up_Server_ResetQuota",
            parameters,
            async (reader, token) =>
            {
                await reader.ReadAsync(token);
                return null;
            },
            cancellationToken);
    }

    public async Task ResetAllQuotasAsync(
        CancellationToken cancellationToken)
    {
        await _dbContext.ExecuteStoredProcAsync<object>(
            "up_Server_ResetAllQuotas",
            new { },
            async (reader, token) =>
            {
                await reader.ReadAsync(token);
                return null;
            },
            cancellationToken);
    }

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