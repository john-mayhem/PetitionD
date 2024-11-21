-- File: Infrastructure/Database/StoredProcedures/Monitoring/up_Server_GetPetitionStats.sql
CREATE PROCEDURE [dbo].[up_Server_GetPetitionStats]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        WorldId,
        State,
        COUNT(*) as PetitionCount,
        MIN(SubmitTime) as OldestPetition,
        MAX(SubmitTime) as NewestPetition,
        AVG(DATEDIFF(MINUTE, SubmitTime, 
            CASE 
                WHEN State IN (9, 12, 13) THEN LastModifiedTime 
                ELSE GETDATE() 
            END)) as AvgResolutionTimeMinutes
    FROM Petition
    GROUP BY WorldId, State;
END