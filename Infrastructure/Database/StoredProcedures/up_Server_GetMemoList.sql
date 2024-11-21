CREATE PROCEDURE [dbo].[up_Server_GetMemoList]
    @Seq NVARCHAR(14)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT
        WriteTime,
        Writer,
        Content
    FROM PetitionMemo
    WHERE PetitionSeq = @Seq
    ORDER BY MemoSeq ASC;
END
GO