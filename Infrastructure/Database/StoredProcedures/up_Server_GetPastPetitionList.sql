CREATE PROCEDURE [dbo].[up_Server_GetPastPetitionList]
    @Seq NVARCHAR(14),
    @Offset INT,
    @Size INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT
        p.WorldId,
        p.PetitionSeq,
        p.Category,
        p.Grade,
        -- User info
        p.AccountName,
        p.AccountUid,
        p.CharName,
        p.CharUid,
        -- GM info
        p.CheckOutGmAccountName,
        p.CheckOutGmAccountUid,
        p.CheckOutGmCharName,
        p.CheckOutGmCharUid,
        -- Status
        p.Flag,
        p.Content,
        p.SubmitTime,
        p.State,
        -- Forced GM
        p.ForcedGmAccountName,
        p.ForcedGmAccountUid,
        p.ForcedGmCharName,
        p.ForcedGmCharUid,
        p.QuotaAtSubmit,
        p.CheckOutTime
    FROM Petition p
    WHERE p.PetitionSeq < @Seq
    AND p.State IN (9, 12, 13) -- Completed states
    ORDER BY p.PetitionSeq DESC
    OFFSET @Offset ROWS
    FETCH NEXT @Size ROWS ONLY;
END
GO