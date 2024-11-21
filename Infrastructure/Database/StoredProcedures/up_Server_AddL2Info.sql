CREATE PROCEDURE [dbo].[up_Server_AddL2Info]
    @PetitionSeq NVARCHAR(14),
    @Race INT,
    @Class INT,
    @Level INT,
    @Disposition INT,
    @SsPosition INT,
    @NewChar INT,
    @Coordinate NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO PetitionL2Info (
        PetitionSeq,
        Race,
        Class,
        [Level],
        Disposition,
        SsPosition,
        NewChar,
        Coordinate
    )
    VALUES (
        @PetitionSeq,
        @Race,
        @Class,
        @Level,
        @Disposition,
        @SsPosition,
        @NewChar,
        @Coordinate
    );
END
GO