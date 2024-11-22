CREATE PROCEDURE [dbo].[up_Server_GetCurrentQuota]
    @AccountUid INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CurrentQuota INT = 0;
    
    -- Count active petitions
    SELECT @CurrentQuota = COUNT(*)
    FROM Petition
    WHERE AccountUid = @AccountUid
        AND State IN (2, 3, 4); -- Submit, CheckOut, BeginChat states
    
    RETURN @CurrentQuota;
END
GO