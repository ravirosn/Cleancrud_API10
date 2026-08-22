using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260816000000_SeedSampleUsersAndRelatedData")]
public partial class SeedSampleUsersAndRelatedData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            SET NOCOUNT ON;

            DECLARE @SeededAtUtc datetime2(0) = '2026-08-16T00:00:00';
            DECLARE @PasswordHash nvarchar(255) = N'$2a$11$R3/qxdrL26yvYqS3zW64AevR5CurYbohuDIzhRH3TnTHYf5XI2Qnq';
            DECLARE @SeedHeadOffice bit = CASE
                WHEN EXISTS (
                    SELECT 1 FROM [dbo].[OfficeBranch]
                    WHERE [IsHeadOffice] = 1 AND [IsActive] = 1)
                THEN 0 ELSE 1 END;

            MERGE [dbo].[Role] AS target
            USING (VALUES
                (N'User', N'USER'),
                (N'Admin', N'ADMIN'),
                (N'Manager', N'MANAGER'),
                (N'Reviewer', N'REVIEWER')
            ) AS source ([Name], [NormalizedName])
            ON target.[NormalizedName] = source.[NormalizedName]
            WHEN NOT MATCHED THEN
                INSERT ([Name], [NormalizedName], [IsActive], [CreatedAtUtc])
                VALUES (source.[Name], source.[NormalizedName], 1, @SeededAtUtc);

            MERGE [dbo].[OfficeBranch] AS target
            USING (VALUES
                (N'KTM-HO', N'Kathmandu Head Office', N'Kathmandu, Bagmati', @SeedHeadOffice),
                (N'PKR', N'Pokhara Branch', N'Pokhara, Gandaki', CAST(0 AS bit)),
                (N'BRT', N'Biratnagar Branch', N'Biratnagar, Koshi', CAST(0 AS bit))
            ) AS source ([Code], [Name], [Address], [IsHeadOffice])
            ON target.[Code] = source.[Code]
            WHEN NOT MATCHED THEN
                INSERT ([Code], [Name], [Address], [IsHeadOffice], [IsActive], [CreatedAtUtc])
                VALUES (source.[Code], source.[Name], source.[Address], source.[IsHeadOffice], 1, @SeededAtUtc);

            MERGE [dbo].[Department] AS target
            USING (
                SELECT branch.[Id] AS [OfficeBranchId], source.[Code], source.[Name]
                FROM (VALUES
                    (N'KTM-HO', N'ADMIN', N'Administration'),
                    (N'KTM-HO', N'FIN', N'Finance'),
                    (N'KTM-HO', N'IT', N'Information Technology'),
                    (N'PKR', N'OPS', N'Operations'),
                    (N'PKR', N'CS', N'Customer Service'),
                    (N'BRT', N'OPS', N'Operations'),
                    (N'BRT', N'CS', N'Customer Service')
                ) AS source ([BranchCode], [Code], [Name])
                INNER JOIN [dbo].[OfficeBranch] branch ON branch.[Code] = source.[BranchCode]
            ) AS source
            ON target.[OfficeBranchId] = source.[OfficeBranchId] AND target.[Code] = source.[Code]
            WHEN NOT MATCHED THEN
                INSERT ([OfficeBranchId], [Code], [Name], [IsActive], [CreatedAtUtc])
                VALUES (source.[OfficeBranchId], source.[Code], source.[Name], 1, @SeededAtUtc);

            MERGE [dbo].[Users] AS target
            USING (VALUES
                (N'demo.admin', N'DEMO.ADMIN', N'Aarav Shrestha', N'demo.admin@cleancrud.local'),
                (N'demo.asha', N'DEMO.ASHA', N'Asha Karki', N'demo.asha@cleancrud.local'),
                (N'demo.bibek', N'DEMO.BIBEK', N'Bibek Thapa', N'demo.bibek@cleancrud.local'),
                (N'demo.deepa', N'DEMO.DEEPA', N'Deepa Rai', N'demo.deepa@cleancrud.local'),
                (N'demo.gaurav', N'DEMO.GAURAV', N'Gaurav Adhikari', N'demo.gaurav@cleancrud.local'),
                (N'demo.kabita', N'DEMO.KABITA', N'Kabita Gurung', N'demo.kabita@cleancrud.local'),
                (N'demo.nabin', N'DEMO.NABIN', N'Nabin Maharjan', N'demo.nabin@cleancrud.local'),
                (N'demo.priya', N'DEMO.PRIYA', N'Priya Bhandari', N'demo.priya@cleancrud.local'),
                (N'demo.roshan', N'DEMO.ROSHAN', N'Roshan Lama', N'demo.roshan@cleancrud.local'),
                (N'demo.sushma', N'DEMO.SUSHMA', N'Sushma Poudel', N'demo.sushma@cleancrud.local')
            ) AS source ([UserName], [NormalizedUserName], [DisplayName], [Email])
            ON target.[NormalizedUserName] = source.[NormalizedUserName]
            WHEN NOT MATCHED THEN
                INSERT ([UserName], [NormalizedUserName], [PasswordHash], [DisplayName], [Email], [IsActive], [CreatedAtUtc])
                VALUES (source.[UserName], source.[NormalizedUserName], @PasswordHash, source.[DisplayName], source.[Email], 1, @SeededAtUtc);

            MERGE [dbo].[UserRole] AS target
            USING (
                SELECT users.[Id] AS [UserId], roles.[Id] AS [RoleId]
                FROM (VALUES
                    (N'DEMO.ADMIN', N'ADMIN'),
                    (N'DEMO.ASHA', N'MANAGER'),
                    (N'DEMO.BIBEK', N'REVIEWER'),
                    (N'DEMO.DEEPA', N'USER'),
                    (N'DEMO.GAURAV', N'USER'),
                    (N'DEMO.KABITA', N'REVIEWER'),
                    (N'DEMO.NABIN', N'USER'),
                    (N'DEMO.PRIYA', N'MANAGER'),
                    (N'DEMO.ROSHAN', N'USER'),
                    (N'DEMO.SUSHMA', N'USER')
                ) AS assignments ([NormalizedUserName], [NormalizedRoleName])
                INNER JOIN [dbo].[Users] users
                    ON users.[NormalizedUserName] = assignments.[NormalizedUserName]
                INNER JOIN [dbo].[Role] roles
                    ON roles.[NormalizedName] = assignments.[NormalizedRoleName]
            ) AS source
            ON target.[UserId] = source.[UserId] AND target.[RoleId] = source.[RoleId]
            WHEN NOT MATCHED THEN
                INSERT ([UserId], [RoleId], [IsActive], [AssignedAtUtc])
                VALUES (source.[UserId], source.[RoleId], 1, @SeededAtUtc);

            MERGE [dbo].[UserModule] AS target
            USING (
                SELECT users.[Id] AS [UserId], modules.[Id] AS [ApplicationModuleId]
                FROM (VALUES
                    (N'DEMO.ADMIN', N'PERMIT'), (N'DEMO.ADMIN', N'VISITOR'), (N'DEMO.ADMIN', N'ASSET'), (N'DEMO.ADMIN', N'POWERBI'),
                    (N'DEMO.ASHA', N'PERMIT'), (N'DEMO.ASHA', N'POWERBI'),
                    (N'DEMO.BIBEK', N'PERMIT'),
                    (N'DEMO.DEEPA', N'VISITOR'),
                    (N'DEMO.GAURAV', N'ASSET'),
                    (N'DEMO.KABITA', N'PERMIT'), (N'DEMO.KABITA', N'VISITOR'),
                    (N'DEMO.NABIN', N'ASSET'),
                    (N'DEMO.PRIYA', N'VISITOR'), (N'DEMO.PRIYA', N'POWERBI'),
                    (N'DEMO.ROSHAN', N'PERMIT'), (N'DEMO.ROSHAN', N'ASSET'),
                    (N'DEMO.SUSHMA', N'VISITOR')
                ) AS assignments ([NormalizedUserName], [ModuleCode])
                INNER JOIN [dbo].[Users] users
                    ON users.[NormalizedUserName] = assignments.[NormalizedUserName]
                INNER JOIN [dbo].[ApplicationModule] modules
                    ON modules.[Code] = assignments.[ModuleCode]
            ) AS source
            ON target.[UserId] = source.[UserId]
                AND target.[ApplicationModuleId] = source.[ApplicationModuleId]
            WHEN NOT MATCHED THEN
                INSERT ([UserId], [ApplicationModuleId], [IsActive], [AssignedAtUtc])
                VALUES (source.[UserId], source.[ApplicationModuleId], 1, @SeededAtUtc);

            MERGE [dbo].[Students] AS target
            USING (VALUES
                (N'Asha Karki', N'asha.karki@example.com', N'9800000001'),
                (N'Bibek Thapa', N'bibek.thapa@example.com', N'9800000002'),
                (N'Deepa Rai', N'deepa.rai@example.com', N'9800000003'),
                (N'Gaurav Adhikari', N'gaurav.adhikari@example.com', N'9800000004'),
                (N'Kabita Gurung', N'kabita.gurung@example.com', N'9800000005'),
                (N'Nabin Maharjan', N'nabin.maharjan@example.com', N'9800000006'),
                (N'Priya Bhandari', N'priya.bhandari@example.com', N'9800000007'),
                (N'Roshan Lama', N'roshan.lama@example.com', N'9800000008'),
                (N'Sushma Poudel', N'sushma.poudel@example.com', N'9800000009'),
                (N'Samir Khadka', N'samir.khadka@example.com', N'9800000010')
            ) AS source ([Name], [Email], [MobileNo])
            ON target.[Email] = source.[Email]
            WHEN NOT MATCHED THEN
                INSERT ([Name], [Email], [MobileNo])
                VALUES (source.[Name], source.[Email], source.[MobileNo]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            SET NOCOUNT ON;

            DELETE FROM [dbo].[Users]
            WHERE [NormalizedUserName] IN (
                N'DEMO.ADMIN', N'DEMO.ASHA', N'DEMO.BIBEK', N'DEMO.DEEPA', N'DEMO.GAURAV',
                N'DEMO.KABITA', N'DEMO.NABIN', N'DEMO.PRIYA', N'DEMO.ROSHAN', N'DEMO.SUSHMA');

            DELETE FROM [dbo].[Students]
            WHERE [Email] IN (
                N'asha.karki@example.com', N'bibek.thapa@example.com', N'deepa.rai@example.com',
                N'gaurav.adhikari@example.com', N'kabita.gurung@example.com',
                N'nabin.maharjan@example.com', N'priya.bhandari@example.com',
                N'roshan.lama@example.com', N'sushma.poudel@example.com', N'samir.khadka@example.com');

            DELETE department
            FROM [dbo].[Department] department
            INNER JOIN [dbo].[OfficeBranch] branch ON branch.[Id] = department.[OfficeBranchId]
            WHERE (branch.[Code] = N'KTM-HO' AND department.[Code] IN (N'ADMIN', N'FIN', N'IT'))
               OR (branch.[Code] IN (N'PKR', N'BRT') AND department.[Code] IN (N'OPS', N'CS'));

            DELETE branch
            FROM [dbo].[OfficeBranch] branch
            WHERE branch.[Code] IN (N'KTM-HO', N'PKR', N'BRT')
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[Department] department
                  WHERE department.[OfficeBranchId] = branch.[Id]);

            DELETE role
            FROM [dbo].[Role] role
            WHERE role.[NormalizedName] IN (N'MANAGER', N'REVIEWER')
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[UserRole] userRole WHERE userRole.[RoleId] = role.[Id]);
            """);
    }
}
