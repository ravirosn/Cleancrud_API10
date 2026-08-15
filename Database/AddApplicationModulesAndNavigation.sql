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

