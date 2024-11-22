CREATE PROCEDURE [dbo].[up_Server_GetPetitionStats]
    @WorldId TINYINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        p.WorldId,
        p.Category,
        p.Grade,
        p.State,
        COUNT(*) AS PetitionCount,
        AVG(DATEDIFF(MINUTE, p.SubmitTime, 
            CASE 
                WHEN p.State IN (2, 9, 12) -- Submit, MessageCheckIn, ChatCheckIn
                THEN p.LastModifiedTime 
                ELSE GETDATE() 
            END)) AS AvgResolutionTimeMinutes
    FROM Petition p
    WHERE (@WorldId IS NULL OR p.WorldId = @WorldId)
    GROUP BY p.WorldId, p.Category, p.Grade, p.State
    ORDER BY p.WorldId, p.Category, p.Grade, p.State;
END
GO