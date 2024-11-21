CREATE PROCEDURE [dbo].[up_Server_AddMemo]
    @Seq NVARCHAR(14),
    @MemoSeq INT,
    @CharName NVARCHAR(16),
    @Content NVARCHAR(1000),
    @Time DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO PetitionMemo (
        PetitionSeq,
        MemoSeq,
        Writer,
        Content,
        WriteTime
    )
    VALUES (
        @Seq,
        @MemoSeq,
        @CharName,
        @Content,
        @Time
    );
END
GO