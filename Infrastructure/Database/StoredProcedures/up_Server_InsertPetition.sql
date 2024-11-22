CREATE PROCEDURE [dbo].[up_Server_InsertPetition]
    @PetitionSeq NVARCHAR(14),
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
    SET XACT_ABORT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        INSERT INTO Petition (
            PetitionSeq, Category, WorldId,
            AccountName, AccountUid, CharName, CharUid,
            Content, 
            ForcedGmAccountName, ForcedGmAccountUid, 
            ForcedGmCharName, ForcedGmCharUid,
            QuotaAtSubmit, SubmitTime,
            State, Grade, Flag
        )
        VALUES (
            @PetitionSeq, @Category, @WorldId,
            @AccountName, @AccountUid, @CharName, @CharUid,
            @Content,
            @ForcedGmAccountName, @ForcedGmAccountUid,
            @ForcedGmCharName, @ForcedGmCharUid,
            @QuotaAtSubmit, @Time,
            2, -- Submit state
            1, -- Default grade
            0  -- Default flag
        );
        
        -- Add initial history entry
        INSERT INTO PetitionHistory (
            PetitionSeq, HistorySeq, Actor, 
            ActionCode, ActionTime
        )
        VALUES (
            @PetitionSeq, 1, @CharName,
            2, -- Submit state
            @Time
        );
        
        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        RETURN -1;
    END CATCH
END
GO