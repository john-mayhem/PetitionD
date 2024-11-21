CREATE PROCEDURE [dbo].[up_Server_GetActivePetitionList]
    @WorldId TINYINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        p.PetitionId,
        p.PetitionSeq,
        p.WorldId,
        p.Category,
        p.State,
        p.Grade,
        p.Flag,
        p.Content,
        p.SubmitTime,
        p.QuotaAtSubmit,
        p.CheckOutTime,
        -- User info
        p.AccountName,
        p.AccountUid,
        p.CharName,
        p.CharUid,
        -- Forced GM info
        p.ForcedGmAccountName,
        p.ForcedGmAccountUid,
        p.ForcedGmCharName,
        p.ForcedGmCharUid,
        -- Assigned GM info
        p.AssignedGmAccountName,
        p.AssignedGmAccountUid,
        p.AssignedGmCharName,
        p.AssignedGmCharUid,
        -- Checkout GM info
        p.CheckOutGmAccountName,
        p.CheckOutGmAccountUid,
        p.CheckOutGmCharName,
        p.CheckOutGmCharUid
    FROM Petition p
    WHERE p.WorldId = @WorldId
    AND p.State IN (2, 3, 4, 7, 10, 11) -- Active states
    ORDER BY p.SubmitTime DESC;
END
GO