CREATE PROCEDURE [dbo].[up_Server_GetTemplateList]
    @GmAccountUid INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get personal and public templates
    SELECT 
        t.TemplateId,
        t.Name,
        t.[Type],
        t.Content,
        t.Category,
        t.SortOrder,
        t.IsPublic,
        t.CreatedBy,
        t.CreatedTime,
        t.ModifiedBy,
        t.ModifiedTime
    FROM Template t
    WHERE t.OwnerAccountUid = @GmAccountUid 
        OR t.IsPublic = 1
    ORDER BY t.Category, t.SortOrder, t.Name;
END
GO