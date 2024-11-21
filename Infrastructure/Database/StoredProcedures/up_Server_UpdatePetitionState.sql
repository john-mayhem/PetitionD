CREATE PROCEDURE [dbo].[up_Server_UpdatePetitionState]
    @Seq NVARCHAR(14),
    @State TINYINT,
    @AccountName NVARCHAR(14),
    @CharName NVARCHAR(16),
    @Message NVARCHAR(1000) = NULL,
    @Flag TINYINT = NULL,
    @Time DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Update petition state
        UPDATE Petition
        SET 
            State = @State,
            Flag = ISNULL(@Flag, Flag),
            LastModifiedTime = @Time
        WHERE PetitionSeq = @Seq;

        -- Add history entry
        INSERT INTO PetitionHistory (
            PetitionSeq,
            HistorySeq,
            Actor,
            ActionCode,
            ActionTime,
            Message
        )
        SELECT 
            @Seq,
            ISNULL(MAX(HistorySeq), 0) + 1,
            @CharName,
            @State,
            @Time,
            @Message
        FROM PetitionHistory
        WHERE PetitionSeq = @Seq;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END