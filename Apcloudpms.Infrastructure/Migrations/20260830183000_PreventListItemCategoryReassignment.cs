using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260830183000_PreventListItemCategoryReassignment")]
public sealed class PreventListItemCategoryReassignment : Migration
{
    private const string ValidationToken = "/*CATEGORY_REASSIGNMENT_VALIDATION*/";

    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(ProcedureTemplate.Replace(
            ValidationToken,
            "IF @ExistingCategoryId<>@ListItemCategoryId THROW 50049,'The category of an existing list item cannot be changed.',1;",
            StringComparison.Ordinal));

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(ProcedureTemplate.Replace(
            ValidationToken, string.Empty, StringComparison.Ordinal));

    private const string ProcedureTemplate = """
        CREATE OR ALTER PROCEDURE [dbo].[SpListItemsEdit]
            @Id int,@ListItemCategoryId int,@Code nvarchar(50),@Name nvarchar(100),@Description nvarchar(500)=NULL,@DisplayOrder int=0,@IsActive bit=1,
            @ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
        AS
        BEGIN
            SET NOCOUNT ON; SET XACT_ABORT ON; SET @Code=UPPER(LTRIM(RTRIM(@Code))); SET @Name=LTRIM(RTRIM(@Name));
            IF LEN(@Code)<2 THROW 50044,'Item code must contain at least two characters.',1;
            IF LEN(@Name)<2 THROW 50045,'Item name must contain at least two characters.',1;
            IF @DisplayOrder<0 THROW 50046,'Display order cannot be negative.',1;
            DECLARE @Now datetime2(0)=SYSUTCDATETIME(); BEGIN TRANSACTION;
            DECLARE @ExistingCategoryId int;
            SELECT @ExistingCategoryId=[ListItemCategoryId] FROM [dbo].[ListItem] WITH(UPDLOCK,HOLDLOCK) WHERE [ListItemId]=@Id;
            IF @ExistingCategoryId IS NULL THROW 50004,'List item was not found.',1;
            /*CATEGORY_REASSIGNMENT_VALIDATION*/
            IF NOT EXISTS(SELECT 1 FROM [dbo].[ListItemCategory] WHERE [ListItemCategoryId]=@ListItemCategoryId AND (@IsActive=0 OR [IsActive]=1))
                THROW 50047,'The selected category does not exist or is inactive.',1;
            IF EXISTS(SELECT 1 FROM [dbo].[ListItem] WHERE [ListItemId]<>@Id AND [ListItemCategoryId]=@ListItemCategoryId AND [SystemName]=@Code)
                THROW 50048,'An item with this code already exists in the category.',1;
            DECLARE @Old nvarchar(max)=(SELECT [ListItemCategoryId],[SystemName] [Code],[ItemName] [Name],[Description],[DisplayOrder],[IsVisible] [IsActive],[UpdatedAtUtc]
                FROM [dbo].[ListItem] WHERE [ListItemId]=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
            UPDATE [dbo].[ListItem] SET [ListItemCategoryId]=@ListItemCategoryId,[SystemName]=@Code,[ItemName]=@Name,
                [Description]=NULLIF(LTRIM(RTRIM(@Description)),N''),[DisplayOrder]=@DisplayOrder,[IsVisible]=@IsActive,[UpdatedAtUtc]=@Now WHERE [ListItemId]=@Id;
            DECLARE @New nvarchar(max)=(SELECT [ListItemCategoryId],[SystemName] [Code],[ItemName] [Name],[Description],[DisplayOrder],[IsVisible] [IsActive],[UpdatedAtUtc]
                FROM [dbo].[ListItem] WHERE [ListItemId]=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
            INSERT [dbo].[AuditLog]([EntityName],[Action],[EntityKey],[ChangedColumns],[OldValues],[NewValues],[ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
                VALUES(N'ListItem',N'Modified',CONCAT(N'{"ListItemId":',@Id,N'}'),N'["ListItemCategoryId","Code","Name","Description","DisplayOrder","IsActive","UpdatedAtUtc"]',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
            COMMIT;
            SELECT i.[ListItemId] [Id],i.[ListItemCategoryId],c.[CategoryName],i.[SystemName] [Code],i.[ItemName] [Name],i.[Description],i.[DisplayOrder],i.[IsVisible] [IsActive],i.[CreatedAtUtc],i.[UpdatedAtUtc]
                FROM [dbo].[ListItem] i INNER JOIN [dbo].[ListItemCategory] c ON c.[ListItemCategoryId]=i.[ListItemCategoryId] WHERE i.[ListItemId]=@Id;
        END;
        """;
}
