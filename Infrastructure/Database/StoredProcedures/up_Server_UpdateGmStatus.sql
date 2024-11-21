CREATE PROCEDURE [dbo].[up_Server_UpdateGmStatus]
    @Action TINYINT,
    @WorldId TINYINT,
    @GmCharName NVARCHAR(16)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = 0 -- Clear
    BEGIN
        DELETE FROM GmStatus;
    END
    ELSE IF @Action = 1 -- Add
    BEGIN
        INSERT INTO GmStatus (WorldId, GmCharName, LoginTime)
        VALUES (@WorldId, @GmCharName, GETDATE());
    END
    ELSE IF @Action = 2 -- Remove
    BEGIN
        DELETE FROM GmStatus 
        WHERE WorldId = @WorldId 
        AND GmCharName = @GmCharName;
    END
END