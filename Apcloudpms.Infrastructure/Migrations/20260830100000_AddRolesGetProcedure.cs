using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260830100000_AddRolesGetProcedure")]
public partial class AddRolesGetProcedure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SpRolesGet]
                @PageNumber int = 1,
                @PageSize int = 10,
                @SearchTerm nvarchar(100) = NULL,
                @IncludeInactive bit = 1
            AS
            BEGIN
                SET NOCOUNT ON;

                IF @PageNumber < 1
                    THROW 50010, 'PageNumber must be greater than zero.', 1;

                IF @PageSize < 1 OR @PageSize > 100
                    THROW 50011, 'PageSize must be between 1 and 100.', 1;

                SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');

                DECLARE @SearchPattern nvarchar(202) = NULL;
                IF @SearchTerm IS NOT NULL
                    SET @SearchPattern = N'%' +
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    REPLACE(@SearchTerm, N'\', N'\\'),
                                    N'%', N'\%'),
                                N'_', N'\_'),
                            N'[', N'\[') + N'%';

                SELECT COUNT_BIG(1) AS [TotalRecords]
                FROM [dbo].[Role] AS role
                WHERE (@IncludeInactive = 1 OR role.[IsActive] = 1)
                  AND (@SearchPattern IS NULL
                    OR role.[Name] LIKE @SearchPattern ESCAPE N'\');

                SELECT
                    role.[Id],
                    role.[Name],
                    role.[IsActive],
                    role.[CreatedAtUtc]
                FROM [dbo].[Role] AS role
                WHERE (@IncludeInactive = 1 OR role.[IsActive] = 1)
                  AND (@SearchPattern IS NULL
                    OR role.[Name] LIKE @SearchPattern ESCAPE N'\')
                ORDER BY role.[Name], role.[Id]
                OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
                FETCH NEXT @PageSize ROWS ONLY;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpRolesGet];");
    }
}

