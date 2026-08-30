IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Students] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Students] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260614174158_InitialCreate', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Students] ADD [MobileNo] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260615172705_jasbdnabd', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [UserName] nvarchar(max) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    [Role] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260617175007_AddUserTable', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
IF EXISTS (SELECT 1 FROM [Users] WHERE LEN([UserName]) > 100)
    THROW 51000, 'A UserName exceeds 100 characters. Fix it before applying this migration.', 1;
IF EXISTS (SELECT 1 FROM [Users] WHERE LEN([Role]) > 50)
    THROW 51000, 'A Role exceeds 50 characters. Fix it before applying this migration.', 1;
IF EXISTS (
    SELECT UPPER(LTRIM(RTRIM([UserName])))
    FROM [Users]
    GROUP BY UPPER(LTRIM(RTRIM([UserName])))
    HAVING COUNT(*) > 1)
    THROW 51000, 'Duplicate normalized usernames exist. Resolve them before applying this migration.', 1;

EXEC sp_rename N'[Users].[Password]', N'PasswordHash', 'COLUMN';

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'UserName');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [Users] ALTER COLUMN [UserName] nvarchar(100) NOT NULL;

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Role');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [Users] ALTER COLUMN [Role] nvarchar(50) NOT NULL;

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'PasswordHash');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [Users] ALTER COLUMN [PasswordHash] nvarchar(255) NOT NULL;

ALTER TABLE [Users] ADD [CreatedAtUtc] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME());

ALTER TABLE [Users] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);

ALTER TABLE [Users] ADD [NormalizedUserName] nvarchar(100) NULL;

UPDATE [Users] SET [NormalizedUserName] = UPPER(LTRIM(RTRIM([UserName])))

DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'NormalizedUserName');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var3 + ';');
ALTER TABLE [Users] ALTER COLUMN [NormalizedUserName] nvarchar(100) NOT NULL;

CREATE TABLE [RefreshTokens] (
    [Id] bigint NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [TokenHash] char(64) NOT NULL,
    [FamilyId] uniqueidentifier NOT NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL,
    [ExpiresAtUtc] datetime2(0) NOT NULL,
    [SessionExpiresAtUtc] datetime2(0) NOT NULL,
    [RevokedAtUtc] datetime2(0) NULL,
    [ReplacedByTokenHash] char(64) NULL,
    [CreatedByIp] nvarchar(45) NULL,
    [RevokedByIp] nvarchar(45) NULL,
    [RevocationReason] nvarchar(100) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Users_NormalizedUserName] ON [Users] ([NormalizedUserName]);

CREATE INDEX [IX_RefreshTokens_FamilyId_RevokedAtUtc] ON [RefreshTokens] ([FamilyId], [RevokedAtUtc]);

CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);

CREATE INDEX [IX_RefreshTokens_UserId_ExpiresAtUtc] ON [RefreshTokens] ([UserId], [ExpiresAtUtc]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260815110619_AddJwtRefreshTokens', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[OfficeBranch] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(20) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Address] nvarchar(500) NULL,
    [IsHeadOffice] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL,
    CONSTRAINT [PK_OfficeBranch] PRIMARY KEY ([Id])
);

CREATE TABLE [dbo].[Role] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    [NormalizedName] nvarchar(50) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL,
    CONSTRAINT [PK_Role] PRIMARY KEY ([Id])
);

CREATE TABLE [dbo].[Department] (
    [Id] int NOT NULL IDENTITY,
    [OfficeBranchId] int NOT NULL,
    [Code] nvarchar(20) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL,
    CONSTRAINT [PK_Department] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Department_OfficeBranch_OfficeBranchId] FOREIGN KEY ([OfficeBranchId]) REFERENCES [dbo].[OfficeBranch] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[UserRole] (
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [AssignedAtUtc] datetime2(0) NOT NULL,
    CONSTRAINT [PK_UserRole] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRole_Role_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Role] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserRole_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

INSERT INTO [dbo].[Role] ([Name], [NormalizedName], [IsActive], [CreatedAtUtc])
VALUES (N'User', N'USER', 1, SYSUTCDATETIME()),
       (N'Admin', N'ADMIN', 1, SYSUTCDATETIME());

INSERT INTO [dbo].[Role] ([Name], [NormalizedName], [IsActive], [CreatedAtUtc])
SELECT MIN(LTRIM(RTRIM([Role]))), UPPER(LTRIM(RTRIM([Role]))), 1, SYSUTCDATETIME()
FROM [Users]
WHERE LTRIM(RTRIM([Role])) <> N''
  AND UPPER(LTRIM(RTRIM([Role]))) NOT IN (N'USER', N'ADMIN')
GROUP BY UPPER(LTRIM(RTRIM([Role])));

INSERT INTO [dbo].[UserRole] ([UserId], [RoleId], [IsActive], [AssignedAtUtc])
SELECT u.[Id], r.[Id], 1, SYSUTCDATETIME()
FROM [Users] u
INNER JOIN [dbo].[Role] r
    ON r.[NormalizedName] = COALESCE(
        NULLIF(UPPER(LTRIM(RTRIM(u.[Role]))), N''), N'USER');

DECLARE @var4 nvarchar(max);
SELECT @var4 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Role');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var4 + ';');
ALTER TABLE [Users] DROP COLUMN [Role];

ALTER TABLE [dbo].[OfficeBranch] ADD CONSTRAINT [CK_OfficeBranch_HeadOfficeActive] CHECK ([IsHeadOffice] = 0 OR [IsActive] = 1);

CREATE UNIQUE INDEX [IX_Department_OfficeBranchId_Code] ON [dbo].[Department] ([OfficeBranchId], [Code]);

CREATE INDEX [IX_Department_OfficeBranchId_IsActive] ON [dbo].[Department] ([OfficeBranchId], [IsActive]);

CREATE UNIQUE INDEX [IX_OfficeBranch_Code] ON [dbo].[OfficeBranch] ([Code]);

CREATE UNIQUE INDEX [IX_OfficeBranch_IsHeadOffice] ON [dbo].[OfficeBranch] ([IsHeadOffice]) WHERE [IsHeadOffice] = 1 AND [IsActive] = 1;

CREATE UNIQUE INDEX [IX_Role_NormalizedName] ON [dbo].[Role] ([NormalizedName]);

CREATE INDEX [IX_UserRole_RoleId_IsActive] ON [dbo].[UserRole] ([RoleId], [IsActive]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260815164114_AddRolesBranchesAndDepartments', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[ApplicationModule] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [Icon] nvarchar(100) NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL,
    CONSTRAINT [PK_ApplicationModule] PRIMARY KEY ([Id])
);

CREATE TABLE [dbo].[ModuleMenu] (
    [Id] int NOT NULL IDENTITY,
    [ApplicationModuleId] int NOT NULL,
    [ParentMenuId] int NULL,
    [Name] nvarchar(100) NOT NULL,
    [ControllerName] nvarchar(100) NOT NULL,
    [ActionName] nvarchar(100) NOT NULL,
    [QueryUrl] nvarchar(500) NOT NULL,
    [Icon] nvarchar(100) NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL,
    CONSTRAINT [PK_ModuleMenu] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ModuleMenu_ApplicationModule_ApplicationModuleId] FOREIGN KEY ([ApplicationModuleId]) REFERENCES [dbo].[ApplicationModule] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ModuleMenu_ModuleMenu_ParentMenuId] FOREIGN KEY ([ParentMenuId]) REFERENCES [dbo].[ModuleMenu] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[UserModule] (
    [UserId] int NOT NULL,
    [ApplicationModuleId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [AssignedAtUtc] datetime2(0) NOT NULL,
    CONSTRAINT [PK_UserModule] PRIMARY KEY ([UserId], [ApplicationModuleId]),
    CONSTRAINT [FK_UserModule_ApplicationModule_ApplicationModuleId] FOREIGN KEY ([ApplicationModuleId]) REFERENCES [dbo].[ApplicationModule] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserModule_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAtUtc', N'Description', N'DisplayOrder', N'Icon', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[dbo].[ApplicationModule]'))
    SET IDENTITY_INSERT [dbo].[ApplicationModule] ON;
INSERT INTO [dbo].[ApplicationModule] ([Id], [Code], [CreatedAtUtc], [Description], [DisplayOrder], [Icon], [IsActive], [Name])
VALUES (1, N'PERMIT', '2026-08-15T00:00:00Z', N'Manage permit applications, reviews, and approvals.', 1, N'file-check', CAST(1 AS bit), N'Permit Management System'),
(2, N'VISITOR', '2026-08-15T00:00:00Z', N'Manage visitor registration, check-in, and visit history.', 2, N'users', CAST(1 AS bit), N'Visitor Management System'),
(3, N'ASSET', '2026-08-15T00:00:00Z', N'Manage organizational assets and assignments.', 3, N'package', CAST(1 AS bit), N'Asset Management System');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAtUtc', N'Description', N'DisplayOrder', N'Icon', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[dbo].[ApplicationModule]'))
    SET IDENTITY_INSERT [dbo].[ApplicationModule] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ActionName', N'ApplicationModuleId', N'ControllerName', N'CreatedAtUtc', N'DisplayOrder', N'Icon', N'IsActive', N'Name', N'ParentMenuId', N'QueryUrl') AND [object_id] = OBJECT_ID(N'[dbo].[ModuleMenu]'))
    SET IDENTITY_INSERT [dbo].[ModuleMenu] ON;
INSERT INTO [dbo].[ModuleMenu] ([Id], [ActionName], [ApplicationModuleId], [ControllerName], [CreatedAtUtc], [DisplayOrder], [Icon], [IsActive], [Name], [ParentMenuId], [QueryUrl])
VALUES (1, N'Index', 1, N'PermitDashboard', '2026-08-15T00:00:00Z', 1, N'dashboard', CAST(1 AS bit), N'Dashboard', NULL, N'/api/permit/dashboard'),
(2, N'Index', 1, N'PermitApplications', '2026-08-15T00:00:00Z', 2, N'file-text', CAST(1 AS bit), N'Permit Applications', NULL, N'/api/permit/applications'),
(3, N'Index', 1, N'PermitApprovals', '2026-08-15T00:00:00Z', 3, N'check-circle', CAST(1 AS bit), N'Permit Approvals', NULL, N'/api/permit/approvals'),
(4, N'Index', 2, N'VisitorDashboard', '2026-08-15T00:00:00Z', 1, N'dashboard', CAST(1 AS bit), N'Dashboard', NULL, N'/api/visitor/dashboard'),
(5, N'Index', 2, N'VisitorCheckIn', '2026-08-15T00:00:00Z', 2, N'log-in', CAST(1 AS bit), N'Visitor Check-In', NULL, N'/api/visitor/check-in'),
(6, N'Index', 2, N'VisitorLog', '2026-08-15T00:00:00Z', 3, N'list', CAST(1 AS bit), N'Visitor Log', NULL, N'/api/visitor/log'),
(7, N'Index', 3, N'AssetDashboard', '2026-08-15T00:00:00Z', 1, N'dashboard', CAST(1 AS bit), N'Dashboard', NULL, N'/api/asset/dashboard'),
(8, N'Index', 3, N'AssetRegister', '2026-08-15T00:00:00Z', 2, N'archive', CAST(1 AS bit), N'Asset Register', NULL, N'/api/asset/register'),
(9, N'Index', 3, N'AssetAssignments', '2026-08-15T00:00:00Z', 3, N'user-check', CAST(1 AS bit), N'Asset Assignments', NULL, N'/api/asset/assignments');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ActionName', N'ApplicationModuleId', N'ControllerName', N'CreatedAtUtc', N'DisplayOrder', N'Icon', N'IsActive', N'Name', N'ParentMenuId', N'QueryUrl') AND [object_id] = OBJECT_ID(N'[dbo].[ModuleMenu]'))
    SET IDENTITY_INSERT [dbo].[ModuleMenu] OFF;

INSERT INTO [dbo].[UserModule]
    ([UserId], [ApplicationModuleId], [IsActive], [AssignedAtUtc])
SELECT u.[Id], m.[Id], 1, SYSUTCDATETIME()
FROM [Users] u
CROSS JOIN [dbo].[ApplicationModule] m
WHERE u.[IsActive] = 1
  AND (
      m.[Id] IN (1, 2)
      OR EXISTS (
          SELECT 1
          FROM [dbo].[UserRole] ur
          INNER JOIN [dbo].[Role] r ON r.[Id] = ur.[RoleId]
          WHERE ur.[UserId] = u.[Id]
            AND ur.[IsActive] = 1
            AND r.[IsActive] = 1
            AND r.[NormalizedName] = N'ADMIN'
      )
  );

CREATE UNIQUE INDEX [IX_ApplicationModule_Code] ON [dbo].[ApplicationModule] ([Code]);

CREATE INDEX [IX_ApplicationModule_IsActive_DisplayOrder] ON [dbo].[ApplicationModule] ([IsActive], [DisplayOrder]);

CREATE INDEX [IX_ModuleMenu_ApplicationModuleId_IsActive_DisplayOrder] ON [dbo].[ModuleMenu] ([ApplicationModuleId], [IsActive], [DisplayOrder]);

CREATE UNIQUE INDEX [IX_ModuleMenu_ApplicationModuleId_QueryUrl] ON [dbo].[ModuleMenu] ([ApplicationModuleId], [QueryUrl]);

CREATE INDEX [IX_ModuleMenu_ParentMenuId] ON [dbo].[ModuleMenu] ([ParentMenuId]);

CREATE INDEX [IX_UserModule_ApplicationModuleId_IsActive] ON [dbo].[UserModule] ([ApplicationModuleId], [IsActive]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260815172733_AddApplicationModulesAndNavigation', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var5 nvarchar(max);
SELECT @var5 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'PasswordHash');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var5 + ';');
ALTER TABLE [Users] ALTER COLUMN [PasswordHash] nvarchar(255) NULL;

ALTER TABLE [Users] ADD [DisplayName] nvarchar(200) NULL;

ALTER TABLE [Users] ADD [Email] nvarchar(320) NULL;

ALTER TABLE [Users] ADD [EntraObjectId] uniqueidentifier NULL;

ALTER TABLE [Users] ADD [EntraTenantId] uniqueidentifier NULL;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAtUtc', N'Description', N'DisplayOrder', N'Icon', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[dbo].[ApplicationModule]'))
    SET IDENTITY_INSERT [dbo].[ApplicationModule] ON;
INSERT INTO [dbo].[ApplicationModule] ([Id], [Code], [CreatedAtUtc], [Description], [DisplayOrder], [Icon], [IsActive], [Name])
VALUES (4, N'POWERBI', '2026-08-15T00:00:00Z', N'View embedded Power BI dashboards and reports.', 4, N'bar-chart', CAST(1 AS bit), N'Analytics and Reports');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAtUtc', N'Description', N'DisplayOrder', N'Icon', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[dbo].[ApplicationModule]'))
    SET IDENTITY_INSERT [dbo].[ApplicationModule] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ActionName', N'ApplicationModuleId', N'ControllerName', N'CreatedAtUtc', N'DisplayOrder', N'Icon', N'IsActive', N'Name', N'ParentMenuId', N'QueryUrl') AND [object_id] = OBJECT_ID(N'[dbo].[ModuleMenu]'))
    SET IDENTITY_INSERT [dbo].[ModuleMenu] ON;
