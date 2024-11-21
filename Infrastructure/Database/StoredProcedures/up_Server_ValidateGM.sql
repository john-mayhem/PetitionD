CREATE PROCEDURE [dbo].[up_Server_ValidateGM]
    @Account NVARCHAR(14),
    @Password NVARCHAR(16)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        a.AccountUid,
        g.Grade
    FROM Account a
    INNER JOIN GmAccount g ON a.AccountUid = g.AccountUid
    WHERE a.AccountName = @Account
    AND a.Password = @Password
    AND a.IsActive = 1
    AND g.IsActive = 1;
END