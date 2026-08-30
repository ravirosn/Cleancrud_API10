using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260830180000_AddListItemManagementProcedures")]
public sealed class AddListItemManagementProcedures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(CategoriesGet);
        migrationBuilder.Sql(CategoriesDdl);
        migrationBuilder.Sql(CategoriesAdd);
        migrationBuilder.Sql(CategoriesEdit);
        migrationBuilder.Sql(CategoriesDelete);
        migrationBuilder.Sql(ItemsGet);
        migrationBuilder.Sql(ItemsAdd);
        migrationBuilder.Sql(ItemsEdit);
        migrationBuilder.Sql(ItemsDelete);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpListItemsDelete];");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpListItemsEdit];");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpListItemsAdd];");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpListItemsGet];");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpListItemCategoriesDelete];");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpListItemCategoriesEdit];");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpListItemCategoriesAdd];");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpListItemCategoriesDdl];");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpListItemCategoriesGet];");
    }

    private const string CategoriesGet = """
        CREATE OR ALTER PROCEDURE [dbo].[SpListItemCategoriesGet]
            @PageNumber int = 1, @PageSize int = 10, @SearchTerm nvarchar(200) = NULL,
            @SortBy nvarchar(30) = N'createdAtUtc', @SortDirection varchar(4) = 'desc',
            @IncludeInactive bit = 0
        AS
        BEGIN
            SET NOCOUNT ON;
            IF @PageNumber < 1 THROW 50010, 'PageNumber must be greater than zero.', 1;
            IF @PageSize < 1 OR @PageSize > 100 THROW 50011, 'PageSize must be between 1 and 100.', 1;
            DECLARE @OrderColumn nvarchar(150) = CASE LOWER(@SortBy)
                WHEN N'code' THEN N'c.[Code]' WHEN N'name' THEN N'c.[CategoryName]'
                WHEN N'itemcount' THEN N'(SELECT COUNT(1) FROM [dbo].[ListItem] x WHERE x.[ListItemCategoryId] = c.[ListItemCategoryId])'
                WHEN N'status' THEN N'c.[IsActive]' WHEN N'updatedatutc' THEN N'c.[UpdatedAtUtc]'
                ELSE N'c.[CreatedAtUtc]' END;
            SET @SortDirection = CASE WHEN LOWER(@SortDirection) = 'asc' THEN 'ASC' ELSE 'DESC' END;
            SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
            DECLARE @Pattern nvarchar(402) = NULL;
            IF @SearchTerm IS NOT NULL SET @Pattern = N'%' + REPLACE(REPLACE(REPLACE(REPLACE(
                @SearchTerm, N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[') + N'%';
            SELECT COUNT_BIG(1) AS [TotalRecords] FROM [dbo].[ListItemCategory] c
            WHERE (@IncludeInactive = 1 OR c.[IsActive] = 1) AND (@Pattern IS NULL
                OR c.[Code] LIKE @Pattern ESCAPE N'\' OR c.[CategoryName] LIKE @Pattern ESCAPE N'\'
                OR c.[Description] LIKE @Pattern ESCAPE N'\');
            DECLARE @Sql nvarchar(max) = N'SELECT c.[ListItemCategoryId] AS [Id], c.[Code],
                c.[CategoryName] AS [Name], c.[Description], c.[IsActive],
                CONVERT(int, (SELECT COUNT(1) FROM [dbo].[ListItem] x WHERE x.[ListItemCategoryId] = c.[ListItemCategoryId])) AS [ItemCount],
                c.[CreatedAtUtc], c.[UpdatedAtUtc] FROM [dbo].[ListItemCategory] c
                WHERE (@active = 1 OR c.[IsActive] = 1) AND (@search IS NULL
                    OR c.[Code] LIKE @search ESCAPE N''\'' OR c.[CategoryName] LIKE @search ESCAPE N''\''
                    OR c.[Description] LIKE @search ESCAPE N''\'') ORDER BY ' + @OrderColumn + N' ' + @SortDirection +
                N', c.[ListItemCategoryId] DESC OFFSET (@page - 1) * @size ROWS FETCH NEXT @size ROWS ONLY;';
            EXEC sys.sp_executesql @Sql, N'@active bit,@search nvarchar(402),@page int,@size int',
                @IncludeInactive, @Pattern, @PageNumber, @PageSize;
        END;
        """;

    private const string CategoriesDdl = """
        CREATE OR ALTER PROCEDURE [dbo].[SpListItemCategoriesDdl]
        AS
        BEGIN
            SET NOCOUNT ON;
            SELECT [ListItemCategoryId] AS [Id], [Code], [CategoryName] AS [Name]
            FROM [dbo].[ListItemCategory] WHERE [IsActive] = 1 ORDER BY [CategoryName], [ListItemCategoryId];
        END;
        """;

    private const string CategoriesAdd = """
        CREATE OR ALTER PROCEDURE [dbo].[SpListItemCategoriesAdd]
            @Code nvarchar(50), @Name nvarchar(100), @Description nvarchar(500) = NULL, @IsActive bit = 1,
            @ActorUserId int = NULL, @ActorName nvarchar(256) = NULL, @TraceId nvarchar(100) = NULL, @IpAddress nvarchar(45) = NULL
        AS
        BEGIN
            SET NOCOUNT ON; SET XACT_ABORT ON;
            SET @Code = UPPER(LTRIM(RTRIM(@Code))); SET @Name = LTRIM(RTRIM(@Name));
            IF LEN(@Code) < 2 THROW 50040, 'Category code must contain at least two characters.', 1;
            IF LEN(@Name) < 2 THROW 50041, 'Category name must contain at least two characters.', 1;
            DECLARE @Now datetime2(0) = SYSUTCDATETIME(); BEGIN TRANSACTION;
            IF EXISTS (SELECT 1 FROM [dbo].[ListItemCategory] WITH (UPDLOCK, HOLDLOCK) WHERE [Code] = @Code)
                THROW 50043, 'A category with this code already exists.', 1;
            INSERT [dbo].[ListItemCategory] ([Code],[CategoryName],[Description],[IsActive],[CreatedAtUtc])
                VALUES (@Code,@Name,NULLIF(LTRIM(RTRIM(@Description)),N''),@IsActive,@Now);
            DECLARE @Id int = CONVERT(int,SCOPE_IDENTITY());
            DECLARE @New nvarchar(max) = (SELECT @Code [Code],@Name [Name],@Description [Description],@IsActive [IsActive],@Now [CreatedAtUtc]
                FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
            INSERT [dbo].[AuditLog] ([EntityName],[Action],[EntityKey],[ChangedColumns],[NewValues],[ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
                VALUES (N'ListItemCategory',N'Added',CONCAT(N'{"ListItemCategoryId":',@Id,N'}'),N'["Code","Name","Description","IsActive","CreatedAtUtc"]',
                    @New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
            COMMIT;
            SELECT [ListItemCategoryId] [Id],[Code],[CategoryName] [Name],[Description],[IsActive],[CreatedAtUtc],[UpdatedAtUtc]
                FROM [dbo].[ListItemCategory] WHERE [ListItemCategoryId] = @Id;
        END;
        """;

    private const string CategoriesEdit = """
        CREATE OR ALTER PROCEDURE [dbo].[SpListItemCategoriesEdit]
            @Id int, @Code nvarchar(50), @Name nvarchar(100), @Description nvarchar(500) = NULL, @IsActive bit = 1,
            @ActorUserId int = NULL, @ActorName nvarchar(256) = NULL, @TraceId nvarchar(100) = NULL, @IpAddress nvarchar(45) = NULL
        AS
        BEGIN
            SET NOCOUNT ON; SET XACT_ABORT ON;
            SET @Code = UPPER(LTRIM(RTRIM(@Code))); SET @Name = LTRIM(RTRIM(@Name));
            IF LEN(@Code) < 2 THROW 50040, 'Category code must contain at least two characters.', 1;
            IF LEN(@Name) < 2 THROW 50041, 'Category name must contain at least two characters.', 1;
            DECLARE @Now datetime2(0) = SYSUTCDATETIME(); BEGIN TRANSACTION;
            IF NOT EXISTS (SELECT 1 FROM [dbo].[ListItemCategory] WITH (UPDLOCK,HOLDLOCK) WHERE [ListItemCategoryId] = @Id)
                THROW 50004, 'List-item category was not found.', 1;
            IF EXISTS (SELECT 1 FROM [dbo].[ListItemCategory] WHERE [ListItemCategoryId] <> @Id AND [Code] = @Code)
                THROW 50043, 'A category with this code already exists.', 1;
            IF @IsActive = 0 AND EXISTS (SELECT 1 FROM [dbo].[ListItem] WHERE [ListItemCategoryId] = @Id AND [IsVisible] = 1)
                THROW 50042, 'Deactivate the category''s active list items before deactivating the category.', 1;
            DECLARE @Old nvarchar(max) = (SELECT [Code],[CategoryName] [Name],[Description],[IsActive],[UpdatedAtUtc]
                FROM [dbo].[ListItemCategory] WHERE [ListItemCategoryId] = @Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
            UPDATE [dbo].[ListItemCategory] SET [Code]=@Code,[CategoryName]=@Name,
                [Description]=NULLIF(LTRIM(RTRIM(@Description)),N''),[IsActive]=@IsActive,[UpdatedAtUtc]=@Now
                WHERE [ListItemCategoryId]=@Id;
            DECLARE @New nvarchar(max) = (SELECT [Code],[CategoryName] [Name],[Description],[IsActive],[UpdatedAtUtc]
                FROM [dbo].[ListItemCategory] WHERE [ListItemCategoryId] = @Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
            INSERT [dbo].[AuditLog] ([EntityName],[Action],[EntityKey],[ChangedColumns],[OldValues],[NewValues],[ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
                VALUES (N'ListItemCategory',N'Modified',CONCAT(N'{"ListItemCategoryId":',@Id,N'}'),N'["Code","Name","Description","IsActive","UpdatedAtUtc"]',
                    @Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
            COMMIT;
            SELECT [ListItemCategoryId] [Id],[Code],[CategoryName] [Name],[Description],[IsActive],[CreatedAtUtc],[UpdatedAtUtc]
                FROM [dbo].[ListItemCategory] WHERE [ListItemCategoryId] = @Id;
        END;
        """;

    private const string CategoriesDelete = """
        CREATE OR ALTER PROCEDURE [dbo].[SpListItemCategoriesDelete]
            @Id int, @ActorUserId int = NULL, @ActorName nvarchar(256) = NULL, @TraceId nvarchar(100) = NULL, @IpAddress nvarchar(45) = NULL
        AS
        BEGIN
            SET NOCOUNT ON; SET XACT_ABORT ON; DECLARE @Now datetime2(0)=SYSUTCDATETIME(); BEGIN TRANSACTION;
            IF NOT EXISTS (SELECT 1 FROM [dbo].[ListItemCategory] WITH (UPDLOCK,HOLDLOCK) WHERE [ListItemCategoryId]=@Id)
            BEGIN COMMIT; SELECT CAST(0 AS int); RETURN; END;
            IF EXISTS (SELECT 1 FROM [dbo].[ListItem] WHERE [ListItemCategoryId]=@Id AND [IsVisible]=1)
                THROW 50042, 'Deactivate the category''s active list items before deactivating the category.', 1;
            DECLARE @Old nvarchar(max)=(SELECT [IsActive],[UpdatedAtUtc] FROM [dbo].[ListItemCategory] WHERE [ListItemCategoryId]=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
            UPDATE [dbo].[ListItemCategory] SET [IsActive]=0,[UpdatedAtUtc]=@Now WHERE [ListItemCategoryId]=@Id;
            DECLARE @New nvarchar(max)=(SELECT [IsActive],[UpdatedAtUtc] FROM [dbo].[ListItemCategory] WHERE [ListItemCategoryId]=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
            INSERT [dbo].[AuditLog] ([EntityName],[Action],[EntityKey],[ChangedColumns],[OldValues],[NewValues],[ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
                VALUES(N'ListItemCategory',N'Modified',CONCAT(N'{"ListItemCategoryId":',@Id,N'}'),N'["IsActive","UpdatedAtUtc"]',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
            COMMIT; SELECT CAST(1 AS int);
        END;
        """;

    private const string ItemsGet = """
        CREATE OR ALTER PROCEDURE [dbo].[SpListItemsGet]
            @PageNumber int=1,@PageSize int=10,@SearchTerm nvarchar(200)=NULL,
            @SortBy nvarchar(30)=N'createdAtUtc',@SortDirection varchar(4)='desc',@IncludeInactive bit=0,
            @ListItemCategoryId int=NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            IF @PageNumber < 1 THROW 50010, 'PageNumber must be greater than zero.', 1;
            IF @PageSize < 1 OR @PageSize > 100 THROW 50011, 'PageSize must be between 1 and 100.', 1;
            DECLARE @OrderColumn nvarchar(80)=CASE LOWER(@SortBy) WHEN N'categoryname' THEN N'c.[CategoryName]'
                WHEN N'code' THEN N'i.[SystemName]' WHEN N'name' THEN N'i.[ItemName]'
                WHEN N'displayorder' THEN N'i.[DisplayOrder]' WHEN N'status' THEN N'i.[IsVisible]'
                WHEN N'updatedatutc' THEN N'i.[UpdatedAtUtc]' ELSE N'i.[CreatedAtUtc]' END;
            SET @SortDirection=CASE WHEN LOWER(@SortDirection)='asc' THEN 'ASC' ELSE 'DESC' END;
            SET @SearchTerm=NULLIF(LTRIM(RTRIM(@SearchTerm)),N''); DECLARE @Pattern nvarchar(402)=NULL;
            IF @SearchTerm IS NOT NULL SET @Pattern=N'%'+REPLACE(REPLACE(REPLACE(REPLACE(@SearchTerm,N'\',N'\\'),N'%',N'\%'),N'_',N'\_'),N'[',N'\[')+N'%';
            SELECT COUNT_BIG(1) [TotalRecords] FROM [dbo].[ListItem] i INNER JOIN [dbo].[ListItemCategory] c ON c.[ListItemCategoryId]=i.[ListItemCategoryId]
            WHERE (@IncludeInactive=1 OR (i.[IsVisible]=1 AND c.[IsActive]=1)) AND (@ListItemCategoryId IS NULL OR i.[ListItemCategoryId]=@ListItemCategoryId)
                AND (@Pattern IS NULL OR i.[SystemName] LIKE @Pattern ESCAPE N'\' OR i.[ItemName] LIKE @Pattern ESCAPE N'\'
                    OR i.[Description] LIKE @Pattern ESCAPE N'\' OR c.[CategoryName] LIKE @Pattern ESCAPE N'\');
            DECLARE @Sql nvarchar(max)=N'SELECT i.[ListItemId] [Id],i.[ListItemCategoryId],c.[CategoryName],i.[SystemName] [Code],
                i.[ItemName] [Name],i.[Description],i.[DisplayOrder],i.[IsVisible] [IsActive],i.[CreatedAtUtc],i.[UpdatedAtUtc]
                FROM [dbo].[ListItem] i INNER JOIN [dbo].[ListItemCategory] c ON c.[ListItemCategoryId]=i.[ListItemCategoryId]
                WHERE (@active=1 OR (i.[IsVisible]=1 AND c.[IsActive]=1)) AND (@categoryId IS NULL OR i.[ListItemCategoryId]=@categoryId)
                AND (@search IS NULL OR i.[SystemName] LIKE @search ESCAPE N''\'' OR i.[ItemName] LIKE @search ESCAPE N''\''
                    OR i.[Description] LIKE @search ESCAPE N''\'' OR c.[CategoryName] LIKE @search ESCAPE N''\'')
                ORDER BY '+@OrderColumn+N' '+@SortDirection+N',i.[ListItemId] DESC OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY;';
            EXEC sys.sp_executesql @Sql,N'@active bit,@categoryId int,@search nvarchar(402),@page int,@size int',
                @IncludeInactive,@ListItemCategoryId,@Pattern,@PageNumber,@PageSize;
        END;
        """;

    private const string ItemsAdd = """
        CREATE OR ALTER PROCEDURE [dbo].[SpListItemsAdd]
            @ListItemCategoryId int,@Code nvarchar(50),@Name nvarchar(100),@Description nvarchar(500)=NULL,@DisplayOrder int=0,@IsActive bit=1,
            @ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
        AS
        BEGIN
            SET NOCOUNT ON; SET XACT_ABORT ON; SET @Code=UPPER(LTRIM(RTRIM(@Code))); SET @Name=LTRIM(RTRIM(@Name));
            IF LEN(@Code)<2 THROW 50044,'Item code must contain at least two characters.',1;
            IF LEN(@Name)<2 THROW 50045,'Item name must contain at least two characters.',1;
            IF @DisplayOrder<0 THROW 50046,'Display order cannot be negative.',1;
            DECLARE @Now datetime2(0)=SYSUTCDATETIME(); BEGIN TRANSACTION;
            IF NOT EXISTS(SELECT 1 FROM [dbo].[ListItemCategory] WITH(UPDLOCK,HOLDLOCK) WHERE [ListItemCategoryId]=@ListItemCategoryId AND (@IsActive=0 OR [IsActive]=1))
                THROW 50047,'The selected category does not exist or is inactive.',1;
            IF EXISTS(SELECT 1 FROM [dbo].[ListItem] WITH(UPDLOCK,HOLDLOCK) WHERE [ListItemCategoryId]=@ListItemCategoryId AND [SystemName]=@Code)
                THROW 50048,'An item with this code already exists in the category.',1;
            INSERT [dbo].[ListItem]([ListItemCategoryId],[SystemName],[ItemName],[Description],[DisplayOrder],[IsVisible],[CreatedAtUtc])
                VALUES(@ListItemCategoryId,@Code,@Name,NULLIF(LTRIM(RTRIM(@Description)),N''),@DisplayOrder,@IsActive,@Now);
            DECLARE @Id int=CONVERT(int,SCOPE_IDENTITY()); DECLARE @New nvarchar(max)=(SELECT @ListItemCategoryId [ListItemCategoryId],@Code [Code],@Name [Name],@Description [Description],@DisplayOrder [DisplayOrder],@IsActive [IsActive],@Now [CreatedAtUtc] FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
            INSERT [dbo].[AuditLog]([EntityName],[Action],[EntityKey],[ChangedColumns],[NewValues],[ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
                VALUES(N'ListItem',N'Added',CONCAT(N'{"ListItemId":',@Id,N'}'),N'["ListItemCategoryId","Code","Name","Description","DisplayOrder","IsActive","CreatedAtUtc"]',@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
            COMMIT;
            SELECT i.[ListItemId] [Id],i.[ListItemCategoryId],c.[CategoryName],i.[SystemName] [Code],i.[ItemName] [Name],i.[Description],i.[DisplayOrder],i.[IsVisible] [IsActive],i.[CreatedAtUtc],i.[UpdatedAtUtc]
                FROM [dbo].[ListItem] i INNER JOIN [dbo].[ListItemCategory] c ON c.[ListItemCategoryId]=i.[ListItemCategoryId] WHERE i.[ListItemId]=@Id;
        END;
        """;

    private const string ItemsEdit = """
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
            IF NOT EXISTS(SELECT 1 FROM [dbo].[ListItem] WHERE [ListItemId]=@Id)
                THROW 50004,'List item was not found.',1;
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

    private const string ItemsDelete = """
        CREATE OR ALTER PROCEDURE [dbo].[SpListItemsDelete]
            @Id int,@ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
        AS
        BEGIN
            SET NOCOUNT ON; SET XACT_ABORT ON; DECLARE @Now datetime2(0)=SYSUTCDATETIME(); BEGIN TRANSACTION;
            IF NOT EXISTS(SELECT 1 FROM [dbo].[ListItem] WITH(UPDLOCK,HOLDLOCK) WHERE [ListItemId]=@Id)
            BEGIN COMMIT; SELECT CAST(0 AS int); RETURN; END;
            DECLARE @Old nvarchar(max)=(SELECT [IsVisible] [IsActive],[UpdatedAtUtc] FROM [dbo].[ListItem] WHERE [ListItemId]=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
            UPDATE [dbo].[ListItem] SET [IsVisible]=0,[UpdatedAtUtc]=@Now WHERE [ListItemId]=@Id;
            DECLARE @New nvarchar(max)=(SELECT [IsVisible] [IsActive],[UpdatedAtUtc] FROM [dbo].[ListItem] WHERE [ListItemId]=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
            INSERT [dbo].[AuditLog]([EntityName],[Action],[EntityKey],[ChangedColumns],[OldValues],[NewValues],[ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
                VALUES(N'ListItem',N'Modified',CONCAT(N'{"ListItemId":',@Id,N'}'),N'["IsActive","UpdatedAtUtc"]',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
            COMMIT; SELECT CAST(1 AS int);
        END;
        """;
}
