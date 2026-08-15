BEGIN TRANSACTION;
DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'PasswordHash');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var + ';');
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

