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

