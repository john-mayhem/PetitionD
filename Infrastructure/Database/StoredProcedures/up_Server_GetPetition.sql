CREATE PROCEDURE [dbo].[up_Server_GetPetition]
    @PetitionId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
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
    WHERE p.PetitionId = @PetitionId;
END
GO