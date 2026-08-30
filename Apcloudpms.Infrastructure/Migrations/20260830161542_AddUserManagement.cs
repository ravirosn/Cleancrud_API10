using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAtUtc",
                table: "Users",
                type: "datetime2(0)",
                precision: 0,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedByUserId",
                table: "Users",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ModifiedByUserId",
                table: "Users",
                column: "ModifiedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_CreatedByUserId",
                table: "Users",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ModifiedByUserId",
                table: "Users",
                column: "ModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(UsersGetProcedure);
            migrationBuilder.Sql(UsersGetSavedProcedure);
            migrationBuilder.Sql(UsersAddProcedure);
            migrationBuilder.Sql(UsersEditProcedure);
            migrationBuilder.Sql(UsersDeleteProcedure);
            migrationBuilder.Sql(UserRolesGetProcedure);
            migrationBuilder.Sql(UserRolesSetProcedure);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpUserRolesSet];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpUserRolesGet];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpUsersDelete];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpUsersEdit];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpUsersAdd];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpUsersGetSaved];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpUsersGet];");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_CreatedByUserId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ModifiedByUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedByUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ModifiedByUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ModifiedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "Users");
        }

        private const string UsersGetProcedure = """
            CREATE OR ALTER PROCEDURE [dbo].[SpUsersGet]
                @PageNumber int = 1,
                @PageSize int = 10,
                @SearchTerm nvarchar(200) = NULL,
                @SortBy nvarchar(30) = N'createdAtUtc',
                @SortDirection varchar(4) = 'desc',
                @IncludeInactive bit = 0
            AS
            BEGIN
                SET NOCOUNT ON;
                IF @PageNumber < 1 THROW 50010, 'PageNumber must be greater than zero.', 1;
                IF @PageSize < 1 OR @PageSize > 100 THROW 50011, 'PageSize must be between 1 and 100.', 1;

                DECLARE @OrderColumn nvarchar(80) = CASE LOWER(@SortBy)
                    WHEN N'username' THEN N'u.[UserName]'
                    WHEN N'displayname' THEN N'u.[DisplayName]'
                    WHEN N'email' THEN N'u.[Email]'
                    WHEN N'officebranchname' THEN N'ob.[Name]'
                    WHEN N'departmentname' THEN N'd.[Name]'
                    WHEN N'status' THEN N'u.[IsActive]'
                    WHEN N'modifiedatutc' THEN N'u.[ModifiedAtUtc]'
                    ELSE N'u.[CreatedAtUtc]' END;
                SET @SortDirection = CASE WHEN LOWER(@SortDirection) = 'asc' THEN 'ASC' ELSE 'DESC' END;
                SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
                DECLARE @SearchPattern nvarchar(402) = NULL;
                IF @SearchTerm IS NOT NULL
                    SET @SearchPattern = N'%' + REPLACE(REPLACE(REPLACE(REPLACE(
                        @SearchTerm, N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[') + N'%';

                SELECT COUNT_BIG(1) AS [TotalRecords]
                FROM [dbo].[Users] u
                LEFT JOIN [dbo].[Department] d ON d.[Id] = u.[DepartmentId]
                LEFT JOIN [dbo].[OfficeBranch] ob ON ob.[Id] = d.[OfficeBranchId]
                WHERE (@IncludeInactive = 1 OR u.[IsActive] = 1)
                  AND (@SearchPattern IS NULL OR u.[UserName] LIKE @SearchPattern ESCAPE N'\'
                    OR u.[DisplayName] LIKE @SearchPattern ESCAPE N'\'
                    OR u.[Email] LIKE @SearchPattern ESCAPE N'\'
                    OR u.[ContactNumber] LIKE @SearchPattern ESCAPE N'\'
                    OR d.[Name] LIKE @SearchPattern ESCAPE N'\'
                    OR ob.[Name] LIKE @SearchPattern ESCAPE N'\');

                DECLARE @Sql nvarchar(max) = N'
                    SELECT u.[Id], u.[UserName], u.[DisplayName], u.[Email], u.[ContactNumber],
                        ob.[Id] AS [OfficeBranchId], ob.[Name] AS [OfficeBranchName],
                        d.[Id] AS [DepartmentId], d.[Name] AS [DepartmentName], u.[IsActive],
                        u.[CreatedAtUtc], u.[CreatedByUserId],
                        COALESCE(cb.[DisplayName], cb.[UserName]) AS [CreatedBy],
                        u.[ModifiedAtUtc], u.[ModifiedByUserId],
                        COALESCE(mb.[DisplayName], mb.[UserName]) AS [ModifiedBy]
                    FROM [dbo].[Users] u
                    LEFT JOIN [dbo].[Department] d ON d.[Id] = u.[DepartmentId]
                    LEFT JOIN [dbo].[OfficeBranch] ob ON ob.[Id] = d.[OfficeBranchId]
                    LEFT JOIN [dbo].[Users] cb ON cb.[Id] = u.[CreatedByUserId]
                    LEFT JOIN [dbo].[Users] mb ON mb.[Id] = u.[ModifiedByUserId]
                    WHERE (@pIncludeInactive = 1 OR u.[IsActive] = 1)
                      AND (@pSearchPattern IS NULL OR u.[UserName] LIKE @pSearchPattern ESCAPE N''\''
                        OR u.[DisplayName] LIKE @pSearchPattern ESCAPE N''\''
                        OR u.[Email] LIKE @pSearchPattern ESCAPE N''\''
                        OR u.[ContactNumber] LIKE @pSearchPattern ESCAPE N''\''
                        OR d.[Name] LIKE @pSearchPattern ESCAPE N''\''
                        OR ob.[Name] LIKE @pSearchPattern ESCAPE N''\'')
                    ORDER BY ' + @OrderColumn + N' ' + @SortDirection + N', u.[Id] DESC
                    OFFSET (@pPageNumber - 1) * @pPageSize ROWS FETCH NEXT @pPageSize ROWS ONLY;';
                EXEC sys.sp_executesql @Sql,
                    N'@pIncludeInactive bit, @pSearchPattern nvarchar(402), @pPageNumber int, @pPageSize int',
                    @IncludeInactive, @SearchPattern, @PageNumber, @PageSize;
            END;
            """;

        private const string UsersAddProcedure = """
            CREATE OR ALTER PROCEDURE [dbo].[SpUsersAdd]
                @UserName nvarchar(100), @PasswordHash nvarchar(255),
                @DisplayName nvarchar(200) = NULL, @Email nvarchar(320) = NULL,
                @ContactNumber nvarchar(20) = NULL, @OfficeBranchId int = NULL,
                @DepartmentId int = NULL, @IsActive bit = 1,
                @ActorUserId int = NULL, @ActorName nvarchar(256) = NULL,
                @TraceId nvarchar(100) = NULL, @IpAddress nvarchar(45) = NULL
            AS
            BEGIN
                SET NOCOUNT ON; SET XACT_ABORT ON;
                SET @UserName = LTRIM(RTRIM(@UserName));
                DECLARE @NormalizedUserName nvarchar(100) = UPPER(@UserName), @Now datetime2(0) = SYSUTCDATETIME();
                IF LEN(@UserName) < 3 THROW 50020, 'Username must contain at least three characters.', 1;
                IF NULLIF(@PasswordHash, N'') IS NULL THROW 50021, 'A password is required.', 1;
                IF (@OfficeBranchId IS NULL AND @DepartmentId IS NOT NULL) OR (@OfficeBranchId IS NOT NULL AND @DepartmentId IS NULL)
                    THROW 50022, 'Select both an office and a department, or leave both empty.', 1;
                IF @DepartmentId IS NOT NULL AND NOT EXISTS (
                    SELECT 1 FROM [dbo].[Department] d INNER JOIN [dbo].[OfficeBranch] ob ON ob.[Id] = d.[OfficeBranchId]
                    WHERE d.[Id] = @DepartmentId AND ob.[Id] = @OfficeBranchId AND d.[IsActive] = 1 AND ob.[IsActive] = 1)
                    THROW 50023, 'The selected department does not belong to the selected active office.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[Users] WITH (UPDLOCK, HOLDLOCK) WHERE [NormalizedUserName] = @NormalizedUserName)
                    THROW 50024, 'Username is already in use.', 1;

                BEGIN TRANSACTION;
                INSERT [dbo].[Users] ([UserName], [NormalizedUserName], [PasswordHash], [DisplayName], [Email],
                    [ContactNumber], [DepartmentId], [IsActive], [CreatedAtUtc], [CreatedByUserId])
                VALUES (@UserName, @NormalizedUserName, @PasswordHash, NULLIF(LTRIM(RTRIM(@DisplayName)), N''),
                    NULLIF(LTRIM(RTRIM(@Email)), N''), NULLIF(LTRIM(RTRIM(@ContactNumber)), N''),
                    @DepartmentId, @IsActive, @Now, @ActorUserId);
                DECLARE @Id int = CONVERT(int, SCOPE_IDENTITY());
                DECLARE @NewValues nvarchar(max) = (SELECT @UserName AS [UserName], N'[REDACTED]' AS [PasswordHash],
                    @DisplayName AS [DisplayName], @Email AS [Email], @ContactNumber AS [ContactNumber],
                    @DepartmentId AS [DepartmentId], @IsActive AS [IsActive], @Now AS [CreatedAtUtc],
                    @ActorUserId AS [CreatedByUserId] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES);
                INSERT [dbo].[AuditLog] ([EntityName], [Action], [EntityKey], [ChangedColumns], [NewValues],
                    [ChangedByUserId], [ChangedBy], [TraceId], [IpAddress], [ChangedAtUtc])
                VALUES (N'Users', N'Added', CONCAT(N'{"Id":', @Id, N'}'),
                    N'["UserName","PasswordHash","DisplayName","Email","ContactNumber","DepartmentId","IsActive","CreatedAtUtc","CreatedByUserId"]',
                    @NewValues, @ActorUserId, @ActorName, @TraceId, @IpAddress, @Now);
                COMMIT;
                EXEC [dbo].[SpUsersGetSaved] @Id;
            END;
            """;

        private const string UsersEditProcedure = """
            CREATE OR ALTER PROCEDURE [dbo].[SpUsersEdit]
                @Id int, @UserName nvarchar(100), @PasswordHash nvarchar(255) = NULL,
                @DisplayName nvarchar(200) = NULL, @Email nvarchar(320) = NULL,
                @ContactNumber nvarchar(20) = NULL, @OfficeBranchId int = NULL,
                @DepartmentId int = NULL, @IsActive bit = 1,
                @ActorUserId int = NULL, @ActorName nvarchar(256) = NULL,
                @TraceId nvarchar(100) = NULL, @IpAddress nvarchar(45) = NULL
            AS
            BEGIN
                SET NOCOUNT ON; SET XACT_ABORT ON;
                SET @UserName = LTRIM(RTRIM(@UserName));
                DECLARE @NormalizedUserName nvarchar(100) = UPPER(@UserName), @Now datetime2(0) = SYSUTCDATETIME();
                IF LEN(@UserName) < 3 THROW 50020, 'Username must contain at least three characters.', 1;
                IF @ActorUserId = @Id AND @IsActive = 0 THROW 50025, 'You cannot deactivate your own account.', 1;
                IF (@OfficeBranchId IS NULL AND @DepartmentId IS NOT NULL) OR (@OfficeBranchId IS NOT NULL AND @DepartmentId IS NULL)
                    THROW 50022, 'Select both an office and a department, or leave both empty.', 1;
                IF @DepartmentId IS NOT NULL AND NOT EXISTS (
                    SELECT 1 FROM [dbo].[Department] d INNER JOIN [dbo].[OfficeBranch] ob ON ob.[Id] = d.[OfficeBranchId]
                    WHERE d.[Id] = @DepartmentId AND ob.[Id] = @OfficeBranchId AND d.[IsActive] = 1 AND ob.[IsActive] = 1)
                    THROW 50023, 'The selected department does not belong to the selected active office.', 1;

                BEGIN TRANSACTION;
                IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = @Id)
                    THROW 50004, 'User was not found.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id] <> @Id AND [NormalizedUserName] = @NormalizedUserName)
                    THROW 50024, 'Username is already in use.', 1;
                DECLARE @OldValues nvarchar(max) = (SELECT [UserName], N'[REDACTED]' AS [PasswordHash], [DisplayName],
                    [Email], [ContactNumber], [DepartmentId], [IsActive], [ModifiedAtUtc], [ModifiedByUserId]
                    FROM [dbo].[Users] WHERE [Id] = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES);
                UPDATE [dbo].[Users] SET [UserName] = @UserName, [NormalizedUserName] = @NormalizedUserName,
                    [PasswordHash] = COALESCE(@PasswordHash, [PasswordHash]),
                    [DisplayName] = NULLIF(LTRIM(RTRIM(@DisplayName)), N''), [Email] = NULLIF(LTRIM(RTRIM(@Email)), N''),
                    [ContactNumber] = NULLIF(LTRIM(RTRIM(@ContactNumber)), N''), [DepartmentId] = @DepartmentId,
                    [IsActive] = @IsActive, [ModifiedAtUtc] = @Now, [ModifiedByUserId] = @ActorUserId WHERE [Id] = @Id;
                DECLARE @NewValues nvarchar(max) = (SELECT [UserName], N'[REDACTED]' AS [PasswordHash], [DisplayName],
                    [Email], [ContactNumber], [DepartmentId], [IsActive], [ModifiedAtUtc], [ModifiedByUserId]
                    FROM [dbo].[Users] WHERE [Id] = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES);
                INSERT [dbo].[AuditLog] ([EntityName], [Action], [EntityKey], [ChangedColumns], [OldValues], [NewValues],
                    [ChangedByUserId], [ChangedBy], [TraceId], [IpAddress], [ChangedAtUtc])
                VALUES (N'Users', N'Modified', CONCAT(N'{"Id":', @Id, N'}'),
                    N'["UserName","PasswordHash","DisplayName","Email","ContactNumber","DepartmentId","IsActive","ModifiedAtUtc","ModifiedByUserId"]',
                    @OldValues, @NewValues, @ActorUserId, @ActorName, @TraceId, @IpAddress, @Now);
                COMMIT;
                EXEC [dbo].[SpUsersGetSaved] @Id;
            END;
            """;

        private const string UsersDeleteProcedure = """
            CREATE OR ALTER PROCEDURE [dbo].[SpUsersDelete]
                @Id int, @ActorUserId int = NULL, @ActorName nvarchar(256) = NULL,
                @TraceId nvarchar(100) = NULL, @IpAddress nvarchar(45) = NULL
            AS
            BEGIN
                SET NOCOUNT ON; SET XACT_ABORT ON;
                IF @ActorUserId = @Id THROW 50025, 'You cannot deactivate your own account.', 1;
                DECLARE @Now datetime2(0) = SYSUTCDATETIME();
                BEGIN TRANSACTION;
                IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = @Id)
                BEGIN COMMIT; SELECT CAST(0 AS int); RETURN; END;
                DECLARE @OldValues nvarchar(max) = (SELECT [IsActive], [ModifiedAtUtc], [ModifiedByUserId]
                    FROM [dbo].[Users] WHERE [Id] = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES);
                UPDATE [dbo].[Users] SET [IsActive] = 0, [ModifiedAtUtc] = @Now, [ModifiedByUserId] = @ActorUserId WHERE [Id] = @Id;
                DECLARE @NewValues nvarchar(max) = (SELECT [IsActive], [ModifiedAtUtc], [ModifiedByUserId]
                    FROM [dbo].[Users] WHERE [Id] = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES);
                INSERT [dbo].[AuditLog] ([EntityName], [Action], [EntityKey], [ChangedColumns], [OldValues], [NewValues],
                    [ChangedByUserId], [ChangedBy], [TraceId], [IpAddress], [ChangedAtUtc])
                VALUES (N'Users', N'Modified', CONCAT(N'{"Id":', @Id, N'}'), N'["IsActive","ModifiedAtUtc","ModifiedByUserId"]',
                    @OldValues, @NewValues, @ActorUserId, @ActorName, @TraceId, @IpAddress, @Now);
                COMMIT; SELECT CAST(1 AS int);
            END;
            """;

        private const string UserRolesGetProcedure = """
            CREATE OR ALTER PROCEDURE [dbo].[SpUserRolesGet] @UserId int
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT [Id] AS [UserId], [UserName] FROM [dbo].[Users] WHERE [Id] = @UserId;
                SELECT r.[Id] AS [RoleId], r.[Name] AS [RoleName],
                    CONVERT(bit, CASE WHEN ur.[UserId] IS NOT NULL AND ur.[IsActive] = 1 THEN 1 ELSE 0 END) AS [IsAssigned]
                FROM [dbo].[Role] r LEFT JOIN [dbo].[UserRole] ur ON ur.[RoleId] = r.[Id] AND ur.[UserId] = @UserId
                WHERE r.[IsActive] = 1 ORDER BY r.[Name];
            END;
            """;

        private const string UserRolesSetProcedure = """
            CREATE OR ALTER PROCEDURE [dbo].[SpUserRolesSet]
                @UserId int, @RoleIdsJson nvarchar(max),
                @ActorUserId int = NULL, @ActorName nvarchar(256) = NULL,
                @TraceId nvarchar(100) = NULL, @IpAddress nvarchar(45) = NULL
            AS
            BEGIN
                SET NOCOUNT ON; SET XACT_ABORT ON;
                IF ISJSON(@RoleIdsJson) <> 1 THROW 50026, 'Role IDs must be a valid JSON array.', 1;
                DECLARE @Requested TABLE ([RoleId] int PRIMARY KEY);
                INSERT @Requested ([RoleId]) SELECT CONVERT(int, [value]) FROM OPENJSON(@RoleIdsJson);
                IF EXISTS (SELECT 1 FROM @Requested q LEFT JOIN [dbo].[Role] r ON r.[Id] = q.[RoleId]
                    WHERE r.[Id] IS NULL OR r.[IsActive] = 0)
                    THROW 50027, 'One or more selected roles do not exist or are inactive.', 1;
                DECLARE @Now datetime2(0) = SYSUTCDATETIME();
                BEGIN TRANSACTION;
                IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = @UserId)
                    THROW 50004, 'User was not found.', 1;
                IF @ActorUserId = @UserId
                   AND EXISTS (SELECT 1 FROM [dbo].[UserRole] ur INNER JOIN [dbo].[Role] r ON r.[Id] = ur.[RoleId]
                       WHERE ur.[UserId] = @UserId AND ur.[IsActive] = 1 AND r.[NormalizedName] = N'ADMIN')
                   AND NOT EXISTS (SELECT 1 FROM @Requested q INNER JOIN [dbo].[Role] r ON r.[Id] = q.[RoleId]
                       WHERE r.[NormalizedName] = N'ADMIN')
                    THROW 50028, 'You cannot remove your own Admin role.', 1;
                DECLARE @OldValues nvarchar(max) = (SELECT [RoleId] FROM [dbo].[UserRole]
                    WHERE [UserId] = @UserId AND [IsActive] = 1 ORDER BY [RoleId] FOR JSON PATH);
                UPDATE [dbo].[UserRole] SET [IsActive] = 0 WHERE [UserId] = @UserId AND [IsActive] = 1;
                UPDATE ur SET [IsActive] = 1, [AssignedAtUtc] = @Now
                    FROM [dbo].[UserRole] ur INNER JOIN @Requested q ON q.[RoleId] = ur.[RoleId]
                    WHERE ur.[UserId] = @UserId;
                INSERT [dbo].[UserRole] ([UserId], [RoleId], [IsActive], [AssignedAtUtc])
                    SELECT @UserId, q.[RoleId], 1, @Now FROM @Requested q
                    WHERE NOT EXISTS (SELECT 1 FROM [dbo].[UserRole] ur WHERE ur.[UserId] = @UserId AND ur.[RoleId] = q.[RoleId]);
                UPDATE [dbo].[Users] SET [ModifiedAtUtc] = @Now, [ModifiedByUserId] = @ActorUserId
                    WHERE [Id] = @UserId;
                DECLARE @NewValues nvarchar(max) = (SELECT [RoleId] FROM [dbo].[UserRole]
                    WHERE [UserId] = @UserId AND [IsActive] = 1 ORDER BY [RoleId] FOR JSON PATH);
                INSERT [dbo].[AuditLog] ([EntityName], [Action], [EntityKey], [ChangedColumns], [OldValues], [NewValues],
                    [ChangedByUserId], [ChangedBy], [TraceId], [IpAddress], [ChangedAtUtc])
                VALUES (N'UserRole', N'Modified', CONCAT(N'{"UserId":', @UserId, N'}'), N'["RoleIds"]',
                    @OldValues, @NewValues, @ActorUserId, @ActorName, @TraceId, @IpAddress, @Now);
                COMMIT;
            END;
            """;

        // Shared projection keeps add/edit result contracts identical without duplicating a long SELECT.
        private const string UsersGetSavedProcedure = """
            CREATE OR ALTER PROCEDURE [dbo].[SpUsersGetSaved] @Id int
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT u.[Id], u.[UserName], u.[DisplayName], u.[Email], u.[ContactNumber],
                    ob.[Id] AS [OfficeBranchId], ob.[Name] AS [OfficeBranchName],
                    d.[Id] AS [DepartmentId], d.[Name] AS [DepartmentName], u.[IsActive],
                    u.[CreatedAtUtc], u.[CreatedByUserId], u.[ModifiedAtUtc], u.[ModifiedByUserId]
                FROM [dbo].[Users] u LEFT JOIN [dbo].[Department] d ON d.[Id] = u.[DepartmentId]
                LEFT JOIN [dbo].[OfficeBranch] ob ON ob.[Id] = d.[OfficeBranchId] WHERE u.[Id] = @Id;
            END;
            """;
    }
}
