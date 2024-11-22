CREATE PROCEDURE [dbo].[up_Server_UpdateGmStatus]
    @Action TINYINT,  -- 0=Clear, 1=Add, 2=Remove
    @WorldId TINYINT,
    @GmCharName NVARCHAR(16)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF @Action = 0  -- Clear all
        BEGIN
            DELETE FROM GmStatus;
        END
        ELSE IF @Action = 1  -- Add
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM GmStatus 
                WHERE WorldId = @WorldId AND GmCharName = @GmCharName
            )
            BEGIN
                INSERT INTO GmStatus (WorldId, GmCharName, LoginTime)
                VALUES (@WorldId, @GmCharName, GETDATE());
            END
        END
        ELSE IF @Action = 2  -- Remove
        BEGIN
            UPDATE GmStatus
            SET LogoutTime = GETDATE()
            WHERE WorldId = @WorldId 
                AND GmCharName = @GmCharName
                AND LogoutTime IS NULL;
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