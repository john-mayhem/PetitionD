CREATE PROCEDURE [dbo].[up_Server_InsertPetition]
    @Seq NVARCHAR(14),
    @Category TINYINT,
    @WorldId TINYINT,
    @AccountName NVARCHAR(14),
    @AccountUid INT,
    @CharName NVARCHAR(16),
    @CharUid INT,
    @Content NVARCHAR(255),
    @ForcedGmAccountName NVARCHAR(14),
    @ForcedGmAccountUid INT,
    @ForcedGmCharName NVARCHAR(16),
    @ForcedGmCharUid INT,
    @QuotaAtSubmit TINYINT,
    @Time DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO Petition (
            PetitionSeq, Category, WorldId, 
            AccountName, AccountUid, CharName, CharUid,
            Content, State, SubmitTime, QuotaAtSubmit,
            ForcedGmAccountName, ForcedGmAccountUid, 
            ForcedGmCharName, ForcedGmCharUid
        )
        VALUES (
            @Seq, @Category, @WorldId,
            @AccountName, @AccountUid, @CharName, @CharUid,
            @Content, 2, @Time, @QuotaAtSubmit,
            @ForcedGmAccountName, @ForcedGmAccountUid,
            @ForcedGmCharName, @ForcedGmCharUid
        );

        -- Add initial history entry
        INSERT INTO PetitionHistory (
            PetitionSeq, HistorySeq, Actor, ActionCode, ActionTime
        )
        VALUES (
            @Seq, 1, @CharName, 2, @Time
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO