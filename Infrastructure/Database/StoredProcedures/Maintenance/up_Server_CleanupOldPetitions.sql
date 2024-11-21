-- File: Infrastructure/Database/StoredProcedures/Maintenance/up_Server_CleanupOldPetitions.sql
CREATE PROCEDURE [dbo].[up_Server_CleanupOldPetitions]
    @DaysToKeep INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME = DATEADD(DAY, -@DaysToKeep, GETDATE())
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Archive old petitions if needed
        INSERT INTO PetitionArchive (
            -- columns
        )
        SELECT *
        FROM Petition 
        WHERE State IN (9, 12, 13) -- Completed states
            AND LastModifiedTime < @CutoffDate;
            
        -- Delete archived petitions
        DELETE FROM Petition 
        WHERE State IN (9, 12, 13)
            AND LastModifiedTime < @CutoffDate;
            
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO