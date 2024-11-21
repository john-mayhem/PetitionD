CREATE PROCEDURE [dbo].[up_Server_GetHistoryList]
    @Seq NVARCHAR(14)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT
        ActionTime,
        Actor,
        ActionCode
    FROM PetitionHistory
    WHERE PetitionSeq = @Seq
    ORDER BY HistorySeq ASC;
END
GO