INSERT INTO [dbo].[ModuleMenu] ([Id], [ActionName], [ApplicationModuleId], [ControllerName], [CreatedAtUtc], [DisplayOrder], [Icon], [IsActive], [Name], [ParentMenuId], [QueryUrl])
VALUES (10, N'GetEmbedConfig', 4, N'PowerBi', '2026-08-15T00:00:00Z', 1, N'bar-chart-2', CAST(1 AS bit), N'Power BI Report', NULL, N'/api/power-bi/embed-config');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ActionName', N'ApplicationModuleId', N'ControllerName', N'CreatedAtUtc', N'DisplayOrder', N'Icon', N'IsActive', N'Name', N'ParentMenuId', N'QueryUrl') AND [object_id] = OBJECT_ID(N'[dbo].[ModuleMenu]'))
    SET IDENTITY_INSERT [dbo].[ModuleMenu] OFF;

INSERT INTO [dbo].[UserModule]
    ([UserId], [ApplicationModuleId], [IsActive], [AssignedAtUtc])
SELECT DISTINCT ur.[UserId], 4, 1, SYSUTCDATETIME()
FROM [dbo].[UserRole] ur
INNER JOIN [dbo].[Role] r ON r.[Id] = ur.[RoleId]
INNER JOIN [Users] u ON u.[Id] = ur.[UserId]
WHERE ur.[IsActive] = 1
  AND r.[IsActive] = 1
  AND r.[NormalizedName] = N'ADMIN'
  AND u.[IsActive] = 1
  AND NOT EXISTS (
      SELECT 1 FROM [dbo].[UserModule] um
      WHERE um.[UserId] = ur.[UserId]
        AND um.[ApplicationModuleId] = 4
  );

CREATE UNIQUE INDEX [IX_Users_EntraTenantId_EntraObjectId] ON [Users] ([EntraTenantId], [EntraObjectId]) WHERE [EntraTenantId] IS NOT NULL AND [EntraObjectId] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260815180307_AddEntraIdentityAndPowerBi', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
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

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260816000000_SeedSampleUsersAndRelatedData', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[AuditLog] (
    [Id] bigint NOT NULL IDENTITY,
    [EntityName] nvarchar(128) NOT NULL,
    [Action] nvarchar(20) NOT NULL,
    [EntityKey] nvarchar(max) NULL,
    [ChangedColumns] nvarchar(max) NULL,
    [OldValues] nvarchar(max) NULL,
    [NewValues] nvarchar(max) NULL,
    [ChangedByUserId] int NULL,
    [ChangedBy] nvarchar(256) NULL,
    [TraceId] nvarchar(100) NULL,
    [IpAddress] nvarchar(45) NULL,
    [ChangedAtUtc] datetime2(0) NOT NULL,
    CONSTRAINT [PK_AuditLog] PRIMARY KEY ([Id])
);

DECLARE @var6 nvarchar(max);
SELECT @var6 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[ListItemCategory]') AND [c].[name] = N'CategoryName');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ListItemCategory] DROP CONSTRAINT ' + @var6 + ';');
ALTER TABLE [dbo].[ListItemCategory] ALTER COLUMN [CategoryName] nvarchar(100) NOT NULL;

ALTER TABLE [dbo].[ListItemCategory] ADD [Code] nvarchar(50) NOT NULL;

ALTER TABLE [dbo].[ListItemCategory] ADD [Description] nvarchar(500) NULL;

ALTER TABLE [dbo].[ListItemCategory] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);

ALTER TABLE [dbo].[ListItemCategory] ADD [CreatedAtUtc] datetime2(0) NOT NULL DEFAULT '2026-08-16T00:00:00Z';

ALTER TABLE [dbo].[ListItemCategory] ADD [UpdatedAtUtc] datetime2(0) NULL;

DECLARE @var7 nvarchar(max);
SELECT @var7 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[ListItem]') AND [c].[name] = N'ItemName');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ListItem] DROP CONSTRAINT ' + @var7 + ';');
ALTER TABLE [dbo].[ListItem] ALTER COLUMN [ItemName] nvarchar(100) NOT NULL;

ALTER TABLE [dbo].[ListItem] ADD [Description] nvarchar(500) NULL;

ALTER TABLE [dbo].[ListItem] ADD [DisplayOrder] int NOT NULL DEFAULT 0;

ALTER TABLE [dbo].[ListItem] ADD [CreatedAtUtc] datetime2(0) NOT NULL DEFAULT '2026-08-16T00:00:00Z';

ALTER TABLE [dbo].[ListItem] ADD [UpdatedAtUtc] datetime2(0) NULL;

ALTER TABLE [dbo].[ListItem] DROP CONSTRAINT [UK_ListItem_SystemName];

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ListItemCategoryId', N'CategoryName', N'Code', N'CreatedAtUtc', N'Description', N'IsActive') AND [object_id] = OBJECT_ID(N'[dbo].[ListItemCategory]'))
    SET IDENTITY_INSERT [dbo].[ListItemCategory] ON;
