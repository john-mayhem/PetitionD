CREATE PROCEDURE [dbo].[up_Server_AddChatLog]
    @Seq NVARCHAR(14),
    @Talker NVARCHAR(16),
    @Message NVARCHAR(255),
    @Time DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO PetitionChat (
        PetitionSeq,
        ChatSeq,
        Talker,
        Message,
        ChatTime
    )
    SELECT 
        @Seq,
        ISNULL(MAX(ChatSeq), 0) + 1,
        @Talker,
        @Message,
        @Time
    FROM PetitionChat
    WHERE PetitionSeq = @Seq;
END
GO