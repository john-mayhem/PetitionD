// File: Infrastructure/Database/DbRepository.cs
namespace PetitionD.Infrastructure.Database;

using Microsoft.Extensions.Logging;
using NC.PetitionLib;
using PetitionD.Core.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Transactions;

public class DbRepository(DbContext dbContext, ILogger<DbRepository> logger) : IDbRepository
{
    private readonly DbContext _dbContext = dbContext;
    private readonly ILogger<DbRepository> _logger = logger;

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
                actor.AccountName,
                actor.AccountUid,
                actor.CharName,
                actor.CharUid,
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

    public async Task<Petition?> GetPetitionByIdAsync(
        int petitionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new { PetitionId = petitionId };

            return await _dbContext.ExecuteStoredProcAsync<Petition?>(
                "up_Server_GetPetition",
                parameters,
                async (reader, token) =>
                {
                    if (!await reader.ReadAsync(token))
                        return null;

                    var petition = new Petition
                    {
                        PetitionId = petitionId,
                        PetitionSeq = reader.GetString(0),
                        WorldId = reader.GetByte(1),
                        Category = reader.GetByte(2),
                        State = (State)reader.GetByte(3),
                        Grade = (Grade)reader.GetByte(4),
                        Flag = reader.GetByte(5),
                        Content = reader.GetString(6),
                        SubmitTime = reader.GetDateTime(7),
                        QuotaAtSubmit = reader.GetByte(8),
                        QuotaAfterTreat = reader.GetByte(9)
                    };

                    if (!reader.IsDBNull(10))
                        petition.CheckOutTime = reader.GetDateTime(10);

                    if (!reader.IsDBNull(11))
                        petition.CheckInTime = reader.GetDateTime(11);

                    // Load associated data
                    await LoadPetitionCharactersAsync(petition, token);
                    await LoadPetitionHistoryAsync(petition, token);
                    await LoadPetitionMemosAsync(petition, token);
                    await LoadLineage2InfoAsync(petition, token);

                    return petition;
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get petition {PetitionId}", petitionId);
            return null;
        }
    }

    private async Task LoadPetitionCharactersAsync(
        Petition petition,
        CancellationToken cancellationToken)
    {
        var parameters = new { Seq = petition.PetitionSeq };

        await _dbContext.ExecuteStoredProcAsync<object>( // Specify object for void operation
            "up_Server_GetPetitionCharacters",
            parameters,
            async (reader, token) =>
            {
                while (await reader.ReadAsync(token))
                {
                    var charType = reader.GetString(0);
                    var character = new GameCharacter
                    {
                        AccountName = reader.GetString(1),
                        AccountUid = reader.GetInt32(2),
                        CharName = reader.GetString(3),
                        CharUid = reader.GetInt32(4),
                        WorldId = petition.WorldId
                    };

                    switch (charType)
                    {
                        case "User": petition.User = character; break;
                        case "ForcedGm": petition.ForcedGm = character; break;
                        case "AssignedGm": petition.AssignedGm = character; break;
                        case "CheckOutGm": petition.CheckOutGm = character; break;
                    }
                }
                // Return Task.CompletedTask to satisfy the lambda expression return type
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    private async Task LoadPetitionHistoryAsync(
        Petition petition,
        CancellationToken cancellationToken)
    {
        var parameters = new { Seq = petition.PetitionSeq };
        petition.History = await _dbContext.ExecuteStoredProcAsync<List<PetitionHistory>>( // Specify List<PetitionHistory>
             "up_Server_GetHistoryList",
             parameters,
             async (reader, token) =>
             {
                 var history = new List<PetitionHistory>();
                 while (await reader.ReadAsync(token))
                 {
                     history.Add(new PetitionHistory
                     {
                         Actor = reader.GetString(0),
                         Time = reader.GetDateTime(1),
                         ActionCode = (State)reader.GetByte(2)
                     });
                 }
                 return history;
             },
             cancellationToken);
    }

    private async Task LoadPetitionMemosAsync(
        Petition petition,
        CancellationToken cancellationToken)
    {
        var parameters = new { Seq = petition.PetitionSeq };

        petition.Memos = await _dbContext.ExecuteStoredProcAsync(
            "up_Server_GetMemoList",
            parameters,
            async (reader, token) =>
            {
                var memos = new List<PetitionMemo>();
                while (await reader.ReadAsync(token))
                {
                    memos.Add(new PetitionMemo
                    {
                        Writer = reader.GetString(0),
                        Time = reader.GetDateTime(1),
                        Content = reader.GetString(2)
                    });
                }
                return memos;
            },
            cancellationToken);
    }

    private async Task LoadLineage2InfoAsync(
        Petition petition,
        CancellationToken cancellationToken)
    {
        var parameters = new { Seq = petition.PetitionSeq };

        petition.Info = await _dbContext.ExecuteStoredProcAsync(
            "up_Server_GetL2Info",
            parameters,
            async (reader, token) =>
            {
                if (!await reader.ReadAsync(token))
                    return new Lineage2Info();

                return new Lineage2Info
                {
                    Race = reader.GetInt32(0),
                    Class = reader.GetInt32(1),
                    Level = reader.GetInt32(2),
                    Disposition = reader.GetInt32(3),
                    SsPosition = reader.GetInt32(4),
                    NewChar = reader.GetInt32(5),
                    Coordinate = reader.GetString(6)
                };
            },
            cancellationToken);
    }

    public async Task<IEnumerable<Petition>> GetActivePetitionsForWorldAsync(
    int worldId,
    CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new { WorldId = (byte)worldId };
            var petitions = new List<Petition>();

            await _dbContext.ExecuteStoredProcAsync<object>( // Changed to explicitly specify type
                "up_Server_GetActivePetitionList",
                parameters,
                async (reader, token) =>
                {
                    while (await reader.ReadAsync(token))
                    {
                        var petition = new Petition
                        {
                            WorldId = worldId,
                            PetitionId = reader.GetInt32(0),
                            PetitionSeq = reader.GetString(1),
                            Category = reader.GetByte(2),
                            State = (State)reader.GetByte(3),
                            Grade = (Grade)reader.GetByte(4),
                            Flag = reader.GetByte(5),
                            Content = reader.GetString(6),
                            SubmitTime = reader.GetDateTime(7),
                            QuotaAtSubmit = reader.GetByte(8)
                        };

                        // Load associated data
                        await LoadPetitionCharactersAsync(petition, token);
                        await LoadPetitionHistoryAsync(petition, token);
                        await LoadPetitionMemosAsync(petition, token);
                        await LoadLineage2InfoAsync(petition, token);

                        petitions.Add(petition);
                    }
                    // Return Task.CompletedTask to satisfy the lambda expression return type
                    return Task.CompletedTask;
                },
                cancellationToken);

            return petitions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active petitions for world {WorldId}", worldId);
            return [];
        }
    }

    public async Task<IEnumerable<Template>> GetTemplatesForGmAsync(
        int gmAccountUid,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new { GmAccountUid = gmAccountUid };

            return await _dbContext.ExecuteStoredProcAsync<List<Template>>( // Specify List<Template>
                "up_Server_GetTemplateList",
                parameters,
                async (reader, token) =>
                {
                    var templates = new List<Template>();
                    while (await reader.ReadAsync(token))
                    {
                        templates.Add(new Template
                        {
                            Code = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Type = (Template.TemplateType)reader.GetByte(2),
                            Content = reader.GetString(3),
                            Category = reader.GetInt32(4),
                            SortOrder = reader.GetInt32(5),
                            AccountUid = gmAccountUid
                        });
                    }
                    return templates;
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get templates for GM {GmAccountUid}", gmAccountUid);
            return [];
        }
    }

    public async Task<PetitionErrorCode> UpdateTemplateAsync(
        Template template,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new
            {
                template.Code,
                template.Name,
                Type = (byte)template.Type,
                template.Content,
                template.Category,
                template.SortOrder,
                template.AccountUid
            };

            await _dbContext.ExecuteStoredProcAsync<int>( // Most SP executions return int for affected rows
                "up_Server_UpdateTemplate",
                parameters,
                async (reader, token) => {
                    await reader.ReadAsync(token);
                    return reader.GetInt32(0);
                },
                cancellationToken);

            return PetitionErrorCode.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template {Code}", template.Code);
            return PetitionErrorCode.DatabaseFail;
        }
    }

    public async Task<PetitionErrorCode> DeleteTemplateAsync(
        int templateCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new { Code = templateCode };

            await _dbContext.ExecuteStoredProcAsync(
                "up_Server_DeleteTemplate",
                parameters,
                cancellationToken);

            return PetitionErrorCode.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete template {Code}", templateCode);
            return PetitionErrorCode.DatabaseFail;
        }
    }

}