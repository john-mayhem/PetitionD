CREATE PROCEDURE [dbo].[up_Server_ResetQuota]
    @AccountUid INT = NULL  -- NULL means reset all quotas
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF @AccountUid IS NULL
        BEGIN
            -- Reset all quotas
            UPDATE AccountQuota
            SET CurrentQuota = 0,
                LastUpdateTime = GETDATE();
        END
        ELSE
        BEGIN
            -- Reset specific account quota
            UPDATE AccountQuota
            SET CurrentQuota = 0,
                LastUpdateTime = GETDATE()
            WHERE AccountUid = @AccountUid;
        END
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        THROW;
    END CATCH
END
GO