CREATE PROCEDURE [dbo].[up_Server_GetL2Info]
    @Seq NVARCHAR(14)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT
        Race,
        Class,
        [Level],
        Disposition,
        SsPosition,
        NewChar,
        Coordinate
    FROM PetitionL2Info
    WHERE PetitionSeq = @Seq;
END
GO