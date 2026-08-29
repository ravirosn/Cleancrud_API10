using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260828100000_AddOrganizationCrudProcedures")]
public partial class AddOrganizationCrudProcedures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SPOfficeBranchGet]
                @PageNumber int = 1,
                @PageSize int = 20,
                @SearchTerm nvarchar(200) = NULL,
                @IncludeInactive bit = 0
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
            END
            """);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SPOfficeBranchDdl]
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT Id, Code, Name
                FROM dbo.OfficeBranch
                WHERE IsActive = 1
                ORDER BY IsHeadOffice DESC, Name, Id;
            END
            """);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SPOfficeBranchGetById] @Id int
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT Id, Code, Name, Address, IsHeadOffice, IsActive
                FROM dbo.OfficeBranch WHERE Id = @Id;
            END
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
            END
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
            END
            """);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SPOfficeBranchDel] @Id int
            AS
            BEGIN
                SET NOCOUNT ON;
                IF NOT EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE Id = @Id)
                BEGIN SELECT CAST(0 AS bit); RETURN; END
                IF EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE Id = @Id AND IsHeadOffice = 1)
                    THROW 50027, 'The head office cannot be deleted. Assign another head office first.', 1;
                IF EXISTS (SELECT 1 FROM dbo.Department WHERE OfficeBranchId = @Id AND IsActive = 1)
                    THROW 50025, 'Disable the branch''s active departments before deleting the branch.', 1;
                UPDATE dbo.OfficeBranch SET IsActive = 0 WHERE Id = @Id;
                SELECT CAST(1 AS bit);
            END
            """);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SPDepartmentGet]
                @PageNumber int = 1, @PageSize int = 20, @SearchTerm nvarchar(200) = NULL,
                @IncludeInactive bit = 0, @OfficeBranchId int = NULL
            AS
            BEGIN
                SET NOCOUNT ON;
                IF @PageNumber < 1 THROW 50010, 'PageNumber must be greater than zero.', 1;
                IF @PageSize < 1 OR @PageSize > 100 THROW 50011, 'PageSize must be between 1 and 100.', 1;
                SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');

                SELECT COUNT_BIG(1) AS TotalRecords
                FROM dbo.Department d INNER JOIN dbo.OfficeBranch b ON b.Id = d.OfficeBranchId
                WHERE (@IncludeInactive = 1 OR d.IsActive = 1)
                  AND (@OfficeBranchId IS NULL OR d.OfficeBranchId = @OfficeBranchId)
                  AND (@SearchTerm IS NULL OR d.Code LIKE N'%' + @SearchTerm + N'%'
                       OR d.Name LIKE N'%' + @SearchTerm + N'%' OR b.Name LIKE N'%' + @SearchTerm + N'%');

                SELECT d.Id, d.OfficeBranchId, b.Name AS BranchName, d.Code, d.Name, d.IsActive
                FROM dbo.Department d INNER JOIN dbo.OfficeBranch b ON b.Id = d.OfficeBranchId
                WHERE (@IncludeInactive = 1 OR d.IsActive = 1)
                  AND (@OfficeBranchId IS NULL OR d.OfficeBranchId = @OfficeBranchId)
                  AND (@SearchTerm IS NULL OR d.Code LIKE N'%' + @SearchTerm + N'%'
                       OR d.Name LIKE N'%' + @SearchTerm + N'%' OR b.Name LIKE N'%' + @SearchTerm + N'%')
                ORDER BY b.Name, d.Name, d.Id
                OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
                FETCH NEXT @PageSize ROWS ONLY;
            END
            """);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SPDepartmentDdl] @OfficeBranchId int = NULL
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT d.Id, d.Code, d.Name
                FROM dbo.Department d INNER JOIN dbo.OfficeBranch b ON b.Id = d.OfficeBranchId
                WHERE d.IsActive = 1 AND b.IsActive = 1
                  AND (@OfficeBranchId IS NULL OR d.OfficeBranchId = @OfficeBranchId)
                ORDER BY d.Name, d.Id;
            END
            """);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SPDepartmentGetById] @Id int
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT d.Id, d.OfficeBranchId, b.Name AS BranchName, d.Code, d.Name, d.IsActive
                FROM dbo.Department d INNER JOIN dbo.OfficeBranch b ON b.Id = d.OfficeBranchId
                WHERE d.Id = @Id;
            END
            """);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SPDepartmentIns]
                @OfficeBranchId int, @Code nvarchar(20), @Name nvarchar(150), @IsActive bit = 1
            AS
            BEGIN
                SET NOCOUNT ON;
                SET @Code = UPPER(LTRIM(RTRIM(@Code))); SET @Name = LTRIM(RTRIM(@Name));
                IF NULLIF(@Code, N'') IS NULL THROW 50020, 'Code is required.', 1;
                IF NULLIF(@Name, N'') IS NULL THROW 50021, 'Name is required.', 1;
                IF NOT EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE Id = @OfficeBranchId)
                    THROW 50030, 'Office branch was not found.', 1;
                IF @IsActive = 1 AND NOT EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE Id = @OfficeBranchId AND IsActive = 1)
                    THROW 50031, 'An active department cannot belong to an inactive branch.', 1;
                IF EXISTS (SELECT 1 FROM dbo.Department WHERE OfficeBranchId = @OfficeBranchId AND Code = @Code)
                    THROW 50032, 'This department code already exists in the selected branch.', 1;
                INSERT dbo.Department(OfficeBranchId, Code, Name, IsActive, CreatedAtUtc)
                VALUES(@OfficeBranchId, @Code, @Name, @IsActive, SYSUTCDATETIME());
                DECLARE @Id int = SCOPE_IDENTITY();
                EXEC dbo.SPDepartmentGetById @Id;
            END
            """);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SPDepartmentUpd]
                @Id int, @OfficeBranchId int, @Code nvarchar(20), @Name nvarchar(150), @IsActive bit = 1
            AS
            BEGIN
                SET NOCOUNT ON;
                IF NOT EXISTS (SELECT 1 FROM dbo.Department WHERE Id = @Id) RETURN;
                SET @Code = UPPER(LTRIM(RTRIM(@Code))); SET @Name = LTRIM(RTRIM(@Name));
                IF NULLIF(@Code, N'') IS NULL THROW 50020, 'Code is required.', 1;
                IF NULLIF(@Name, N'') IS NULL THROW 50021, 'Name is required.', 1;
                IF NOT EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE Id = @OfficeBranchId)
                    THROW 50030, 'Office branch was not found.', 1;
                IF @IsActive = 1 AND NOT EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE Id = @OfficeBranchId AND IsActive = 1)
                    THROW 50031, 'An active department cannot belong to an inactive branch.', 1;
                IF EXISTS (SELECT 1 FROM dbo.Department WHERE OfficeBranchId = @OfficeBranchId AND Code = @Code AND Id <> @Id)
                    THROW 50032, 'This department code already exists in the selected branch.', 1;
                UPDATE dbo.Department SET OfficeBranchId = @OfficeBranchId, Code = @Code,
                    Name = @Name, IsActive = @IsActive WHERE Id = @Id;
                EXEC dbo.SPDepartmentGetById @Id;
            END
            """);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SPDepartmentDel] @Id int
            AS
            BEGIN
                SET NOCOUNT ON;
                IF NOT EXISTS (SELECT 1 FROM dbo.Department WHERE Id = @Id)
                BEGIN SELECT CAST(0 AS bit); RETURN; END
                UPDATE dbo.Department SET IsActive = 0 WHERE Id = @Id;
                SELECT CAST(1 AS bit);
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP PROCEDURE IF EXISTS dbo.SPDepartmentDel;
            DROP PROCEDURE IF EXISTS dbo.SPDepartmentUpd;
            DROP PROCEDURE IF EXISTS dbo.SPDepartmentIns;
            DROP PROCEDURE IF EXISTS dbo.SPDepartmentGetById;
            DROP PROCEDURE IF EXISTS dbo.SPDepartmentDdl;
            DROP PROCEDURE IF EXISTS dbo.SPDepartmentGet;
            DROP PROCEDURE IF EXISTS dbo.SPOfficeBranchDel;
            DROP PROCEDURE IF EXISTS dbo.SPOfficeBranchUpd;
            DROP PROCEDURE IF EXISTS dbo.SPOfficeBranchIns;
            DROP PROCEDURE IF EXISTS dbo.SPOfficeBranchGetById;
            DROP PROCEDURE IF EXISTS dbo.SPOfficeBranchDdl;
            DROP PROCEDURE IF EXISTS dbo.SPOfficeBranchGet;
            """);
    }
}
