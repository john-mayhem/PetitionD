CREATE PROCEDURE [dbo].[up_Server_UpdateQuota]
    @AccountUid INT,
    @Delta INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF NOT EXISTS (SELECT 1 FROM AccountQuota WHERE AccountUid = @AccountUid)
        BEGIN
            INSERT INTO AccountQuota (AccountUid, CurrentQuota, LastUpdateTime)
            VALUES (@AccountUid, @Delta, GETDATE());
        END
        ELSE
        BEGIN
            UPDATE AccountQuota
            SET CurrentQuota = CurrentQuota + @Delta,
                LastUpdateTime = GETDATE()
            WHERE AccountUid = @AccountUid;
        END
        
        COMMIT TRANSACTION;
        RETURN 0; -- Success
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        RETURN -1; -- Error
    END CATCH
END
GO