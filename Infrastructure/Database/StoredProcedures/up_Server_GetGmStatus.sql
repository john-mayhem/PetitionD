CREATE PROCEDURE [dbo].[up_Server_GetGmStatus]
    @WorldId TINYINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        p.WorldId,
        p.CheckOutGmCharName,
        COUNT(*) AS ActivePetitions,
        MIN(p.CheckOutTime) AS OldestCheckOut
    FROM Petition p
    WHERE p.State = 3  -- CheckOut state
        AND (@WorldId IS NULL OR p.WorldId = @WorldId)
        AND p.CheckOutGmCharName IS NOT NULL
    GROUP BY p.WorldId, p.CheckOutGmCharName;
END
GO