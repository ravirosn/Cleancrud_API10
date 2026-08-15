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

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Role');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var + ';');
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

