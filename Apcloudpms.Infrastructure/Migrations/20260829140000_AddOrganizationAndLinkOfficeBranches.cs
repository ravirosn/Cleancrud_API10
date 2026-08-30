using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationAndLinkOfficeBranches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organization",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organization", x => x.Id);
                });

            migrationBuilder.Sql("""
                SET IDENTITY_INSERT [dbo].[Organization] ON;
                INSERT INTO [dbo].[Organization]
                    ([Id], [Code], [Name], [Address], [IsActive], [CreatedAtUtc])
                VALUES
                    (1, N'DEFAULT', N'Default Organization', N'Not configured', 1, SYSUTCDATETIME());
                SET IDENTITY_INSERT [dbo].[Organization] OFF;
                """);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                schema: "dbo",
                table: "OfficeBranch",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE [dbo].[OfficeBranch] SET [OrganizationId] = 1 WHERE [OrganizationId] IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "OrganizationId",
                schema: "dbo",
                table: "OfficeBranch",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficeBranch_OrganizationId_IsActive",
                schema: "dbo",
                table: "OfficeBranch",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Organization_Code",
                schema: "dbo",
                table: "Organization",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organization_IsActive_Name",
                schema: "dbo",
                table: "Organization",
                columns: new[] { "IsActive", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_OfficeBranch_Organization_OrganizationId",
                schema: "dbo",
                table: "OfficeBranch",
                column: "OrganizationId",
                principalSchema: "dbo",
                principalTable: "Organization",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE [dbo].[SPOfficeBranchGet]
                    @PageNumber int = 1, @PageSize int = 20,
                    @SearchTerm nvarchar(200) = NULL, @IncludeInactive bit = 0
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF @PageNumber < 1 THROW 50010, 'PageNumber must be greater than zero.', 1;
                    IF @PageSize < 1 OR @PageSize > 100 THROW 50011, 'PageSize must be between 1 and 100.', 1;
                    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');

                    SELECT COUNT_BIG(1) AS TotalRecords
                    FROM dbo.OfficeBranch b
                    INNER JOIN dbo.Organization o ON o.Id = b.OrganizationId
                    WHERE (@IncludeInactive = 1 OR b.IsActive = 1)
                      AND (@SearchTerm IS NULL OR b.Code LIKE N'%' + @SearchTerm + N'%'
                           OR b.Name LIKE N'%' + @SearchTerm + N'%'
                           OR b.Address LIKE N'%' + @SearchTerm + N'%'
                           OR o.Name LIKE N'%' + @SearchTerm + N'%');

                    SELECT b.Id, b.OrganizationId, o.Name AS OrganizationName,
                           b.Code, b.Name, b.Address, b.IsHeadOffice, b.IsActive
                    FROM dbo.OfficeBranch b
                    INNER JOIN dbo.Organization o ON o.Id = b.OrganizationId
                    WHERE (@IncludeInactive = 1 OR b.IsActive = 1)
                      AND (@SearchTerm IS NULL OR b.Code LIKE N'%' + @SearchTerm + N'%'
                           OR b.Name LIKE N'%' + @SearchTerm + N'%'
                           OR b.Address LIKE N'%' + @SearchTerm + N'%'
                           OR o.Name LIKE N'%' + @SearchTerm + N'%')
                    ORDER BY b.IsHeadOffice DESC, b.Name, b.Id
                    OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE [dbo].[SPOfficeBranchGetById] @Id int
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT b.Id, b.OrganizationId, o.Name AS OrganizationName,
                           b.Code, b.Name, b.Address, b.IsHeadOffice, b.IsActive
                    FROM dbo.OfficeBranch b
                    INNER JOIN dbo.Organization o ON o.Id = b.OrganizationId
                    WHERE b.Id = @Id;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE [dbo].[SPOfficeBranchIns]
                    @OrganizationId int, @Code nvarchar(20), @Name nvarchar(150),
                    @Address nvarchar(500) = NULL, @IsHeadOffice bit = 0, @IsActive bit = 1
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
                    SET @Code = UPPER(LTRIM(RTRIM(@Code)));
                    SET @Name = LTRIM(RTRIM(@Name));
                    SET @Address = NULLIF(LTRIM(RTRIM(@Address)), N'');
                    IF NOT EXISTS (SELECT 1 FROM dbo.Organization WHERE Id = @OrganizationId)
                        THROW 50040, 'Organization was not found.', 1;
                    IF @IsActive = 1 AND NOT EXISTS
                        (SELECT 1 FROM dbo.Organization WHERE Id = @OrganizationId AND IsActive = 1)
                        THROW 50041, 'An active branch cannot belong to an inactive organization.', 1;
                    IF NULLIF(@Code, N'') IS NULL THROW 50020, 'Code is required.', 1;
                    IF NULLIF(@Name, N'') IS NULL THROW 50021, 'Name is required.', 1;
                    IF @IsHeadOffice = 1 AND @IsActive = 0 THROW 50022, 'The head office must be active.', 1;
                    IF EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE Code = @Code)
                        THROW 50023, 'An office branch with this code already exists.', 1;
                    IF @IsActive = 1 AND @IsHeadOffice = 0
                       AND NOT EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE IsHeadOffice = 1 AND IsActive = 1)
                        THROW 50024, 'The first active office branch must be the head office.', 1;

                    BEGIN TRANSACTION;
                    IF @IsHeadOffice = 1 UPDATE dbo.OfficeBranch SET IsHeadOffice = 0 WHERE IsHeadOffice = 1;
                    INSERT dbo.OfficeBranch(OrganizationId, Code, Name, Address, IsHeadOffice, IsActive, CreatedAtUtc)
                    VALUES(@OrganizationId, @Code, @Name, @Address, @IsHeadOffice, @IsActive, SYSUTCDATETIME());
                    DECLARE @Id int = SCOPE_IDENTITY();
                    COMMIT TRANSACTION;
                    EXEC dbo.SPOfficeBranchGetById @Id;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE [dbo].[SPOfficeBranchUpd]
                    @Id int, @OrganizationId int, @Code nvarchar(20), @Name nvarchar(150),
                    @Address nvarchar(500) = NULL, @IsHeadOffice bit = 0, @IsActive bit = 1
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
                    IF NOT EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE Id = @Id) RETURN;
                    SET @Code = UPPER(LTRIM(RTRIM(@Code)));
                    SET @Name = LTRIM(RTRIM(@Name));
                    SET @Address = NULLIF(LTRIM(RTRIM(@Address)), N'');
                    IF NOT EXISTS (SELECT 1 FROM dbo.Organization WHERE Id = @OrganizationId)
                        THROW 50040, 'Organization was not found.', 1;
                    IF @IsActive = 1 AND NOT EXISTS
                        (SELECT 1 FROM dbo.Organization WHERE Id = @OrganizationId AND IsActive = 1)
                        THROW 50041, 'An active branch cannot belong to an inactive organization.', 1;
                    IF NULLIF(@Code, N'') IS NULL THROW 50020, 'Code is required.', 1;
                    IF NULLIF(@Name, N'') IS NULL THROW 50021, 'Name is required.', 1;
                    IF @IsHeadOffice = 1 AND @IsActive = 0 THROW 50022, 'The head office must be active.', 1;
                    IF EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE Code = @Code AND Id <> @Id)
                        THROW 50023, 'An office branch with this code already exists.', 1;
                    IF @IsActive = 0 AND EXISTS (SELECT 1 FROM dbo.Department WHERE OfficeBranchId = @Id AND IsActive = 1)
                        THROW 50025, 'Disable the branch''s active departments before disabling the branch.', 1;
                    IF EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE Id = @Id AND IsHeadOffice = 1 AND IsActive = 1)
                       AND (@IsHeadOffice = 0 OR @IsActive = 0)
                        THROW 50026, 'Assign another active branch as head office before disabling this one.', 1;

                    BEGIN TRANSACTION;
                    IF @IsHeadOffice = 1 UPDATE dbo.OfficeBranch SET IsHeadOffice = 0 WHERE IsHeadOffice = 1 AND Id <> @Id;
                    UPDATE dbo.OfficeBranch SET OrganizationId = @OrganizationId, Code = @Code,
                        Name = @Name, Address = @Address, IsHeadOffice = @IsHeadOffice, IsActive = @IsActive
                    WHERE Id = @Id;
                    COMMIT TRANSACTION;
                    EXEC dbo.SPOfficeBranchGetById @Id;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE [dbo].[SPOfficeBranchGet]
                    @PageNumber int = 1, @PageSize int = 20,
                    @SearchTerm nvarchar(200) = NULL, @IncludeInactive bit = 0
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF @PageNumber < 1 THROW 50010, 'PageNumber must be greater than zero.', 1;
                    IF @PageSize < 1 OR @PageSize > 100 THROW 50011, 'PageSize must be between 1 and 100.', 1;
                    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
                    SELECT COUNT_BIG(1) AS TotalRecords
                    FROM dbo.OfficeBranch b
                    WHERE (@IncludeInactive = 1 OR b.IsActive = 1)
                      AND (@SearchTerm IS NULL OR b.Code LIKE N'%' + @SearchTerm + N'%'
                           OR b.Name LIKE N'%' + @SearchTerm + N'%'
                           OR b.Address LIKE N'%' + @SearchTerm + N'%');
                    SELECT b.Id, b.Code, b.Name, b.Address, b.IsHeadOffice, b.IsActive
                    FROM dbo.OfficeBranch b
                    WHERE (@IncludeInactive = 1 OR b.IsActive = 1)
                      AND (@SearchTerm IS NULL OR b.Code LIKE N'%' + @SearchTerm + N'%'
                           OR b.Name LIKE N'%' + @SearchTerm + N'%'
                           OR b.Address LIKE N'%' + @SearchTerm + N'%')
                    ORDER BY b.IsHeadOffice DESC, b.Name, b.Id
                    OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE [dbo].[SPOfficeBranchGetById] @Id int
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT Id, Code, Name, Address, IsHeadOffice, IsActive
                    FROM dbo.OfficeBranch WHERE Id = @Id;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE [dbo].[SPOfficeBranchIns]
                    @Code nvarchar(20), @Name nvarchar(150), @Address nvarchar(500) = NULL,
                    @IsHeadOffice bit = 0, @IsActive bit = 1
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
                    SET @Code = UPPER(LTRIM(RTRIM(@Code)));
                    SET @Name = LTRIM(RTRIM(@Name));
                    SET @Address = NULLIF(LTRIM(RTRIM(@Address)), N'');
                    IF NULLIF(@Code, N'') IS NULL THROW 50020, 'Code is required.', 1;
                    IF NULLIF(@Name, N'') IS NULL THROW 50021, 'Name is required.', 1;
                    IF @IsHeadOffice = 1 AND @IsActive = 0 THROW 50022, 'The head office must be active.', 1;
                    IF EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE Code = @Code)
                        THROW 50023, 'An office branch with this code already exists.', 1;
                    IF @IsActive = 1 AND @IsHeadOffice = 0
                       AND NOT EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE IsHeadOffice = 1 AND IsActive = 1)
                        THROW 50024, 'The first active office branch must be the head office.', 1;
                    BEGIN TRANSACTION;
                    IF @IsHeadOffice = 1 UPDATE dbo.OfficeBranch SET IsHeadOffice = 0 WHERE IsHeadOffice = 1;
                    INSERT dbo.OfficeBranch(Code, Name, Address, IsHeadOffice, IsActive, CreatedAtUtc)
                    VALUES(@Code, @Name, @Address, @IsHeadOffice, @IsActive, SYSUTCDATETIME());
                    DECLARE @Id int = SCOPE_IDENTITY();
                    COMMIT TRANSACTION;
                    EXEC dbo.SPOfficeBranchGetById @Id;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE [dbo].[SPOfficeBranchUpd]
                    @Id int, @Code nvarchar(20), @Name nvarchar(150), @Address nvarchar(500) = NULL,
                    @IsHeadOffice bit = 0, @IsActive bit = 1
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
                    IF NOT EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE Id = @Id) RETURN;
                    SET @Code = UPPER(LTRIM(RTRIM(@Code)));
                    SET @Name = LTRIM(RTRIM(@Name));
                    SET @Address = NULLIF(LTRIM(RTRIM(@Address)), N'');
                    IF NULLIF(@Code, N'') IS NULL THROW 50020, 'Code is required.', 1;
                    IF NULLIF(@Name, N'') IS NULL THROW 50021, 'Name is required.', 1;
                    IF @IsHeadOffice = 1 AND @IsActive = 0 THROW 50022, 'The head office must be active.', 1;
                    IF EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE Code = @Code AND Id <> @Id)
                        THROW 50023, 'An office branch with this code already exists.', 1;
                    IF @IsActive = 0 AND EXISTS (SELECT 1 FROM dbo.Department WHERE OfficeBranchId = @Id AND IsActive = 1)
                        THROW 50025, 'Disable the branch''s active departments before disabling the branch.', 1;
                    IF EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE Id = @Id AND IsHeadOffice = 1 AND IsActive = 1)
                       AND (@IsHeadOffice = 0 OR @IsActive = 0)
                        THROW 50026, 'Assign another active branch as head office before disabling this one.', 1;
                    BEGIN TRANSACTION;
                    IF @IsHeadOffice = 1 UPDATE dbo.OfficeBranch SET IsHeadOffice = 0 WHERE IsHeadOffice = 1 AND Id <> @Id;
                    UPDATE dbo.OfficeBranch SET Code = @Code, Name = @Name, Address = @Address,
                        IsHeadOffice = @IsHeadOffice, IsActive = @IsActive WHERE Id = @Id;
                    COMMIT TRANSACTION;
                    EXEC dbo.SPOfficeBranchGetById @Id;
                END;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_OfficeBranch_Organization_OrganizationId",
                schema: "dbo",
                table: "OfficeBranch");

            migrationBuilder.DropTable(
                name: "Organization",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_OfficeBranch_OrganizationId_IsActive",
                schema: "dbo",
                table: "OfficeBranch");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "dbo",
                table: "OfficeBranch");
        }
    }
}
