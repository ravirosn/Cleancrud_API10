BEGIN TRANSACTION;

IF NOT EXISTS (
    SELECT 1
    FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829150000_AddRoleModuleAssignments'
)
BEGIN
    CREATE TABLE [dbo].[RoleModule] (
        [RoleId] int NOT NULL,
        [ApplicationModuleId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [AssignedAtUtc] datetime2(0) NOT NULL,
        CONSTRAINT [PK_RoleModule]
            PRIMARY KEY ([RoleId], [ApplicationModuleId]),
        CONSTRAINT [FK_RoleModule_ApplicationModule_ApplicationModuleId]
            FOREIGN KEY ([ApplicationModuleId])
            REFERENCES [dbo].[ApplicationModule] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [FK_RoleModule_Role_RoleId]
            FOREIGN KEY ([RoleId])
            REFERENCES [dbo].[Role] ([Id])
            ON DELETE NO ACTION
    );

    CREATE INDEX [IX_RoleModule_ApplicationModuleId_IsActive]
        ON [dbo].[RoleModule] ([ApplicationModuleId], [IsActive]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260829150000_AddRoleModuleAssignments', N'10.0.9');
END;

COMMIT;
GO
