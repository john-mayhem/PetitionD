CREATE PROCEDURE [dbo].[up_Server_UpdateTemplate]
    @GmAccountUid INT,
    @TemplateId INT,
    @Name NVARCHAR(20),
    @Type TINYINT,
    @Content NVARCHAR(500),
    @Category TINYINT,
    @SortOrder INT,
    @IsPublic BIT,
    @ModifiedBy NVARCHAR(14)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF @TemplateId = 0
        BEGIN
            -- Insert new template
            INSERT INTO Template (
                Name, [Type], Content, Category, SortOrder, 
                OwnerAccountUid, IsPublic, CreatedBy, CreatedTime, 
                ModifiedBy, ModifiedTime
            )
            VALUES (
                @Name, @Type, @Content, @Category, @SortOrder,
                @GmAccountUid, @IsPublic, @ModifiedBy, GETDATE(),
                @ModifiedBy, GETDATE()
            );
            
            SET @TemplateId = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            -- Update existing template
            UPDATE Template
            SET Name = @Name,
                [Type] = @Type,
                Content = @Content,
                Category = @Category,
                SortOrder = @SortOrder,
                IsPublic = @IsPublic,
                ModifiedBy = @ModifiedBy,
                ModifiedTime = GETDATE()
            WHERE TemplateId = @TemplateId
                AND OwnerAccountUid = @GmAccountUid;
        END
        
        COMMIT TRANSACTION;
        RETURN @TemplateId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        RETURN -1;
    END CATCH
END
GO