INSERT INTO [dbo].[ListItemCategory] ([ListItemCategoryId], [CategoryName], [Code], [CreatedAtUtc], [Description], [IsActive])
VALUES (1, N'Permit Status', N'PERMIT_STATUS', '2026-08-16T00:00:00Z', N'Workflow statuses for permit applications.', CAST(1 AS bit)),
(2, N'Permit Type', N'PERMIT_TYPE', '2026-08-16T00:00:00Z', N'Available permit application types.', CAST(1 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ListItemCategoryId', N'CategoryName', N'Code', N'CreatedAtUtc', N'Description', N'IsActive') AND [object_id] = OBJECT_ID(N'[dbo].[ListItemCategory]'))
    SET IDENTITY_INSERT [dbo].[ListItemCategory] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ListItemId', N'ListItemCategoryId', N'SystemName', N'ItemName', N'Description', N'DisplayOrder', N'IsVisible', N'CreatedAtUtc') AND [object_id] = OBJECT_ID(N'[dbo].[ListItem]'))
    SET IDENTITY_INSERT [dbo].[ListItem] ON;
INSERT INTO [dbo].[ListItem] ([ListItemId], [ListItemCategoryId], [SystemName], [ItemName], [Description], [DisplayOrder], [IsVisible], [CreatedAtUtc])
VALUES (1, 1, N'DRAFT', N'Draft', NULL, 1, CAST(1 AS bit), '2026-08-16T00:00:00Z'),
(2, 1, N'SUBMITTED_FOR_APPROVAL', N'Submitted For Approval', NULL, 2, CAST(1 AS bit), '2026-08-16T00:00:00Z'),
(3, 1, N'APPROVED', N'Approved', NULL, 3, CAST(1 AS bit), '2026-08-16T00:00:00Z'),
(4, 1, N'REJECTED', N'Rejected', NULL, 4, CAST(1 AS bit), '2026-08-16T00:00:00Z'),
(5, 1, N'DELETED', N'Deleted', NULL, 5, CAST(1 AS bit), '2026-08-16T00:00:00Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ListItemId', N'ListItemCategoryId', N'SystemName', N'ItemName', N'Description', N'DisplayOrder', N'IsVisible', N'CreatedAtUtc') AND [object_id] = OBJECT_ID(N'[dbo].[ListItem]'))
    SET IDENTITY_INSERT [dbo].[ListItem] OFF;

CREATE TABLE [dbo].[PermitApplication] (
    [Id] bigint NOT NULL IDENTITY,
    [PermitNumber] nvarchar(50) NOT NULL,
    [IssueDate] date NOT NULL,
    [PermitIssuer] nvarchar(200) NOT NULL,
    [PermitReceiver] nvarchar(250) NOT NULL,
    [LocationOfWork] nvarchar(500) NOT NULL,
    [DescriptionOfWork] nvarchar(max) NOT NULL,
    [SpecialInstructions] nvarchar(max) NULL,
    [PlannedStartDateTime] datetime2(0) NOT NULL,
    [PlannedEndDateTime] datetime2(0) NOT NULL,
    [PermitTypeListItemId] int NOT NULL,
    [PermitStatusListItemId] int NOT NULL,
    [SubmittedAtUtc] datetime2(0) NULL,
    [CreatedByUserId] int NULL,
    [UpdatedByUserId] int NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL,
    [UpdatedAtUtc] datetime2(0) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_PermitApplication] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PermitApplication_ListItem_PermitStatusListItemId] FOREIGN KEY ([PermitStatusListItemId]) REFERENCES [dbo].[ListItem] ([ListItemId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PermitApplication_ListItem_PermitTypeListItemId] FOREIGN KEY ([PermitTypeListItemId]) REFERENCES [dbo].[ListItem] ([ListItemId]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AuditLog_ChangedByUserId_ChangedAtUtc] ON [dbo].[AuditLog] ([ChangedByUserId], [ChangedAtUtc]);

CREATE INDEX [IX_AuditLog_EntityName_ChangedAtUtc] ON [dbo].[AuditLog] ([EntityName], [ChangedAtUtc]);

CREATE UNIQUE INDEX [IX_ListItem_ListItemCategoryId_Code] ON [dbo].[ListItem] ([ListItemCategoryId], [SystemName]);

CREATE INDEX [IX_ListItem_ListItemCategoryId_IsActive_DisplayOrder] ON [dbo].[ListItem] ([ListItemCategoryId], [IsVisible], [DisplayOrder]);

CREATE UNIQUE INDEX [IX_ListItemCategory_Code] ON [dbo].[ListItemCategory] ([Code]);

CREATE UNIQUE INDEX [IX_PermitApplication_PermitNumber] ON [dbo].[PermitApplication] ([PermitNumber]);

CREATE INDEX [IX_PermitApplication_PermitStatusListItemId_CreatedAtUtc] ON [dbo].[PermitApplication] ([PermitStatusListItemId], [CreatedAtUtc]);

CREATE INDEX [IX_PermitApplication_PermitTypeListItemId] ON [dbo].[PermitApplication] ([PermitTypeListItemId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260816175221_AddPermitApplicationsListItemsAndAudit', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[RiskAssessment] (
    [Id] int NOT NULL IDENTITY,
    [PreRiskAssessmentNumber] nvarchar(50) NOT NULL,
    [IssueDate] date NOT NULL,
    [PermitIssuerName] nvarchar(100) NOT NULL,
    [PermitIssuerContact] nvarchar(30) NULL,
    [PermitReceiverName] nvarchar(100) NOT NULL,
    [PermitReceiverContact] nvarchar(30) NULL,
    [AreaResponsibleName] nvarchar(100) NOT NULL,
    [AreaResponsibleContact] nvarchar(30) NULL,
    [LocationOfWork] nvarchar(255) NOT NULL,
    [DescriptionOfWork] nvarchar(max) NULL,
    [SpecialInstructions] nvarchar(max) NULL,
    [PlannedStartDateTime] datetime2(0) NULL,
    [PlannedEndDateTime] datetime2(0) NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_RiskAssessment] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818113425_AddRiskAssessment', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[RiskAssessmentHazardCategories] (
    [RiskAssessmentId] int NOT NULL,
    [HazardCategoriesListItemId] int NOT NULL,
    [IsSelected] bit NULL,
    CONSTRAINT [PK_RiskAssessmentHazardCategories] PRIMARY KEY ([RiskAssessmentId], [HazardCategoriesListItemId]),
    CONSTRAINT [FK_RiskAssessmentHazardCategories_ListItem_HazardCategoriesListItemId] FOREIGN KEY ([HazardCategoriesListItemId]) REFERENCES [dbo].[ListItem] ([ListItemId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RiskAssessmentHazardCategories_RiskAssessment_RiskAssessmentId] FOREIGN KEY ([RiskAssessmentId]) REFERENCES [dbo].[RiskAssessment] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_RiskAssessmentHazardCategories_HazardCategoriesListItemId] ON [dbo].[RiskAssessmentHazardCategories] ([HazardCategoriesListItemId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818114014_AddRiskAssessmentHazardCategories', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[RiskAssessmentAdditionalPPE] (
    [RiskAssessmentId] int NOT NULL,
    [AdditionalProtectiveMeasuresListItemId] int NOT NULL,
    [IsSelected] bit NULL,
    CONSTRAINT [PK_RiskAssessmentAdditionalPPE] PRIMARY KEY ([RiskAssessmentId], [AdditionalProtectiveMeasuresListItemId]),
    CONSTRAINT [FK_RiskAssessmentAdditionalPPE_ListItem_AdditionalProtectiveMeasuresListItemId] FOREIGN KEY ([AdditionalProtectiveMeasuresListItemId]) REFERENCES [dbo].[ListItem] ([ListItemId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RiskAssessmentAdditionalPPE_RiskAssessment_RiskAssessmentId] FOREIGN KEY ([RiskAssessmentId]) REFERENCES [dbo].[RiskAssessment] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[RiskAssessmentPPE] (
    [RiskAssessmentId] int NOT NULL,
    [SpecialPermitListItemId] int NOT NULL,
    [IsSelected] bit NULL,
    CONSTRAINT [PK_RiskAssessmentPPE] PRIMARY KEY ([RiskAssessmentId], [SpecialPermitListItemId]),
    CONSTRAINT [FK_RiskAssessmentPPE_ListItem_SpecialPermitListItemId] FOREIGN KEY ([SpecialPermitListItemId]) REFERENCES [dbo].[ListItem] ([ListItemId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RiskAssessmentPPE_RiskAssessment_RiskAssessmentId] FOREIGN KEY ([RiskAssessmentId]) REFERENCES [dbo].[RiskAssessment] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[RiskAssessmentSpecialPermit] (
    [RiskAssessmentId] int NOT NULL,
    [SpecialPermitListItemId] int NOT NULL,
    [IsSelected] bit NULL,
    CONSTRAINT [PK_RiskAssessmentSpecialPermit] PRIMARY KEY ([RiskAssessmentId], [SpecialPermitListItemId]),
    CONSTRAINT [FK_RiskAssessmentSpecialPermit_ListItem_SpecialPermitListItemId] FOREIGN KEY ([SpecialPermitListItemId]) REFERENCES [dbo].[ListItem] ([ListItemId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RiskAssessmentSpecialPermit_RiskAssessment_RiskAssessmentId] FOREIGN KEY ([RiskAssessmentId]) REFERENCES [dbo].[RiskAssessment] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_RiskAssessmentAdditionalPPE_AdditionalProtectiveMeasuresListItemId] ON [dbo].[RiskAssessmentAdditionalPPE] ([AdditionalProtectiveMeasuresListItemId]);

CREATE INDEX [IX_RiskAssessmentPPE_SpecialPermitListItemId] ON [dbo].[RiskAssessmentPPE] ([SpecialPermitListItemId]);

CREATE INDEX [IX_RiskAssessmentSpecialPermit_SpecialPermitListItemId] ON [dbo].[RiskAssessmentSpecialPermit] ([SpecialPermitListItemId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818114700_AddRiskAssessmentPermitAndPpeSelections', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var8 nvarchar(max);
SELECT @var8 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[PermitApplication]') AND [c].[name] = N'PlannedEndDateTime');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[PermitApplication] DROP CONSTRAINT ' + @var8 + ';');
ALTER TABLE [dbo].[PermitApplication] DROP COLUMN [PlannedEndDateTime];

DECLARE @var9 nvarchar(max);
SELECT @var9 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[PermitApplication]') AND [c].[name] = N'PlannedStartDateTime');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[PermitApplication] DROP CONSTRAINT ' + @var9 + ';');
ALTER TABLE [dbo].[PermitApplication] DROP COLUMN [PlannedStartDateTime];

EXEC sp_rename N'[dbo].[PermitApplication].[PermitIssuer]', N'PermitIssuerName', 'COLUMN';

EXEC sp_rename N'[dbo].[PermitApplication].[PermitReceiver]', N'PermitReceiverName', 'COLUMN';

EXEC sp_rename N'[dbo].[PermitApplication].[LocationOfWork]', N'WorkLocation', 'COLUMN';

EXEC sp_rename N'[dbo].[PermitApplication].[DescriptionOfWork]', N'WorkDescription', 'COLUMN';

ALTER TABLE [dbo].[PermitApplication] ADD [PermitIssuerContactNumber] nvarchar(30) NULL;

DECLARE @var10 nvarchar(max);
SELECT @var10 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[PermitApplication]') AND [c].[name] = N'PermitReceiverName');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[PermitApplication] DROP CONSTRAINT ' + @var10 + ';');
ALTER TABLE [dbo].[PermitApplication] ALTER COLUMN [PermitReceiverName] nvarchar(200) NOT NULL;

ALTER TABLE [dbo].[PermitApplication] ADD [PermitReceiverContactNumber] nvarchar(30) NULL;

ALTER TABLE [dbo].[PermitApplication] ADD [PreRiskAssessmentNumber] nvarchar(50) NULL;

ALTER TABLE [dbo].[PermitApplication] ADD [WorkHeightBelowSurface] nvarchar(200) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818171735_UpdatePermitApplicationDetails', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE OR ALTER PROCEDURE [dbo].[SpGetListItemsByCategory]
    @ListItemCategoryId int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        li.[ListItemId],
        li.[ListItemCategoryId],
        li.[SystemName] AS [Code],
        li.[ItemName] AS [Name],
        li.[Description],
        li.[DisplayOrder]
    FROM [dbo].[ListItem] AS li
    INNER JOIN [dbo].[ListItemCategory] AS category
        ON category.[ListItemCategoryId] = li.[ListItemCategoryId]
    WHERE li.[ListItemCategoryId] = @ListItemCategoryId
        AND category.[IsActive] = 1
        AND li.[IsVisible] = 1
    ORDER BY li.[DisplayOrder], li.[ItemName], li.[ListItemId];
END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818174955_AddGetListItemsByCategoryProcedure', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE OR ALTER PROCEDURE [dbo].[SpGetListItemsByCategory]
    @CategoryName nvarchar(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        li.[ListItemId],
        li.[ListItemCategoryId],
        li.[SystemName] AS [Code],
        li.[ItemName] AS [Name],
        li.[Description],
        li.[DisplayOrder]
    FROM [dbo].[ListItem] AS li
    INNER JOIN [dbo].[ListItemCategory] AS category
        ON category.[ListItemCategoryId] = li.[ListItemCategoryId]
    WHERE category.[CategoryName] = @CategoryName
        AND category.[IsActive] = 1
        AND li.[IsVisible] = 1
    ORDER BY li.[DisplayOrder], li.[ItemName], li.[ListItemId];
END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818175354_UseCategoryNameForListItemsProcedure', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Users] ADD [ContactNumber] nvarchar(20) NULL;

ALTER TABLE [Users] ADD [DepartmentId] int NULL;

;WITH NumberedUsers AS (
    SELECT [Id], ROW_NUMBER() OVER (ORDER BY [Id]) AS [RowNumber]
    FROM [dbo].[Users]
    WHERE [ContactNumber] IS NULL
)
UPDATE users
SET [ContactNumber] = CONVERT(nvarchar(20),
    CAST(9800000000 AS bigint) + numbered.[RowNumber])
FROM [dbo].[Users] users
INNER JOIN NumberedUsers numbered ON numbered.[Id] = users.[Id];

WITH Assignments AS (
    SELECT * FROM (VALUES
        (N'DEMO.ADMIN', N'KTM-HO', N'ADMIN'),
        (N'DEMO.ASHA', N'KTM-HO', N'FIN'),
        (N'DEMO.BIBEK', N'KTM-HO', N'IT'),
        (N'DEMO.DEEPA', N'PKR', N'OPS'),
        (N'DEMO.GAURAV', N'PKR', N'CS'),
        (N'DEMO.KABITA', N'BRT', N'OPS'),
        (N'DEMO.NABIN', N'BRT', N'CS'),
        (N'DEMO.PRIYA', N'KTM-HO', N'FIN'),
        (N'DEMO.ROSHAN', N'PKR', N'OPS'),
        (N'DEMO.SUSHMA', N'BRT', N'CS')
    ) valuesTable ([NormalizedUserName], [BranchCode], [DepartmentCode])
)
UPDATE users
SET [DepartmentId] = department.[Id]
FROM [dbo].[Users] users
INNER JOIN Assignments assignment
    ON assignment.[NormalizedUserName] = users.[NormalizedUserName]
INNER JOIN [dbo].[OfficeBranch] branch
    ON branch.[Code] = assignment.[BranchCode]
INNER JOIN [dbo].[Department] department
    ON department.[OfficeBranchId] = branch.[Id]
    AND department.[Code] = assignment.[DepartmentCode]
WHERE users.[DepartmentId] IS NULL;

CREATE INDEX [IX_Users_DepartmentId] ON [Users] ([DepartmentId]);

ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Department_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Department] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818183026_AddUserContactAndDepartment', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [dbo].[RiskAssessment] ADD [CreatedBy] int NULL;

ALTER TABLE [dbo].[RiskAssessment] ADD [ModifiedBy] int NULL;

ALTER TABLE [dbo].[RiskAssessment] ADD [OtherEquipmentsPPE] nvarchar(500) NULL;

ALTER TABLE [dbo].[RiskAssessment] ADD [OtherProtectionMeasures] nvarchar(500) NULL;

ALTER TABLE [dbo].[RiskAssessment] ADD [RiskAssessmentStatusListItemId] int NULL;

DECLARE @CategoryId int;
SELECT @CategoryId = [ListItemCategoryId]
FROM [dbo].[ListItemCategory]
WHERE [Code] = N'RISK_ASSESSMENT_STATUS';

IF @CategoryId IS NULL
BEGIN
    INSERT INTO [dbo].[ListItemCategory]
        ([Code], [CategoryName], [Description], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES
        (N'RISK_ASSESSMENT_STATUS', N'RiskAssessmentStatus',
         N'Workflow statuses for risk assessments.', 1, SYSUTCDATETIME(), NULL);
    SET @CategoryId = CONVERT(int, SCOPE_IDENTITY());
END;

DECLARE @Statuses TABLE
(
    [SystemName] nvarchar(50) NOT NULL,
    [ItemName] nvarchar(100) NOT NULL,
    [DisplayOrder] int NOT NULL
);
INSERT INTO @Statuses ([SystemName], [ItemName], [DisplayOrder])
VALUES
    (N'DRAFT', N'Draft', 1),
    (N'SUBMITTED_FOR_APPROVAL', N'Submitted For Approval', 2),
    (N'APPROVED', N'Approved', 3),
    (N'REJECTED', N'Rejected', 4),
    (N'DELETED', N'Deleted', 5);

INSERT INTO [dbo].[ListItem]
    ([ListItemCategoryId], [SystemName], [ItemName], [Description],
     [DisplayOrder], [IsVisible], [CreatedAtUtc], [UpdatedAtUtc])
SELECT @CategoryId, source.[SystemName], source.[ItemName], NULL,
       source.[DisplayOrder], 1, SYSUTCDATETIME(), NULL
FROM @Statuses AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[ListItem] AS target
    WHERE target.[ListItemCategoryId] = @CategoryId
      AND target.[SystemName] = source.[SystemName]
);

DECLARE @DraftStatusId int;
SELECT @DraftStatusId = [ListItemId]
FROM [dbo].[ListItem]
WHERE [ListItemCategoryId] = @CategoryId AND [SystemName] = N'DRAFT';

UPDATE [dbo].[RiskAssessment]
SET [RiskAssessmentStatusListItemId] = @DraftStatusId
WHERE [RiskAssessmentStatusListItemId] IS NULL;

DECLARE @var11 nvarchar(max);
SELECT @var11 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[RiskAssessment]') AND [c].[name] = N'RiskAssessmentStatusListItemId');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[RiskAssessment] DROP CONSTRAINT ' + @var11 + ';');
ALTER TABLE [dbo].[RiskAssessment] ALTER COLUMN [RiskAssessmentStatusListItemId] int NOT NULL;

CREATE INDEX [IX_RiskAssessment_CreatedBy] ON [dbo].[RiskAssessment] ([CreatedBy]);

CREATE INDEX [IX_RiskAssessment_ModifiedBy] ON [dbo].[RiskAssessment] ([ModifiedBy]);

CREATE INDEX [IX_RiskAssessment_RiskAssessmentStatusListItemId_CreatedAtUtc] ON [dbo].[RiskAssessment] ([RiskAssessmentStatusListItemId], [CreatedAtUtc]);

ALTER TABLE [dbo].[RiskAssessment] ADD CONSTRAINT [FK_RiskAssessment_ListItem_RiskAssessmentStatusListItemId] FOREIGN KEY ([RiskAssessmentStatusListItemId]) REFERENCES [dbo].[ListItem] ([ListItemId]) ON DELETE NO ACTION;

ALTER TABLE [dbo].[RiskAssessment] ADD CONSTRAINT [FK_RiskAssessment_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [dbo].[RiskAssessment] ADD CONSTRAINT [FK_RiskAssessment_Users_ModifiedBy] FOREIGN KEY ([ModifiedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

CREATE TYPE [dbo].[RiskAssessmentSelectionTableType] AS TABLE
(
    [ListItemId] int NOT NULL,
    [IsSelected] bit NOT NULL
);

CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentIns]
    @PreRiskAssessmentNumber nvarchar(50),
    @IssueDate date,
    @PermitIssuerName nvarchar(100),
    @PermitIssuerContact nvarchar(30) = NULL,
    @PermitReceiverName nvarchar(100),
    @PermitReceiverContact nvarchar(30) = NULL,
    @AreaResponsibleName nvarchar(100),
    @AreaResponsibleContact nvarchar(30) = NULL,
    @LocationOfWork nvarchar(255),
    @DescriptionOfWork nvarchar(max) = NULL,
    @SpecialInstructions nvarchar(max) = NULL,
    @OtherEquipmentsPPE nvarchar(500) = NULL,
    @OtherProtectionMeasures nvarchar(500) = NULL,
    @PlannedStartDateTime datetime2(0) = NULL,
    @PlannedEndDateTime datetime2(0) = NULL,
    @CreatedBy int,
    @AdditionalPpe [dbo].[RiskAssessmentSelectionTableType] READONLY,
    @HazardCategories [dbo].[RiskAssessmentSelectionTableType] READONLY,
    @PersonalProtectiveEquipment [dbo].[RiskAssessmentSelectionTableType] READONLY,
    @SpecialPermits [dbo].[RiskAssessmentSelectionTableType] READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @DraftStatusId int;
    DECLARE @RiskAssessmentId int;
    DECLARE @Now datetime2(0) = SYSUTCDATETIME();

    SELECT @DraftStatusId = item.[ListItemId]
    FROM [dbo].[ListItem] AS item
    INNER JOIN [dbo].[ListItemCategory] AS category
        ON category.[ListItemCategoryId] = item.[ListItemCategoryId]
    WHERE category.[Code] = N'RISK_ASSESSMENT_STATUS'
      AND item.[SystemName] = N'DRAFT';

    IF @DraftStatusId IS NULL
        THROW 50003, 'The Draft risk assessment status is not configured.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[RiskAssessment]
        (
            [PreRiskAssessmentNumber], [IssueDate], [PermitIssuerName], [PermitIssuerContact],
            [PermitReceiverName], [PermitReceiverContact], [AreaResponsibleName],
            [AreaResponsibleContact], [LocationOfWork], [DescriptionOfWork],
            [SpecialInstructions], [OtherEquipmentsPPE], [OtherProtectionMeasures],
            [PlannedStartDateTime], [PlannedEndDateTime],
            [RiskAssessmentStatusListItemId], [CreatedBy], [ModifiedBy],
            [CreatedAtUtc], [UpdatedAtUtc]
        )
        VALUES
        (
            @PreRiskAssessmentNumber, @IssueDate, @PermitIssuerName, @PermitIssuerContact,
            @PermitReceiverName, @PermitReceiverContact, @AreaResponsibleName,
            @AreaResponsibleContact, @LocationOfWork, @DescriptionOfWork,
            @SpecialInstructions, @OtherEquipmentsPPE, @OtherProtectionMeasures,
            @PlannedStartDateTime, @PlannedEndDateTime,
            @DraftStatusId, @CreatedBy, NULL, @Now, @Now
        );

        SET @RiskAssessmentId = CONVERT(int, SCOPE_IDENTITY());

        INSERT INTO [dbo].[RiskAssessmentAdditionalPPE]
            ([RiskAssessmentId], [AdditionalProtectiveMeasuresListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @AdditionalPpe;

        INSERT INTO [dbo].[RiskAssessmentHazardCategories]
            ([RiskAssessmentId], [HazardCategoriesListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @HazardCategories;

        INSERT INTO [dbo].[RiskAssessmentPPE]
            ([RiskAssessmentId], [SpecialPermitListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @PersonalProtectiveEquipment;

        INSERT INTO [dbo].[RiskAssessmentSpecialPermit]
            ([RiskAssessmentId], [SpecialPermitListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @SpecialPermits;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT @RiskAssessmentId AS [RiskAssessmentId],
           @DraftStatusId AS [RiskAssessmentStatusListItemId],
           N'Draft' AS [Status], @Now AS [UpdatedAtUtc];
END;

CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentUpd]
    @RiskAssessmentId int,
    @PreRiskAssessmentNumber nvarchar(50),
    @IssueDate date,
    @PermitIssuerName nvarchar(100),
    @PermitIssuerContact nvarchar(30) = NULL,
    @PermitReceiverName nvarchar(100),
    @PermitReceiverContact nvarchar(30) = NULL,
    @AreaResponsibleName nvarchar(100),
    @AreaResponsibleContact nvarchar(30) = NULL,
    @LocationOfWork nvarchar(255),
    @DescriptionOfWork nvarchar(max) = NULL,
    @SpecialInstructions nvarchar(max) = NULL,
    @OtherEquipmentsPPE nvarchar(500) = NULL,
    @OtherProtectionMeasures nvarchar(500) = NULL,
    @PlannedStartDateTime datetime2(0) = NULL,
    @PlannedEndDateTime datetime2(0) = NULL,
    @ModifiedBy int,
    @AdditionalPpe [dbo].[RiskAssessmentSelectionTableType] READONLY,
    @HazardCategories [dbo].[RiskAssessmentSelectionTableType] READONLY,
    @PersonalProtectiveEquipment [dbo].[RiskAssessmentSelectionTableType] READONLY,
    @SpecialPermits [dbo].[RiskAssessmentSelectionTableType] READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CurrentStatusId int;
    DECLARE @Now datetime2(0) = SYSUTCDATETIME();

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @CurrentStatusId = [RiskAssessmentStatusListItemId]
        FROM [dbo].[RiskAssessment] WITH (UPDLOCK, HOLDLOCK)
        WHERE [Id] = @RiskAssessmentId;

        IF @CurrentStatusId IS NULL
            THROW 50001, 'Risk assessment was not found.', 1;

        IF NOT EXISTS
        (
            SELECT 1
            FROM [dbo].[ListItem] AS item
            INNER JOIN [dbo].[ListItemCategory] AS category
                ON category.[ListItemCategoryId] = item.[ListItemCategoryId]
            WHERE item.[ListItemId] = @CurrentStatusId
              AND category.[Code] = N'RISK_ASSESSMENT_STATUS'
              AND item.[SystemName] = N'DRAFT'
        )
            THROW 50002, 'Only Draft risk assessments can be updated.', 1;

        UPDATE [dbo].[RiskAssessment]
        SET [PreRiskAssessmentNumber] = @PreRiskAssessmentNumber,
            [IssueDate] = @IssueDate,
            [PermitIssuerName] = @PermitIssuerName,
            [PermitIssuerContact] = @PermitIssuerContact,
            [PermitReceiverName] = @PermitReceiverName,
            [PermitReceiverContact] = @PermitReceiverContact,
            [AreaResponsibleName] = @AreaResponsibleName,
            [AreaResponsibleContact] = @AreaResponsibleContact,
            [LocationOfWork] = @LocationOfWork,
            [DescriptionOfWork] = @DescriptionOfWork,
            [SpecialInstructions] = @SpecialInstructions,
            [OtherEquipmentsPPE] = @OtherEquipmentsPPE,
            [OtherProtectionMeasures] = @OtherProtectionMeasures,
            [PlannedStartDateTime] = @PlannedStartDateTime,
            [PlannedEndDateTime] = @PlannedEndDateTime,
            [ModifiedBy] = @ModifiedBy,
            [UpdatedAtUtc] = @Now
        WHERE [Id] = @RiskAssessmentId;

        DELETE FROM [dbo].[RiskAssessmentAdditionalPPE] WHERE [RiskAssessmentId] = @RiskAssessmentId;
        DELETE FROM [dbo].[RiskAssessmentHazardCategories] WHERE [RiskAssessmentId] = @RiskAssessmentId;
        DELETE FROM [dbo].[RiskAssessmentPPE] WHERE [RiskAssessmentId] = @RiskAssessmentId;
        DELETE FROM [dbo].[RiskAssessmentSpecialPermit] WHERE [RiskAssessmentId] = @RiskAssessmentId;

        INSERT INTO [dbo].[RiskAssessmentAdditionalPPE]
            ([RiskAssessmentId], [AdditionalProtectiveMeasuresListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @AdditionalPpe;
        INSERT INTO [dbo].[RiskAssessmentHazardCategories]
            ([RiskAssessmentId], [HazardCategoriesListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @HazardCategories;
        INSERT INTO [dbo].[RiskAssessmentPPE]
            ([RiskAssessmentId], [SpecialPermitListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @PersonalProtectiveEquipment;
        INSERT INTO [dbo].[RiskAssessmentSpecialPermit]
            ([RiskAssessmentId], [SpecialPermitListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @SpecialPermits;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT @RiskAssessmentId AS [RiskAssessmentId],
           @CurrentStatusId AS [RiskAssessmentStatusListItemId],
           N'Draft' AS [Status], @Now AS [UpdatedAtUtc];
END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819010000_AddRiskAssessmentWorkflowAndProcedures', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentIns]
    @PreRiskAssessmentNumber nvarchar(50),
    @IssueDate date,
    @PermitIssuerName nvarchar(100),
    @PermitReceiverName nvarchar(100),
    @AreaResponsibleName nvarchar(100),
    @LocationOfWork nvarchar(255),
    @DescriptionOfWork nvarchar(max) = NULL,
    @SpecialInstructions nvarchar(max) = NULL,
    @OtherEquipmentsPPE nvarchar(500) = NULL,
    @OtherProtectionMeasures nvarchar(500) = NULL,
    @PlannedStartDateTime datetime2(0) = NULL,
    @PlannedEndDateTime datetime2(0) = NULL,
    @CreatedBy int,
    @AdditionalPpe [dbo].[RiskAssessmentSelectionTableType] READONLY,
    @HazardCategories [dbo].[RiskAssessmentSelectionTableType] READONLY,
    @PersonalProtectiveEquipment [dbo].[RiskAssessmentSelectionTableType] READONLY,
    @SpecialPermits [dbo].[RiskAssessmentSelectionTableType] READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @DraftStatusId int;
    DECLARE @RiskAssessmentId int;
    DECLARE @Now datetime2(0) = SYSUTCDATETIME();

    SELECT @DraftStatusId = item.[ListItemId]
    FROM [dbo].[ListItem] AS item
    INNER JOIN [dbo].[ListItemCategory] AS category
        ON category.[ListItemCategoryId] = item.[ListItemCategoryId]
    WHERE category.[Code] = N'RISK_ASSESSMENT_STATUS'
      AND item.[SystemName] = N'DRAFT';

    IF @DraftStatusId IS NULL
        THROW 50003, 'The Draft risk assessment status is not configured.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[RiskAssessment]
        (
            [PreRiskAssessmentNumber], [IssueDate], [PermitIssuerName],
            [PermitReceiverName], [AreaResponsibleName],
            [LocationOfWork], [DescriptionOfWork], [SpecialInstructions],
            [OtherEquipmentsPPE], [OtherProtectionMeasures],
            [PlannedStartDateTime], [PlannedEndDateTime],
            [RiskAssessmentStatusListItemId], [CreatedBy], [ModifiedBy],
            [CreatedAtUtc], [UpdatedAtUtc]
        )
        VALUES
        (
            @PreRiskAssessmentNumber, @IssueDate, @PermitIssuerName,
            @PermitReceiverName, @AreaResponsibleName,
            @LocationOfWork, @DescriptionOfWork, @SpecialInstructions,
            @OtherEquipmentsPPE, @OtherProtectionMeasures,
            @PlannedStartDateTime, @PlannedEndDateTime,
            @DraftStatusId, @CreatedBy, NULL, @Now, @Now
        );

        SET @RiskAssessmentId = CONVERT(int, SCOPE_IDENTITY());

        INSERT INTO [dbo].[RiskAssessmentAdditionalPPE]
            ([RiskAssessmentId], [AdditionalProtectiveMeasuresListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @AdditionalPpe;
        INSERT INTO [dbo].[RiskAssessmentHazardCategories]
            ([RiskAssessmentId], [HazardCategoriesListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @HazardCategories;
        INSERT INTO [dbo].[RiskAssessmentPPE]
            ([RiskAssessmentId], [SpecialPermitListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @PersonalProtectiveEquipment;
        INSERT INTO [dbo].[RiskAssessmentSpecialPermit]
            ([RiskAssessmentId], [SpecialPermitListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @SpecialPermits;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT @RiskAssessmentId AS [RiskAssessmentId],
           @DraftStatusId AS [RiskAssessmentStatusListItemId],
           N'Draft' AS [Status], @Now AS [UpdatedAtUtc];
END;

CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentUpd]
    @RiskAssessmentId int,
    @PreRiskAssessmentNumber nvarchar(50),
    @IssueDate date,
    @PermitIssuerName nvarchar(100),
    @PermitReceiverName nvarchar(100),
    @AreaResponsibleName nvarchar(100),
    @LocationOfWork nvarchar(255),
    @DescriptionOfWork nvarchar(max) = NULL,
    @SpecialInstructions nvarchar(max) = NULL,
    @OtherEquipmentsPPE nvarchar(500) = NULL,
    @OtherProtectionMeasures nvarchar(500) = NULL,
    @PlannedStartDateTime datetime2(0) = NULL,
    @PlannedEndDateTime datetime2(0) = NULL,
    @ModifiedBy int,
    @AdditionalPpe [dbo].[RiskAssessmentSelectionTableType] READONLY,
    @HazardCategories [dbo].[RiskAssessmentSelectionTableType] READONLY,
    @PersonalProtectiveEquipment [dbo].[RiskAssessmentSelectionTableType] READONLY,
    @SpecialPermits [dbo].[RiskAssessmentSelectionTableType] READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CurrentStatusId int;
    DECLARE @Now datetime2(0) = SYSUTCDATETIME();

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @CurrentStatusId = [RiskAssessmentStatusListItemId]
        FROM [dbo].[RiskAssessment] WITH (UPDLOCK, HOLDLOCK)
        WHERE [Id] = @RiskAssessmentId;

        IF @CurrentStatusId IS NULL
            THROW 50001, 'Risk assessment was not found.', 1;

        IF NOT EXISTS
        (
            SELECT 1
            FROM [dbo].[ListItem] AS item
            INNER JOIN [dbo].[ListItemCategory] AS category
                ON category.[ListItemCategoryId] = item.[ListItemCategoryId]
            WHERE item.[ListItemId] = @CurrentStatusId
              AND category.[Code] = N'RISK_ASSESSMENT_STATUS'
              AND item.[SystemName] = N'DRAFT'
        )
            THROW 50002, 'Only Draft risk assessments can be updated.', 1;

        UPDATE [dbo].[RiskAssessment]
        SET [PreRiskAssessmentNumber] = @PreRiskAssessmentNumber,
            [IssueDate] = @IssueDate,
            [PermitIssuerName] = @PermitIssuerName,
            [PermitReceiverName] = @PermitReceiverName,
            [AreaResponsibleName] = @AreaResponsibleName,
            [LocationOfWork] = @LocationOfWork,
            [DescriptionOfWork] = @DescriptionOfWork,
            [SpecialInstructions] = @SpecialInstructions,
            [OtherEquipmentsPPE] = @OtherEquipmentsPPE,
            [OtherProtectionMeasures] = @OtherProtectionMeasures,
            [PlannedStartDateTime] = @PlannedStartDateTime,
            [PlannedEndDateTime] = @PlannedEndDateTime,
            [ModifiedBy] = @ModifiedBy,
            [UpdatedAtUtc] = @Now
        WHERE [Id] = @RiskAssessmentId;

        DELETE FROM [dbo].[RiskAssessmentAdditionalPPE] WHERE [RiskAssessmentId] = @RiskAssessmentId;
        DELETE FROM [dbo].[RiskAssessmentHazardCategories] WHERE [RiskAssessmentId] = @RiskAssessmentId;
        DELETE FROM [dbo].[RiskAssessmentPPE] WHERE [RiskAssessmentId] = @RiskAssessmentId;
        DELETE FROM [dbo].[RiskAssessmentSpecialPermit] WHERE [RiskAssessmentId] = @RiskAssessmentId;

        INSERT INTO [dbo].[RiskAssessmentAdditionalPPE]
            ([RiskAssessmentId], [AdditionalProtectiveMeasuresListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @AdditionalPpe;
        INSERT INTO [dbo].[RiskAssessmentHazardCategories]
            ([RiskAssessmentId], [HazardCategoriesListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @HazardCategories;
        INSERT INTO [dbo].[RiskAssessmentPPE]
            ([RiskAssessmentId], [SpecialPermitListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @PersonalProtectiveEquipment;
        INSERT INTO [dbo].[RiskAssessmentSpecialPermit]
            ([RiskAssessmentId], [SpecialPermitListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @SpecialPermits;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT @RiskAssessmentId AS [RiskAssessmentId],
           @CurrentStatusId AS [RiskAssessmentStatusListItemId],
           N'Draft' AS [Status], @Now AS [UpdatedAtUtc];
END;

DECLARE @var12 nvarchar(max);
SELECT @var12 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[RiskAssessment]') AND [c].[name] = N'AreaResponsibleContact');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[RiskAssessment] DROP CONSTRAINT ' + @var12 + ';');
ALTER TABLE [dbo].[RiskAssessment] DROP COLUMN [AreaResponsibleContact];

DECLARE @var13 nvarchar(max);
SELECT @var13 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[RiskAssessment]') AND [c].[name] = N'PermitIssuerContact');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[RiskAssessment] DROP CONSTRAINT ' + @var13 + ';');
ALTER TABLE [dbo].[RiskAssessment] DROP COLUMN [PermitIssuerContact];

DECLARE @var14 nvarchar(max);
SELECT @var14 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[RiskAssessment]') AND [c].[name] = N'PermitReceiverContact');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[RiskAssessment] DROP CONSTRAINT ' + @var14 + ';');
ALTER TABLE [dbo].[RiskAssessment] DROP COLUMN [PermitReceiverContact];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819055319_RemoveRiskAssessmentContacts', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[ApprovalWorkflow] (
    [Id] int NOT NULL IDENTITY,
    [PermitTypeListItemId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedByUserId] int NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL,
    [UpdatedAtUtc] datetime2(0) NULL,
    CONSTRAINT [PK_ApprovalWorkflow] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApprovalWorkflow_ListItem_PermitTypeListItemId] FOREIGN KEY ([PermitTypeListItemId]) REFERENCES [dbo].[ListItem] ([ListItemId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ApprovalWorkflow_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[PermitApproval] (
    [Id] bigint NOT NULL IDENTITY,
    [PermitApplicationId] bigint NOT NULL,
    [LevelNumber] tinyint NOT NULL,
    [PrimaryApproverRoleId] int NOT NULL,
    [AlternateApproverRoleId] int NULL,
    [Status] nvarchar(20) NOT NULL,
    [ActionedByUserId] int NULL,
    [Comments] nvarchar(1000) NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL,
    [ActionedAtUtc] datetime2(0) NULL,
    CONSTRAINT [PK_PermitApproval] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_PermitApproval_LevelNumber] CHECK ([LevelNumber] BETWEEN 1 AND 3),
    CONSTRAINT [FK_PermitApproval_PermitApplication_PermitApplicationId] FOREIGN KEY ([PermitApplicationId]) REFERENCES [dbo].[PermitApplication] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PermitApproval_Role_AlternateApproverRoleId] FOREIGN KEY ([AlternateApproverRoleId]) REFERENCES [dbo].[Role] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PermitApproval_Role_PrimaryApproverRoleId] FOREIGN KEY ([PrimaryApproverRoleId]) REFERENCES [dbo].[Role] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PermitApproval_Users_ActionedByUserId] FOREIGN KEY ([ActionedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[ApprovalWorkflowLevel] (
    [Id] int NOT NULL IDENTITY,
    [ApprovalWorkflowId] int NOT NULL,
    [LevelNumber] tinyint NOT NULL,
    [PrimaryApproverRoleId] int NOT NULL,
    [AlternateApproverRoleId] int NULL,
    CONSTRAINT [PK_ApprovalWorkflowLevel] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_ApprovalWorkflowLevel_LevelNumber] CHECK ([LevelNumber] BETWEEN 1 AND 3),
    CONSTRAINT [FK_ApprovalWorkflowLevel_ApprovalWorkflow_ApprovalWorkflowId] FOREIGN KEY ([ApprovalWorkflowId]) REFERENCES [dbo].[ApprovalWorkflow] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ApprovalWorkflowLevel_Role_AlternateApproverRoleId] FOREIGN KEY ([AlternateApproverRoleId]) REFERENCES [dbo].[Role] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ApprovalWorkflowLevel_Role_PrimaryApproverRoleId] FOREIGN KEY ([PrimaryApproverRoleId]) REFERENCES [dbo].[Role] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[ApprovalNotification] (
    [Id] bigint NOT NULL IDENTITY,
    [PermitApprovalId] bigint NOT NULL,
    [RecipientUserId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [AttemptCount] int NOT NULL,
    [LastError] nvarchar(1000) NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL,
    [SentAtUtc] datetime2(0) NULL,
    [ReadAtUtc] datetime2(0) NULL,
    CONSTRAINT [PK_ApprovalNotification] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApprovalNotification_PermitApproval_PermitApprovalId] FOREIGN KEY ([PermitApprovalId]) REFERENCES [dbo].[PermitApproval] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ApprovalNotification_Users_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_ApprovalNotification_PermitApprovalId_RecipientUserId] ON [dbo].[ApprovalNotification] ([PermitApprovalId], [RecipientUserId]);

CREATE INDEX [IX_ApprovalNotification_RecipientUserId_ReadAtUtc_CreatedAtUtc] ON [dbo].[ApprovalNotification] ([RecipientUserId], [ReadAtUtc], [CreatedAtUtc]);

CREATE INDEX [IX_ApprovalNotification_Status_CreatedAtUtc] ON [dbo].[ApprovalNotification] ([Status], [CreatedAtUtc]);

CREATE INDEX [IX_ApprovalWorkflow_CreatedByUserId] ON [dbo].[ApprovalWorkflow] ([CreatedByUserId]);

CREATE UNIQUE INDEX [IX_ApprovalWorkflow_PermitTypeListItemId] ON [dbo].[ApprovalWorkflow] ([PermitTypeListItemId]);

CREATE INDEX [IX_ApprovalWorkflowLevel_AlternateApproverRoleId] ON [dbo].[ApprovalWorkflowLevel] ([AlternateApproverRoleId]);

CREATE UNIQUE INDEX [IX_ApprovalWorkflowLevel_ApprovalWorkflowId_LevelNumber] ON [dbo].[ApprovalWorkflowLevel] ([ApprovalWorkflowId], [LevelNumber]);

CREATE INDEX [IX_ApprovalWorkflowLevel_PrimaryApproverRoleId] ON [dbo].[ApprovalWorkflowLevel] ([PrimaryApproverRoleId]);

CREATE INDEX [IX_PermitApproval_ActionedByUserId] ON [dbo].[PermitApproval] ([ActionedByUserId]);

CREATE INDEX [IX_PermitApproval_AlternateApproverRoleId] ON [dbo].[PermitApproval] ([AlternateApproverRoleId]);

CREATE UNIQUE INDEX [IX_PermitApproval_PermitApplicationId_LevelNumber] ON [dbo].[PermitApproval] ([PermitApplicationId], [LevelNumber]);

CREATE INDEX [IX_PermitApproval_PrimaryApproverRoleId] ON [dbo].[PermitApproval] ([PrimaryApproverRoleId]);

CREATE INDEX [IX_PermitApproval_Status_AlternateApproverRoleId] ON [dbo].[PermitApproval] ([Status], [AlternateApproverRoleId]);

CREATE INDEX [IX_PermitApproval_Status_PrimaryApproverRoleId] ON [dbo].[PermitApproval] ([Status], [PrimaryApproverRoleId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819082833_AddConfigurablePermitApprovalWorkflow', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [dbo].[PermitApplication] ADD [RiskAssessmentId] int NULL;

CREATE INDEX [IX_PermitApplication_RiskAssessmentId] ON [dbo].[PermitApplication] ([RiskAssessmentId]);

ALTER TABLE [dbo].[PermitApplication] ADD CONSTRAINT [FK_PermitApplication_RiskAssessment_RiskAssessmentId] FOREIGN KEY ([RiskAssessmentId]) REFERENCES [dbo].[RiskAssessment] ([Id]) ON DELETE NO ACTION;

CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentIns]
    @PreRiskAssessmentNumber nvarchar(50),
    @IssueDate date,
    @PermitIssuerName nvarchar(100),
    @PermitReceiverName nvarchar(100),
    @AreaResponsibleName nvarchar(100),
    @LocationOfWork nvarchar(255),
    @DescriptionOfWork nvarchar(max) = NULL,
    @SpecialInstructions nvarchar(max) = NULL,
    @OtherEquipmentsPPE nvarchar(500) = NULL,
    @OtherProtectionMeasures nvarchar(500) = NULL,
    @PlannedStartDateTime datetime2(0) = NULL,
    @PlannedEndDateTime datetime2(0) = NULL,
    @CreatedBy int,
    @AdditionalPpe [dbo].[RiskAssessmentSelectionTableType] READONLY,
    @HazardCategories [dbo].[RiskAssessmentSelectionTableType] READONLY,
    @PersonalProtectiveEquipment [dbo].[RiskAssessmentSelectionTableType] READONLY,
    @SpecialPermits [dbo].[RiskAssessmentSelectionTableType] READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @DraftStatusId int;
    DECLARE @PermitDraftStatusId int;
    DECLARE @RiskAssessmentId int;
    DECLARE @Now datetime2(0) = SYSUTCDATETIME();

    SELECT @DraftStatusId = item.[ListItemId]
    FROM [dbo].[ListItem] AS item
    INNER JOIN [dbo].[ListItemCategory] AS category
        ON category.[ListItemCategoryId] = item.[ListItemCategoryId]
    WHERE category.[Code] = N'RISK_ASSESSMENT_STATUS'
      AND item.[SystemName] = N'DRAFT';

    IF @DraftStatusId IS NULL
        THROW 50003, 'The Draft risk assessment status is not configured.', 1;

    SELECT @PermitDraftStatusId = item.[ListItemId]
FROM [dbo].[ListItem] AS item
INNER JOIN [dbo].[ListItemCategory] AS category
    ON category.[ListItemCategoryId] = item.[ListItemCategoryId]
WHERE category.[Code] = N'PERMIT_STATUS'
  AND item.[SystemName] = N'DRAFT';

IF @PermitDraftStatusId IS NULL
    THROW 50004, 'The Draft permit status is not configured.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[RiskAssessment]
        (
            [PreRiskAssessmentNumber], [IssueDate], [PermitIssuerName],
            [PermitReceiverName], [AreaResponsibleName], [LocationOfWork],
            [DescriptionOfWork], [SpecialInstructions], [OtherEquipmentsPPE],
            [OtherProtectionMeasures], [PlannedStartDateTime], [PlannedEndDateTime],
            [RiskAssessmentStatusListItemId], [CreatedBy], [ModifiedBy],
            [CreatedAtUtc], [UpdatedAtUtc]
        )
        VALUES
        (
            @PreRiskAssessmentNumber, @IssueDate, @PermitIssuerName,
            @PermitReceiverName, @AreaResponsibleName, @LocationOfWork,
            @DescriptionOfWork, @SpecialInstructions, @OtherEquipmentsPPE,
            @OtherProtectionMeasures, @PlannedStartDateTime, @PlannedEndDateTime,
            @DraftStatusId, @CreatedBy, NULL, @Now, @Now
        );

        SET @RiskAssessmentId = CONVERT(int, SCOPE_IDENTITY());

        INSERT INTO [dbo].[RiskAssessmentAdditionalPPE]
            ([RiskAssessmentId], [AdditionalProtectiveMeasuresListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @AdditionalPpe;
        INSERT INTO [dbo].[RiskAssessmentHazardCategories]
            ([RiskAssessmentId], [HazardCategoriesListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @HazardCategories;
        INSERT INTO [dbo].[RiskAssessmentPPE]
            ([RiskAssessmentId], [SpecialPermitListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @PersonalProtectiveEquipment;
        INSERT INTO [dbo].[RiskAssessmentSpecialPermit]
            ([RiskAssessmentId], [SpecialPermitListItemId], [IsSelected])
        SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @SpecialPermits;

        INSERT INTO [dbo].[PermitApplication]
(
    [RiskAssessmentId], [PermitNumber], [IssueDate],
    [PermitIssuerName], [PermitIssuerContactNumber],
    [PermitReceiverName], [PermitReceiverContactNumber],
    [PreRiskAssessmentNumber], [WorkLocation], [WorkDescription],
    [SpecialInstructions], [WorkHeightBelowSurface],
    [PermitTypeListItemId], [PermitStatusListItemId],
    [SubmittedAtUtc], [CreatedByUserId], [UpdatedByUserId],
    [CreatedAtUtc], [UpdatedAtUtc]
)
SELECT
    @RiskAssessmentId,
    CONCAT(N'RA-', @RiskAssessmentId, N'-', specialPermit.[ListItemId]),
    @IssueDate,
    @PermitIssuerName,
    NULL,
    @PermitReceiverName,
    NULL,
    @PreRiskAssessmentNumber,
    @LocationOfWork,
    COALESCE(@DescriptionOfWork, N''),
    @SpecialInstructions,
    NULL,
    specialPermit.[ListItemId],
    @PermitDraftStatusId,
    NULL,
    @CreatedBy,
    NULL,
    @Now,
    NULL
FROM @SpecialPermits AS specialPermit
WHERE specialPermit.[IsSelected] = 1;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT @RiskAssessmentId AS [RiskAssessmentId],
           @DraftStatusId AS [RiskAssessmentStatusListItemId],
           N'Draft' AS [Status], @Now AS [UpdatedAtUtc];
END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819130000_LinkRiskAssessmentsToPermitApplications', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [IX_ApprovalWorkflow_PermitTypeListItemId] ON [dbo].[ApprovalWorkflow];

CREATE UNIQUE INDEX [IX_ApprovalWorkflow_PermitTypeListItemId] ON [dbo].[ApprovalWorkflow] ([PermitTypeListItemId]) WHERE [IsActive] = 1;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819141000_EnforceSingleActiveApprovalWorkflowPerPermitType', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [dbo].[ApprovalWorkflowLevel] DROP CONSTRAINT [CK_ApprovalWorkflowLevel_LevelNumber];

ALTER TABLE [dbo].[PermitApproval] DROP CONSTRAINT [CK_PermitApproval_LevelNumber];

ALTER TABLE [dbo].[ApprovalWorkflowLevel] ADD CONSTRAINT [CK_ApprovalWorkflowLevel_LevelNumber] CHECK ([LevelNumber] BETWEEN 1 AND 5);

ALTER TABLE [dbo].[PermitApproval] ADD CONSTRAINT [CK_PermitApproval_LevelNumber] CHECK ([LevelNumber] BETWEEN 1 AND 5);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819160000_AllowUpToFiveApprovalWorkflowLevels', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[PermitApplicationConfinedSpace] (
    [PermitApplicationId] bigint NOT NULL,
    [WorkingInConfinedSpaceListItemId] int NOT NULL,
    [IsSelected] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_PermitApplicationConfinedSpace] PRIMARY KEY ([PermitApplicationId], [WorkingInConfinedSpaceListItemId]),
    CONSTRAINT [FK_PermitApplicationConfinedSpace_ListItem_WorkingInConfinedSpaceListItemId] FOREIGN KEY ([WorkingInConfinedSpaceListItemId]) REFERENCES [dbo].[ListItem] ([ListItemId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PermitApplicationConfinedSpace_PermitApplication_PermitApplicationId] FOREIGN KEY ([PermitApplicationId]) REFERENCES [dbo].[PermitApplication] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[PermitApplicationInspectionPriorToComm] (
    [PermitApplicationId] bigint NOT NULL,
    [InspectionPriorToCommListItemId] int NOT NULL,
    [IsSelected] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_PermitApplicationInspectionPriorToComm] PRIMARY KEY ([PermitApplicationId], [InspectionPriorToCommListItemId]),
    CONSTRAINT [FK_PermitApplicationInspectionPriorToComm_ListItem_InspectionPriorToCommListItemId] FOREIGN KEY ([InspectionPriorToCommListItemId]) REFERENCES [dbo].[ListItem] ([ListItemId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PermitApplicationInspectionPriorToComm_PermitApplication_PermitApplicationId] FOREIGN KEY ([PermitApplicationId]) REFERENCES [dbo].[PermitApplication] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[PermitApplicationWallWorks] (
    [PermitApplicationId] bigint NOT NULL,
    [WorksonWallListItemId] int NOT NULL,
    [IsSelected] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_PermitApplicationWallWorks] PRIMARY KEY ([PermitApplicationId], [WorksonWallListItemId]),
    CONSTRAINT [FK_PermitApplicationWallWorks_ListItem_WorksonWallListItemId] FOREIGN KEY ([WorksonWallListItemId]) REFERENCES [dbo].[ListItem] ([ListItemId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PermitApplicationWallWorks_PermitApplication_PermitApplicationId] FOREIGN KEY ([PermitApplicationId]) REFERENCES [dbo].[PermitApplication] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_PermitApplicationConfinedSpace_WorkingInConfinedSpaceListItemId] ON [dbo].[PermitApplicationConfinedSpace] ([WorkingInConfinedSpaceListItemId]);

CREATE INDEX [IX_PermitApplicationInspectionPriorToComm_InspectionPriorToCommListItemId] ON [dbo].[PermitApplicationInspectionPriorToComm] ([InspectionPriorToCommListItemId]);

CREATE INDEX [IX_PermitApplicationWallWorks_WorksonWallListItemId] ON [dbo].[PermitApplicationWallWorks] ([WorksonWallListItemId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819203805_AddPermitApplicationExtensionTables', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentGet]
    @PageNumber int = 1,
    @PageSize int = 10,
    @SearchTerm nvarchar(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1
        THROW 50010, 'PageNumber must be greater than zero.', 1;

    IF @PageSize < 1 OR @PageSize > 100
        THROW 50011, 'PageSize must be between 1 and 100.', 1;

    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');

    DECLARE @SearchPattern nvarchar(402) = NULL;
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
    FROM [dbo].[RiskAssessment] AS riskAssessment
    INNER JOIN [dbo].[ListItem] AS statusItem
        ON statusItem.[ListItemId] = riskAssessment.[RiskAssessmentStatusListItemId]
    WHERE @SearchPattern IS NULL
       OR riskAssessment.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
       OR riskAssessment.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
       OR riskAssessment.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
       OR riskAssessment.[AreaResponsibleName] LIKE @SearchPattern ESCAPE N'\'
       OR statusItem.[ItemName] LIKE @SearchPattern ESCAPE N'\';

    SELECT
        riskAssessment.[Id],
        riskAssessment.[PreRiskAssessmentNumber],
        riskAssessment.[IssueDate],
        riskAssessment.[PermitIssuerName],
        riskAssessment.[PermitReceiverName],
        riskAssessment.[AreaResponsibleName],
        riskAssessment.[PlannedStartDateTime],
        riskAssessment.[PlannedEndDateTime],
        riskAssessment.[RiskAssessmentStatusListItemId],
        statusItem.[ItemName] AS [RiskAssessmentStatus]
    FROM [dbo].[RiskAssessment] AS riskAssessment
    INNER JOIN [dbo].[ListItem] AS statusItem
        ON statusItem.[ListItemId] = riskAssessment.[RiskAssessmentStatusListItemId]
    WHERE @SearchPattern IS NULL
       OR riskAssessment.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
       OR riskAssessment.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
       OR riskAssessment.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
       OR riskAssessment.[AreaResponsibleName] LIKE @SearchPattern ESCAPE N'\'
       OR statusItem.[ItemName] LIKE @SearchPattern ESCAPE N'\'
    ORDER BY riskAssessment.[IssueDate] DESC, riskAssessment.[Id] DESC
    OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260820090000_AddRiskAssessmentGetProcedure', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE OR ALTER PROCEDURE [dbo].[SpPermitApplicationsGet]
    @CreatedByUserId int,
    @PageNumber int = 1,
    @PageSize int = 10,
    @SearchTerm nvarchar(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @CreatedByUserId < 1
        THROW 50012, 'CreatedByUserId must be greater than zero.', 1;

    IF @PageNumber < 1
        THROW 50010, 'PageNumber must be greater than zero.', 1;

    IF @PageSize < 1 OR @PageSize > 100
        THROW 50011, 'PageSize must be between 1 and 100.', 1;

    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');

    DECLARE @SearchPattern nvarchar(402) = NULL;
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
    FROM [dbo].[PermitApplication] AS permitApplication
    INNER JOIN [dbo].[ListItem] AS permitType
        ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
    INNER JOIN [dbo].[ListItem] AS permitStatus
        ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
    INNER JOIN [dbo].[Users] AS createdByUser
        ON createdByUser.[Id] = permitApplication.[CreatedByUserId]
    WHERE permitApplication.[CreatedByUserId] = @CreatedByUserId
      AND (@SearchPattern IS NULL
        OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
        OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\'
        OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\');

    SELECT
        permitApplication.[Id],
        permitApplication.[PermitNumber],
        permitApplication.[IssueDate],
        permitApplication.[PermitIssuerName],
        permitApplication.[PermitReceiverName],
        permitApplication.[PermitTypeListItemId],
        permitType.[ItemName] AS [PermitTypeName],
        permitApplication.[PermitStatusListItemId],
        permitStatus.[ItemName] AS [PermitStatusName],
        permitApplication.[SubmittedAtUtc],
        permitApplication.[CreatedByUserId],
        COALESCE(createdByUser.[DisplayName], createdByUser.[UserName]) AS [CreatedByUserName],
        permitApplication.[PreRiskAssessmentNumber],
        permitApplication.[RiskAssessmentId]
    FROM [dbo].[PermitApplication] AS permitApplication
    INNER JOIN [dbo].[ListItem] AS permitType
        ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
    INNER JOIN [dbo].[ListItem] AS permitStatus
        ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
    INNER JOIN [dbo].[Users] AS createdByUser
        ON createdByUser.[Id] = permitApplication.[CreatedByUserId]
    WHERE permitApplication.[CreatedByUserId] = @CreatedByUserId
      AND (@SearchPattern IS NULL
        OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
        OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\'
        OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\')
    ORDER BY permitApplication.[IssueDate] DESC, permitApplication.[Id] DESC
    OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260820100000_AddPermitApplicationsGetProcedure', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [dbo].[PermitApplication] ADD [CompletionOfWorks] nvarchar(500) NULL;

ALTER TABLE [dbo].[PermitApplication] ADD [CompletionApprovedBy] int NULL;

ALTER TABLE [dbo].[PermitApplication] ADD [CompletionDate] datetime2(0) NULL;

ALTER TABLE [dbo].[PermitApplication] ADD [CompletionRemarks] nvarchar(500) NULL;

ALTER TABLE [dbo].[PermitApplication] ADD [CancelledBy] int NULL;

ALTER TABLE [dbo].[PermitApplication] ADD [CancelledDate] datetime2(0) NULL;

ALTER TABLE [dbo].[PermitApplication] ADD [CancelledRemarks] nvarchar(500) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260820110000_AddPermitApplicationCompletionCancellation', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE OR ALTER PROCEDURE [dbo].[SpAuditLogsGet]
    @PageNumber int = 1,
    @PageSize int = 20,
    @SearchTerm nvarchar(200) = NULL,
    @EntityName nvarchar(128) = NULL,
    @Action nvarchar(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1
        THROW 50010, 'PageNumber must be greater than zero.', 1;

    IF @PageSize < 1 OR @PageSize > 100
        THROW 50011, 'PageSize must be between 1 and 100.', 1;

    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
    SET @EntityName = NULLIF(LTRIM(RTRIM(@EntityName)), N'');
    SET @Action = NULLIF(LTRIM(RTRIM(@Action)), N'');

    DECLARE @SearchPattern nvarchar(402) = NULL;
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
    FROM [dbo].[AuditLog] AS auditLog
    LEFT JOIN [dbo].[Users] AS changedByUser
        ON changedByUser.[Id] = auditLog.[ChangedByUserId]
    WHERE (@EntityName IS NULL OR auditLog.[EntityName] = @EntityName)
      AND (@Action IS NULL OR auditLog.[Action] = @Action)
      AND (@SearchPattern IS NULL
        OR auditLog.[EntityName] LIKE @SearchPattern ESCAPE N'\'
        OR auditLog.[Action] LIKE @SearchPattern ESCAPE N'\'
        OR auditLog.[ChangedBy] LIKE @SearchPattern ESCAPE N'\'
        OR changedByUser.[DisplayName] LIKE @SearchPattern ESCAPE N'\'
        OR changedByUser.[UserName] LIKE @SearchPattern ESCAPE N'\'
        OR auditLog.[ChangedColumns] LIKE @SearchPattern ESCAPE N'\');

    ;WITH PagedAuditLogs AS
    (
        SELECT
            auditLog.[Id],
            auditLog.[EntityName],
            auditLog.[Action],
            auditLog.[EntityKey],
            auditLog.[ChangedColumns],
            auditLog.[OldValues],
            auditLog.[NewValues],
            auditLog.[ChangedByUserId],
            COALESCE(changedByUser.[DisplayName], changedByUser.[UserName], auditLog.[ChangedBy])
                AS [ChangedByName],
            auditLog.[TraceId],
            auditLog.[IpAddress],
            auditLog.[ChangedAtUtc]
        FROM [dbo].[AuditLog] AS auditLog
        LEFT JOIN [dbo].[Users] AS changedByUser
            ON changedByUser.[Id] = auditLog.[ChangedByUserId]
        WHERE (@EntityName IS NULL OR auditLog.[EntityName] = @EntityName)
          AND (@Action IS NULL OR auditLog.[Action] = @Action)
          AND (@SearchPattern IS NULL
            OR auditLog.[EntityName] LIKE @SearchPattern ESCAPE N'\'
            OR auditLog.[Action] LIKE @SearchPattern ESCAPE N'\'
            OR auditLog.[ChangedBy] LIKE @SearchPattern ESCAPE N'\'
            OR changedByUser.[DisplayName] LIKE @SearchPattern ESCAPE N'\'
            OR changedByUser.[UserName] LIKE @SearchPattern ESCAPE N'\'
            OR auditLog.[ChangedColumns] LIKE @SearchPattern ESCAPE N'\')
        ORDER BY auditLog.[ChangedAtUtc] DESC, auditLog.[Id] DESC
        OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
        FETCH NEXT @PageSize ROWS ONLY
    )
    SELECT
        paged.[Id],
        paged.[EntityName],
        COALESCE(
            entityPermit.[PermitNumber],
            entityRiskAssessment.[PreRiskAssessmentNumber],
            entityWorkflow.[Name],
            entityRole.[Name],
            entityListItem.[ItemName],
            entityDepartment.[Name],
            entityBranch.[Name],
            entityModule.[Name],
            entityMenu.[Name],
            COALESCE(entityUser.[DisplayName], entityUser.[UserName]),
            relatedPermit.[PermitNumber],
            relatedWorkflow.[Name],
            COALESCE(relatedUser.[DisplayName], relatedUser.[UserName]),
            CONCAT(paged.[EntityName],
                CASE WHEN keys.[EntityId] IS NULL THEN N''
                     ELSE CONCAT(N' #', keys.[EntityId]) END)) AS [EntityDisplayName],
        paged.[Action],
        paged.[EntityKey],
        paged.[ChangedColumns],
        paged.[OldValues],
        paged.[NewValues],
        JSON_QUERY((
            SELECT
                COALESCE(relatedUser.[DisplayName], relatedUser.[UserName]) AS [UserId],
                COALESCE(createdBy.[DisplayName], createdBy.[UserName]) AS [CreatedByUserId],
                COALESCE(updatedBy.[DisplayName], updatedBy.[UserName]) AS [UpdatedByUserId],
                COALESCE(actionedBy.[DisplayName], actionedBy.[UserName]) AS [ActionedByUserId],
                COALESCE(completionBy.[DisplayName], completionBy.[UserName]) AS [CompletionApprovedBy],
                COALESCE(cancelledBy.[DisplayName], cancelledBy.[UserName]) AS [CancelledBy],
                permitType.[ItemName] AS [PermitTypeListItemId],
                permitStatus.[ItemName] AS [PermitStatusListItemId],
                riskStatus.[ItemName] AS [RiskAssessmentStatusListItemId],
                primaryRole.[Name] AS [PrimaryApproverRoleId],
                alternateRole.[Name] AS [AlternateApproverRoleId],
                relatedWorkflow.[Name] AS [ApprovalWorkflowId],
                relatedPermit.[PermitNumber] AS [PermitApplicationId],
                relatedRiskAssessment.[PreRiskAssessmentNumber] AS [RiskAssessmentId],
                relatedDepartment.[Name] AS [DepartmentId],
                relatedBranch.[Name] AS [OfficeBranchId],
                relatedModule.[Name] AS [ApplicationModuleId],
                relatedMenu.[Name] AS [ParentMenuId]
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        )) AS [RelatedNames],
        paged.[ChangedByUserId],
        paged.[ChangedByName],
        paged.[TraceId],
        paged.[IpAddress],
        paged.[ChangedAtUtc]
    FROM PagedAuditLogs AS paged
    OUTER APPLY
    (
        SELECT
            TRY_CONVERT(bigint, JSON_VALUE(paged.[EntityKey], '$.Id')) AS [EntityId],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.UserId')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.UserId'))) AS [UserId],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.CreatedByUserId')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.CreatedByUserId'))) AS [CreatedByUserId],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.UpdatedByUserId')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.UpdatedByUserId'))) AS [UpdatedByUserId],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.ActionedByUserId')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.ActionedByUserId'))) AS [ActionedByUserId],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.CompletionApprovedBy')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.CompletionApprovedBy'))) AS [CompletionApprovedBy],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.CancelledBy')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.CancelledBy'))) AS [CancelledBy],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.PermitTypeListItemId')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.PermitTypeListItemId'))) AS [PermitTypeListItemId],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.PermitStatusListItemId')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.PermitStatusListItemId'))) AS [PermitStatusListItemId],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.RiskAssessmentStatusListItemId')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.RiskAssessmentStatusListItemId'))) AS [RiskAssessmentStatusListItemId],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.PrimaryApproverRoleId')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.PrimaryApproverRoleId'))) AS [PrimaryApproverRoleId],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.AlternateApproverRoleId')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.AlternateApproverRoleId'))) AS [AlternateApproverRoleId],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.ApprovalWorkflowId')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.ApprovalWorkflowId'))) AS [ApprovalWorkflowId],
            COALESCE(
                TRY_CONVERT(bigint, JSON_VALUE(paged.[NewValues], '$.PermitApplicationId')),
                TRY_CONVERT(bigint, JSON_VALUE(paged.[OldValues], '$.PermitApplicationId')),
                TRY_CONVERT(bigint, JSON_VALUE(paged.[EntityKey], '$.PermitApplicationId'))) AS [PermitApplicationId],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.RiskAssessmentId')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.RiskAssessmentId'))) AS [RiskAssessmentId],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.DepartmentId')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.DepartmentId'))) AS [DepartmentId],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.OfficeBranchId')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.OfficeBranchId'))) AS [OfficeBranchId],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.ApplicationModuleId')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.ApplicationModuleId'))) AS [ApplicationModuleId],
            COALESCE(
                TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.ParentMenuId')),
                TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.ParentMenuId'))) AS [ParentMenuId]
    ) AS keys
    LEFT JOIN [dbo].[PermitApplication] AS entityPermit
        ON paged.[EntityName] = N'PermitApplication' AND entityPermit.[Id] = keys.[EntityId]
    LEFT JOIN [dbo].[RiskAssessment] AS entityRiskAssessment
        ON paged.[EntityName] = N'RiskAssessment' AND entityRiskAssessment.[Id] = keys.[EntityId]
    LEFT JOIN [dbo].[ApprovalWorkflow] AS entityWorkflow
        ON paged.[EntityName] = N'ApprovalWorkflow' AND entityWorkflow.[Id] = keys.[EntityId]
    LEFT JOIN [dbo].[Role] AS entityRole
        ON paged.[EntityName] = N'Role' AND entityRole.[Id] = keys.[EntityId]
    LEFT JOIN [dbo].[ListItem] AS entityListItem
        ON paged.[EntityName] = N'ListItem' AND entityListItem.[ListItemId] = keys.[EntityId]
    LEFT JOIN [dbo].[Department] AS entityDepartment
        ON paged.[EntityName] = N'Department' AND entityDepartment.[Id] = keys.[EntityId]
    LEFT JOIN [dbo].[OfficeBranch] AS entityBranch
        ON paged.[EntityName] = N'OfficeBranch' AND entityBranch.[Id] = keys.[EntityId]
    LEFT JOIN [dbo].[ApplicationModule] AS entityModule
        ON paged.[EntityName] = N'ApplicationModule' AND entityModule.[Id] = keys.[EntityId]
    LEFT JOIN [dbo].[ModuleMenu] AS entityMenu
        ON paged.[EntityName] = N'ModuleMenu' AND entityMenu.[Id] = keys.[EntityId]
    LEFT JOIN [dbo].[Users] AS entityUser
        ON paged.[EntityName] = N'Users' AND entityUser.[Id] = keys.[EntityId]
    LEFT JOIN [dbo].[Users] AS relatedUser ON relatedUser.[Id] = keys.[UserId]
    LEFT JOIN [dbo].[Users] AS createdBy ON createdBy.[Id] = keys.[CreatedByUserId]
    LEFT JOIN [dbo].[Users] AS updatedBy ON updatedBy.[Id] = keys.[UpdatedByUserId]
    LEFT JOIN [dbo].[Users] AS actionedBy ON actionedBy.[Id] = keys.[ActionedByUserId]
    LEFT JOIN [dbo].[Users] AS completionBy ON completionBy.[Id] = keys.[CompletionApprovedBy]
    LEFT JOIN [dbo].[Users] AS cancelledBy ON cancelledBy.[Id] = keys.[CancelledBy]
    LEFT JOIN [dbo].[ListItem] AS permitType ON permitType.[ListItemId] = keys.[PermitTypeListItemId]
    LEFT JOIN [dbo].[ListItem] AS permitStatus ON permitStatus.[ListItemId] = keys.[PermitStatusListItemId]
    LEFT JOIN [dbo].[ListItem] AS riskStatus ON riskStatus.[ListItemId] = keys.[RiskAssessmentStatusListItemId]
    LEFT JOIN [dbo].[Role] AS primaryRole ON primaryRole.[Id] = keys.[PrimaryApproverRoleId]
    LEFT JOIN [dbo].[Role] AS alternateRole ON alternateRole.[Id] = keys.[AlternateApproverRoleId]
    LEFT JOIN [dbo].[ApprovalWorkflow] AS relatedWorkflow ON relatedWorkflow.[Id] = keys.[ApprovalWorkflowId]
    LEFT JOIN [dbo].[PermitApplication] AS relatedPermit ON relatedPermit.[Id] = keys.[PermitApplicationId]
    LEFT JOIN [dbo].[RiskAssessment] AS relatedRiskAssessment ON relatedRiskAssessment.[Id] = keys.[RiskAssessmentId]
    LEFT JOIN [dbo].[Department] AS relatedDepartment ON relatedDepartment.[Id] = keys.[DepartmentId]
    LEFT JOIN [dbo].[OfficeBranch] AS relatedBranch ON relatedBranch.[Id] = keys.[OfficeBranchId]
    LEFT JOIN [dbo].[ApplicationModule] AS relatedModule ON relatedModule.[Id] = keys.[ApplicationModuleId]
    LEFT JOIN [dbo].[ModuleMenu] AS relatedMenu ON relatedMenu.[Id] = keys.[ParentMenuId]
    ORDER BY paged.[ChangedAtUtc] DESC, paged.[Id] DESC;
END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260820120000_AddAuditLogsGetProcedure', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @PermitStatusCategoryId int;

SELECT @PermitStatusCategoryId = [ListItemCategoryId]
FROM [dbo].[ListItemCategory]
WHERE [Code] = N'PERMIT_STATUS';

IF @PermitStatusCategoryId IS NULL
    THROW 50010, 'The PERMIT_STATUS list item category is not configured.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM [dbo].[ListItem]
    WHERE [ListItemCategoryId] = @PermitStatusCategoryId
      AND [SystemName] = N'FINALIZED_FOR_APPROVAL'
)
BEGIN
    INSERT INTO [dbo].[ListItem]
    (
        [ListItemCategoryId], [SystemName], [ItemName], [Description],
        [DisplayOrder], [IsVisible], [CreatedAtUtc]
    )
    VALUES
    (
        @PermitStatusCategoryId, N'FINALIZED_FOR_APPROVAL',
        N'Finalized For Approval',
        N'The permit is complete and ready for its risk assessment to be submitted.',
        20, 1, SYSUTCDATETIME()
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM [dbo].[ListItem]
    WHERE [ListItemCategoryId] = @PermitStatusCategoryId
      AND [SystemName] = N'PERMIT_SUBMITTED_FOR_APPROVAL'
)
BEGIN
    INSERT INTO [dbo].[ListItem]
    (
        [ListItemCategoryId], [SystemName], [ItemName], [Description],
        [DisplayOrder], [IsVisible], [CreatedAtUtc]
    )
    VALUES
    (
        @PermitStatusCategoryId, N'PERMIT_SUBMITTED_FOR_APPROVAL',
        N'Permit Submitted For Approval',
        N'The permit approval workflow has started.',
        30, 1, SYSUTCDATETIME()
    );
END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260821100000_AddPermitApprovalSubmissionStatuses', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE OR ALTER PROCEDURE [dbo].[SpPermitApprovalHistoryGet]
    @ActionedByUserId int,
    @ApprovalStatus varchar(20),
    @PageNumber int = 1,
    @PageSize int = 10,
    @SearchTerm nvarchar(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @ActionedByUserId < 1
        THROW 50012, 'ActionedByUserId must be greater than zero.', 1;

    IF @ApprovalStatus NOT IN ('APPROVED', 'REJECTED')
        THROW 50013, 'ApprovalStatus must be APPROVED or REJECTED.', 1;

    IF @PageNumber < 1
        THROW 50010, 'PageNumber must be greater than zero.', 1;

    IF @PageSize < 1 OR @PageSize > 100
        THROW 50011, 'PageSize must be between 1 and 100.', 1;

    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');

    DECLARE @SearchPattern nvarchar(402) = NULL;
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
    FROM [dbo].[PermitApproval] AS permitApproval
    INNER JOIN [dbo].[PermitApplication] AS permitApplication
        ON permitApplication.[Id] = permitApproval.[PermitApplicationId]
    INNER JOIN [dbo].[ListItem] AS permitType
        ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
    INNER JOIN [dbo].[ListItem] AS permitStatus
        ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
    WHERE permitApproval.[ActionedByUserId] = @ActionedByUserId
      AND permitApproval.[Status] = @ApprovalStatus
      AND (@SearchPattern IS NULL
        OR permitApplication.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
        OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\'
        OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\'
        OR permitApproval.[Comments] LIKE @SearchPattern ESCAPE N'\');

    SELECT
        permitApplication.[PreRiskAssessmentNumber],
        permitApplication.[PermitNumber],
        permitApplication.[IssueDate] AS [IssuedDate],
        permitApplication.[PermitIssuerName],
        permitApplication.[PermitReceiverName],
        permitType.[ItemName] AS [PermitType],
        permitStatus.[ItemName] AS [PermitStatus],
        permitApproval.[ActionedAtUtc] AS [DecisionDate],
        permitApproval.[Comments] AS [Remarks]
    FROM [dbo].[PermitApproval] AS permitApproval
    INNER JOIN [dbo].[PermitApplication] AS permitApplication
        ON permitApplication.[Id] = permitApproval.[PermitApplicationId]
    INNER JOIN [dbo].[ListItem] AS permitType
        ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
    INNER JOIN [dbo].[ListItem] AS permitStatus
        ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
    WHERE permitApproval.[ActionedByUserId] = @ActionedByUserId
      AND permitApproval.[Status] = @ApprovalStatus
      AND (@SearchPattern IS NULL
        OR permitApplication.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
        OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\'
        OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\'
        OR permitApproval.[Comments] LIKE @SearchPattern ESCAPE N'\')
    ORDER BY permitApproval.[ActionedAtUtc] DESC, permitApproval.[Id] DESC
    OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260822100000_AddPermitApprovalHistoryGetProcedure', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [dbo].[PermitApproval] ADD [RowVersion] rowversion NOT NULL;

CREATE TABLE [dbo].[PermitApprovalAssignee] (
    [PermitApprovalId] bigint NOT NULL,
    [UserId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [AssignedByUserId] int NOT NULL,
    [AssignedAtUtc] datetime2(0) NOT NULL,
    [RevokedByUserId] int NULL,
    [RevokedAtUtc] datetime2(0) NULL,
    CONSTRAINT [PK_PermitApprovalAssignee] PRIMARY KEY ([PermitApprovalId], [UserId]),
    CONSTRAINT [FK_PermitApprovalAssignee_PermitApproval_PermitApprovalId] FOREIGN KEY ([PermitApprovalId]) REFERENCES [dbo].[PermitApproval] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PermitApprovalAssignee_Users_AssignedByUserId] FOREIGN KEY ([AssignedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PermitApprovalAssignee_Users_RevokedByUserId] FOREIGN KEY ([RevokedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PermitApprovalAssignee_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_PermitApprovalAssignee_AssignedByUserId] ON [dbo].[PermitApprovalAssignee] ([AssignedByUserId]);

CREATE INDEX [IX_PermitApprovalAssignee_RevokedByUserId] ON [dbo].[PermitApprovalAssignee] ([RevokedByUserId]);

CREATE INDEX [IX_PermitApprovalAssignee_UserId_IsActive_PermitApprovalId] ON [dbo].[PermitApprovalAssignee] ([UserId], [IsActive], [PermitApprovalId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260822120000_AddDirectPermitApprovalAssignees', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
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

CREATE OR ALTER PROCEDURE [dbo].[SPOfficeBranchDdl]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Code, Name
    FROM dbo.OfficeBranch
    WHERE IsActive = 1
    ORDER BY IsHeadOffice DESC, Name, Id;
END

CREATE OR ALTER PROCEDURE [dbo].[SPOfficeBranchGetById] @Id int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Code, Name, Address, IsHeadOffice, IsActive
    FROM dbo.OfficeBranch WHERE Id = @Id;
END

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

CREATE OR ALTER PROCEDURE [dbo].[SPDepartmentGetById] @Id int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.Id, d.OfficeBranchId, b.Name AS BranchName, d.Code, d.Name, d.IsActive
    FROM dbo.Department d INNER JOIN dbo.OfficeBranch b ON b.Id = d.OfficeBranchId
    WHERE d.Id = @Id;
END

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

CREATE OR ALTER PROCEDURE [dbo].[SPDepartmentDel] @Id int
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Department WHERE Id = @Id)
    BEGIN SELECT CAST(0 AS bit); RETURN; END
    UPDATE dbo.Department SET IsActive = 0 WHERE Id = @Id;
    SELECT CAST(1 AS bit);
END

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260828100000_AddOrganizationCrudProcedures', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [IX_ModuleMenu_ApplicationModuleId_QueryUrl] ON [dbo].[ModuleMenu];

DECLARE @var15 nvarchar(max);
SELECT @var15 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[ModuleMenu]') AND [c].[name] = N'ControllerName');
IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ModuleMenu] DROP CONSTRAINT ' + @var15 + ';');
ALTER TABLE [dbo].[ModuleMenu] ALTER COLUMN [ControllerName] nvarchar(100) NULL;

DECLARE @var16 nvarchar(max);
SELECT @var16 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[ModuleMenu]') AND [c].[name] = N'ActionName');
IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ModuleMenu] DROP CONSTRAINT ' + @var16 + ';');
ALTER TABLE [dbo].[ModuleMenu] ALTER COLUMN [ActionName] nvarchar(100) NULL;

DECLARE @var17 nvarchar(max);
SELECT @var17 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[ModuleMenu]') AND [c].[name] = N'QueryUrl');
IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ModuleMenu] DROP CONSTRAINT ' + @var17 + ';');
ALTER TABLE [dbo].[ModuleMenu] ALTER COLUMN [QueryUrl] nvarchar(500) NULL;

CREATE UNIQUE INDEX [IX_ModuleMenu_ApplicationModuleId_QueryUrl] ON [dbo].[ModuleMenu] ([ApplicationModuleId], [QueryUrl]) WHERE [QueryUrl] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260828120000_AllowRootModuleMenuNullNavigation', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[UserThemeSetting] (
    [UserId] int NOT NULL,
    [Mode] nvarchar(10) NOT NULL,
    [Color] nvarchar(20) NOT NULL,
    [Radius] int NOT NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL,
    [UpdatedAtUtc] datetime2(0) NOT NULL,
    CONSTRAINT [PK_UserThemeSetting] PRIMARY KEY ([UserId]),
    CONSTRAINT [CK_UserThemeSetting_Color] CHECK ([Color] IN (N'blue', N'azure', N'indigo', N'purple', N'pink', N'red', N'orange', N'green')),
    CONSTRAINT [CK_UserThemeSetting_Mode] CHECK ([Mode] IN (N'light', N'dark', N'system')),
    CONSTRAINT [CK_UserThemeSetting_Radius] CHECK ([Radius] IN (0, 6, 12)),
    CONSTRAINT [FK_UserThemeSetting_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260829100000_AddUserThemeSettings', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE OR ALTER PROCEDURE [dbo].[SpPermitApplicationsGet]
    @CreatedByUserId int,
    @PageNumber int = 1,
    @PageSize int = 10,
    @SearchTerm nvarchar(200) = NULL,
    @SortBy nvarchar(40) = N'issueDate',
    @SortDirection varchar(4) = 'desc'
AS
BEGIN
    SET NOCOUNT ON;

    IF @CreatedByUserId < 1 THROW 50012, 'CreatedByUserId must be greater than zero.', 1;
    IF @PageNumber < 1 THROW 50010, 'PageNumber must be greater than zero.', 1;
    IF @PageSize < 1 OR @PageSize > 100 THROW 50011, 'PageSize must be between 1 and 100.', 1;

    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
    SET @SortBy = NULLIF(LTRIM(RTRIM(@SortBy)), N'');
    SET @SortDirection = LOWER(NULLIF(LTRIM(RTRIM(@SortDirection)), ''));

    IF @SortBy NOT IN (N'preRiskAssessmentNumber', N'permitNumber', N'permitIssuerName',
        N'permitReceiverName', N'permitTypeName', N'permitStatusName', N'submittedAtUtc', N'issueDate')
        THROW 50013, 'SortBy is not supported.', 1;
    IF @SortDirection NOT IN ('asc', 'desc')
        THROW 50014, 'SortDirection must be asc or desc.', 1;

    DECLARE @SearchPattern nvarchar(402) = NULL;
    IF @SearchTerm IS NOT NULL
        SET @SearchPattern = N'%' +
            REPLACE(REPLACE(REPLACE(REPLACE(@SearchTerm, N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[') + N'%';

    SELECT COUNT_BIG(1) AS [TotalRecords]
    FROM [dbo].[PermitApplication] AS permitApplication
    INNER JOIN [dbo].[ListItem] AS permitType ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
    INNER JOIN [dbo].[ListItem] AS permitStatus ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
    INNER JOIN [dbo].[Users] AS createdByUser ON createdByUser.[Id] = permitApplication.[CreatedByUserId]
    WHERE permitApplication.[CreatedByUserId] = @CreatedByUserId
      AND (@SearchPattern IS NULL
        OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
        OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\'
        OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\');

    SELECT
        permitApplication.[Id], permitApplication.[PermitNumber], permitApplication.[IssueDate],
        permitApplication.[PermitIssuerName], permitApplication.[PermitReceiverName],
        permitApplication.[PermitTypeListItemId], permitType.[ItemName] AS [PermitTypeName],
        permitApplication.[PermitStatusListItemId], permitStatus.[ItemName] AS [PermitStatusName],
        permitApplication.[SubmittedAtUtc], permitApplication.[CreatedByUserId],
        COALESCE(createdByUser.[DisplayName], createdByUser.[UserName]) AS [CreatedByUserName],
        permitApplication.[PreRiskAssessmentNumber], permitApplication.[RiskAssessmentId]
    FROM [dbo].[PermitApplication] AS permitApplication
    INNER JOIN [dbo].[ListItem] AS permitType ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
    INNER JOIN [dbo].[ListItem] AS permitStatus ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
    INNER JOIN [dbo].[Users] AS createdByUser ON createdByUser.[Id] = permitApplication.[CreatedByUserId]
    WHERE permitApplication.[CreatedByUserId] = @CreatedByUserId
      AND (@SearchPattern IS NULL
        OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
        OR permitApplication.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
        OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\'
        OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\')
    ORDER BY
        CASE WHEN @SortBy = N'preRiskAssessmentNumber' AND @SortDirection = 'asc' THEN permitApplication.[PreRiskAssessmentNumber] END ASC,
        CASE WHEN @SortBy = N'preRiskAssessmentNumber' AND @SortDirection = 'desc' THEN permitApplication.[PreRiskAssessmentNumber] END DESC,
        CASE WHEN @SortBy = N'permitNumber' AND @SortDirection = 'asc' THEN permitApplication.[PermitNumber] END ASC,
        CASE WHEN @SortBy = N'permitNumber' AND @SortDirection = 'desc' THEN permitApplication.[PermitNumber] END DESC,
        CASE WHEN @SortBy = N'permitIssuerName' AND @SortDirection = 'asc' THEN permitApplication.[PermitIssuerName] END ASC,
        CASE WHEN @SortBy = N'permitIssuerName' AND @SortDirection = 'desc' THEN permitApplication.[PermitIssuerName] END DESC,
        CASE WHEN @SortBy = N'permitReceiverName' AND @SortDirection = 'asc' THEN permitApplication.[PermitReceiverName] END ASC,
        CASE WHEN @SortBy = N'permitReceiverName' AND @SortDirection = 'desc' THEN permitApplication.[PermitReceiverName] END DESC,
        CASE WHEN @SortBy = N'permitTypeName' AND @SortDirection = 'asc' THEN permitType.[ItemName] END ASC,
        CASE WHEN @SortBy = N'permitTypeName' AND @SortDirection = 'desc' THEN permitType.[ItemName] END DESC,
        CASE WHEN @SortBy = N'permitStatusName' AND @SortDirection = 'asc' THEN permitStatus.[ItemName] END ASC,
        CASE WHEN @SortBy = N'permitStatusName' AND @SortDirection = 'desc' THEN permitStatus.[ItemName] END DESC,
        CASE WHEN @SortBy = N'submittedAtUtc' AND @SortDirection = 'asc' THEN permitApplication.[SubmittedAtUtc] END ASC,
        CASE WHEN @SortBy = N'submittedAtUtc' AND @SortDirection = 'desc' THEN permitApplication.[SubmittedAtUtc] END DESC,
        CASE WHEN @SortBy = N'issueDate' AND @SortDirection = 'asc' THEN permitApplication.[IssueDate] END ASC,
        CASE WHEN @SortBy = N'issueDate' AND @SortDirection = 'desc' THEN permitApplication.[IssueDate] END DESC,
        permitApplication.[Id] DESC
    OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260829110000_AddPermitApplicationSorting', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentGet]
    @PageNumber int = 1,
    @PageSize int = 10,
    @SearchTerm nvarchar(200) = NULL,
    @SortBy nvarchar(40) = N'issueDate',
    @SortDirection varchar(4) = 'desc'
AS
BEGIN
    SET NOCOUNT ON;
    IF @PageNumber < 1 THROW 50010, 'PageNumber must be greater than zero.', 1;
    IF @PageSize < 1 OR @PageSize > 100 THROW 50011, 'PageSize must be between 1 and 100.', 1;

    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
    SET @SortBy = NULLIF(LTRIM(RTRIM(@SortBy)), N'');
    SET @SortDirection = LOWER(NULLIF(LTRIM(RTRIM(@SortDirection)), ''));
    IF @SortBy NOT IN (N'preRiskAssessmentNumber', N'issueDate', N'permitIssuerName',
        N'permitReceiverName', N'areaResponsibleName', N'plannedStartDateTime',
        N'plannedEndDateTime', N'riskAssessmentStatus')
        THROW 50013, 'SortBy is not supported.', 1;
    IF @SortDirection NOT IN ('asc', 'desc')
        THROW 50014, 'SortDirection must be asc or desc.', 1;

    DECLARE @SearchPattern nvarchar(402) = NULL;
    IF @SearchTerm IS NOT NULL
        SET @SearchPattern = N'%' +
            REPLACE(REPLACE(REPLACE(REPLACE(@SearchTerm, N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[') + N'%';

    SELECT COUNT_BIG(1) AS [TotalRecords]
    FROM [dbo].[RiskAssessment] AS riskAssessment
    INNER JOIN [dbo].[ListItem] AS statusItem
        ON statusItem.[ListItemId] = riskAssessment.[RiskAssessmentStatusListItemId]
    WHERE @SearchPattern IS NULL
       OR riskAssessment.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
       OR riskAssessment.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
       OR riskAssessment.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
       OR riskAssessment.[AreaResponsibleName] LIKE @SearchPattern ESCAPE N'\'
       OR statusItem.[ItemName] LIKE @SearchPattern ESCAPE N'\';

    SELECT riskAssessment.[Id], riskAssessment.[PreRiskAssessmentNumber],
        riskAssessment.[IssueDate], riskAssessment.[PermitIssuerName],
        riskAssessment.[PermitReceiverName], riskAssessment.[AreaResponsibleName],
        riskAssessment.[PlannedStartDateTime], riskAssessment.[PlannedEndDateTime],
        riskAssessment.[RiskAssessmentStatusListItemId],
        statusItem.[ItemName] AS [RiskAssessmentStatus]
    FROM [dbo].[RiskAssessment] AS riskAssessment
    INNER JOIN [dbo].[ListItem] AS statusItem
        ON statusItem.[ListItemId] = riskAssessment.[RiskAssessmentStatusListItemId]
    WHERE @SearchPattern IS NULL
       OR riskAssessment.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
       OR riskAssessment.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
       OR riskAssessment.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
       OR riskAssessment.[AreaResponsibleName] LIKE @SearchPattern ESCAPE N'\'
       OR statusItem.[ItemName] LIKE @SearchPattern ESCAPE N'\'
    ORDER BY
        CASE WHEN @SortBy = N'preRiskAssessmentNumber' AND @SortDirection = 'asc' THEN riskAssessment.[PreRiskAssessmentNumber] END ASC,
        CASE WHEN @SortBy = N'preRiskAssessmentNumber' AND @SortDirection = 'desc' THEN riskAssessment.[PreRiskAssessmentNumber] END DESC,
        CASE WHEN @SortBy = N'issueDate' AND @SortDirection = 'asc' THEN riskAssessment.[IssueDate] END ASC,
        CASE WHEN @SortBy = N'issueDate' AND @SortDirection = 'desc' THEN riskAssessment.[IssueDate] END DESC,
        CASE WHEN @SortBy = N'permitIssuerName' AND @SortDirection = 'asc' THEN riskAssessment.[PermitIssuerName] END ASC,
        CASE WHEN @SortBy = N'permitIssuerName' AND @SortDirection = 'desc' THEN riskAssessment.[PermitIssuerName] END DESC,
        CASE WHEN @SortBy = N'permitReceiverName' AND @SortDirection = 'asc' THEN riskAssessment.[PermitReceiverName] END ASC,
        CASE WHEN @SortBy = N'permitReceiverName' AND @SortDirection = 'desc' THEN riskAssessment.[PermitReceiverName] END DESC,
        CASE WHEN @SortBy = N'areaResponsibleName' AND @SortDirection = 'asc' THEN riskAssessment.[AreaResponsibleName] END ASC,
        CASE WHEN @SortBy = N'areaResponsibleName' AND @SortDirection = 'desc' THEN riskAssessment.[AreaResponsibleName] END DESC,
        CASE WHEN @SortBy = N'plannedStartDateTime' AND @SortDirection = 'asc' THEN riskAssessment.[PlannedStartDateTime] END ASC,
        CASE WHEN @SortBy = N'plannedStartDateTime' AND @SortDirection = 'desc' THEN riskAssessment.[PlannedStartDateTime] END DESC,
        CASE WHEN @SortBy = N'plannedEndDateTime' AND @SortDirection = 'asc' THEN riskAssessment.[PlannedEndDateTime] END ASC,
        CASE WHEN @SortBy = N'plannedEndDateTime' AND @SortDirection = 'desc' THEN riskAssessment.[PlannedEndDateTime] END DESC,
        CASE WHEN @SortBy = N'riskAssessmentStatus' AND @SortDirection = 'asc' THEN statusItem.[ItemName] END ASC,
        CASE WHEN @SortBy = N'riskAssessmentStatus' AND @SortDirection = 'desc' THEN statusItem.[ItemName] END DESC,
        riskAssessment.[Id] DESC
    OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260829120000_AddRiskAssessmentSorting', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Users] ADD [ProfilePicturePath] nvarchar(500) NULL;

ALTER TABLE [Users] ADD [ProfilePictureUpdatedAtUtc] datetime2(0) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260829130000_AddUserProfilePicture', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[Organization] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(20) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Address] nvarchar(500) NOT NULL,
    [PhoneNumber] nvarchar(30) NULL,
    [Email] nvarchar(320) NULL,
    [Website] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL,
    [UpdatedAtUtc] datetime2(0) NULL,
    CONSTRAINT [PK_Organization] PRIMARY KEY ([Id])
);

SET IDENTITY_INSERT [dbo].[Organization] ON;
INSERT INTO [dbo].[Organization]
    ([Id], [Code], [Name], [Address], [IsActive], [CreatedAtUtc])
VALUES
    (1, N'DEFAULT', N'Default Organization', N'Not configured', 1, SYSUTCDATETIME());
SET IDENTITY_INSERT [dbo].[Organization] OFF;

ALTER TABLE [dbo].[OfficeBranch] ADD [OrganizationId] int NULL;

UPDATE [dbo].[OfficeBranch] SET [OrganizationId] = 1 WHERE [OrganizationId] IS NULL;

DECLARE @var18 nvarchar(max);
SELECT @var18 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[OfficeBranch]') AND [c].[name] = N'OrganizationId');
IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[OfficeBranch] DROP CONSTRAINT ' + @var18 + ';');
ALTER TABLE [dbo].[OfficeBranch] ALTER COLUMN [OrganizationId] int NOT NULL;

CREATE INDEX [IX_OfficeBranch_OrganizationId_IsActive] ON [dbo].[OfficeBranch] ([OrganizationId], [IsActive]);

CREATE UNIQUE INDEX [IX_Organization_Code] ON [dbo].[Organization] ([Code]);

CREATE INDEX [IX_Organization_IsActive_Name] ON [dbo].[Organization] ([IsActive], [Name]);

ALTER TABLE [dbo].[OfficeBranch] ADD CONSTRAINT [FK_OfficeBranch_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [dbo].[Organization] ([Id]) ON DELETE NO ACTION;

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

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260829140000_AddOrganizationAndLinkOfficeBranches', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[RoleModule] (
    [RoleId] int NOT NULL,
    [ApplicationModuleId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [AssignedAtUtc] datetime2(0) NOT NULL,
    CONSTRAINT [PK_RoleModule] PRIMARY KEY ([RoleId], [ApplicationModuleId]),
    CONSTRAINT [FK_RoleModule_ApplicationModule_ApplicationModuleId] FOREIGN KEY ([ApplicationModuleId]) REFERENCES [dbo].[ApplicationModule] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RoleModule_Role_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Role] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_RoleModule_ApplicationModuleId_IsActive] ON [dbo].[RoleModule] ([ApplicationModuleId], [IsActive]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260829150000_AddRoleModuleAssignments', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [dbo].[ModuleMenu] ADD CONSTRAINT [AK_ModuleMenu_ApplicationModuleId_Id] UNIQUE ([ApplicationModuleId], [Id]);

CREATE TABLE [dbo].[RoleModuleMenu] (
    [RoleId] int NOT NULL,
    [ApplicationModuleId] int NOT NULL,
    [ModuleMenuId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [AssignedAtUtc] datetime2(0) NOT NULL,
    CONSTRAINT [PK_RoleModuleMenu] PRIMARY KEY ([RoleId], [ApplicationModuleId], [ModuleMenuId]),
    CONSTRAINT [FK_RoleModuleMenu_ModuleMenu_ApplicationModuleId_ModuleMenuId] FOREIGN KEY ([ApplicationModuleId], [ModuleMenuId]) REFERENCES [dbo].[ModuleMenu] ([ApplicationModuleId], [Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RoleModuleMenu_RoleModule_RoleId_ApplicationModuleId] FOREIGN KEY ([RoleId], [ApplicationModuleId]) REFERENCES [dbo].[RoleModule] ([RoleId], [ApplicationModuleId]) ON DELETE CASCADE
);

CREATE INDEX [IX_RoleModuleMenu_ApplicationModuleId_ModuleMenuId_IsActive] ON [dbo].[RoleModuleMenu] ([ApplicationModuleId], [ModuleMenuId], [IsActive]);

INSERT INTO [dbo].[RoleModuleMenu]
    ([RoleId], [ApplicationModuleId], [ModuleMenuId], [IsActive], [AssignedAtUtc])
SELECT rm.[RoleId], rm.[ApplicationModuleId], mm.[Id], CAST(1 AS bit), SYSUTCDATETIME()
FROM [dbo].[RoleModule] rm
INNER JOIN [dbo].[ModuleMenu] mm
    ON mm.[ApplicationModuleId] = rm.[ApplicationModuleId]
WHERE rm.[IsActive] = 1 AND mm.[IsActive] = 1;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260829160000_AddRoleModuleMenuAssignments', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
DROP TABLE [dbo].[UserModule];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260829170000_RemoveUserModuleAssignments', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
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

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260830100000_AddRolesGetProcedure', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE OR ALTER PROCEDURE [dbo].[SpApplicationModulesGet]
    @PageNumber int = 1,
    @PageSize int = 10,
    @SearchTerm nvarchar(200) = NULL,
    @IncludeInactive bit = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1
        THROW 50010, 'PageNumber must be greater than zero.', 1;

    IF @PageSize < 1 OR @PageSize > 100
        THROW 50011, 'PageSize must be between 1 and 100.', 1;

    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');

    DECLARE @SearchPattern nvarchar(402) = NULL;
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
    FROM [dbo].[ApplicationModule] AS module
    WHERE (@IncludeInactive = 1 OR module.[IsActive] = 1)
      AND (@SearchPattern IS NULL
        OR module.[Code] LIKE @SearchPattern ESCAPE N'\'
        OR module.[Name] LIKE @SearchPattern ESCAPE N'\'
        OR module.[Description] LIKE @SearchPattern ESCAPE N'\');

    SELECT
        module.[Id],
        module.[Code],
        module.[Name],
        module.[Description],
        module.[Icon],
        module.[DisplayOrder],
        module.[IsActive],
        module.[CreatedAtUtc]
    FROM [dbo].[ApplicationModule] AS module
    WHERE (@IncludeInactive = 1 OR module.[IsActive] = 1)
      AND (@SearchPattern IS NULL
        OR module.[Code] LIKE @SearchPattern ESCAPE N'\'
        OR module.[Name] LIKE @SearchPattern ESCAPE N'\'
        OR module.[Description] LIKE @SearchPattern ESCAPE N'\')
    ORDER BY module.[DisplayOrder], module.[Name], module.[Id]
    OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260830110000_AddApplicationModulesGetProcedure', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Users] ADD [CreatedByUserId] int NULL;

ALTER TABLE [Users] ADD [ModifiedAtUtc] datetime2(0) NULL;

ALTER TABLE [Users] ADD [ModifiedByUserId] int NULL;

CREATE INDEX [IX_Users_CreatedByUserId] ON [Users] ([CreatedByUserId]);

CREATE INDEX [IX_Users_ModifiedByUserId] ON [Users] ([ModifiedByUserId]);

ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Users_ModifiedByUserId] FOREIGN KEY ([ModifiedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

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

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260830161542_AddUserManagement', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
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

CREATE OR ALTER PROCEDURE [dbo].[SpListItemCategoriesDdl]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [ListItemCategoryId] AS [Id], [Code], [CategoryName] AS [Name]
    FROM [dbo].[ListItemCategory] WHERE [IsActive] = 1 ORDER BY [CategoryName], [ListItemCategoryId];
END;

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

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260830180000_AddListItemManagementProcedures', N'10.0.9');

COMMIT;
GO

