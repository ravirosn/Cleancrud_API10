BEGIN TRANSACTION;

IF NOT EXISTS (
    SELECT 1
    FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829160000_AddRoleModuleMenuAssignments'
)
BEGIN
    ALTER TABLE [dbo].[ModuleMenu]
        ADD CONSTRAINT [AK_ModuleMenu_ApplicationModuleId_Id]
        UNIQUE ([ApplicationModuleId], [Id]);

    CREATE TABLE [dbo].[RoleModuleMenu] (
        [RoleId] int NOT NULL,
        [ApplicationModuleId] int NOT NULL,
        [ModuleMenuId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [AssignedAtUtc] datetime2(0) NOT NULL,
        CONSTRAINT [PK_RoleModuleMenu]
            PRIMARY KEY ([RoleId], [ApplicationModuleId], [ModuleMenuId]),
        CONSTRAINT [FK_RoleModuleMenu_ModuleMenu_ApplicationModuleId_ModuleMenuId]
            FOREIGN KEY ([ApplicationModuleId], [ModuleMenuId])
            REFERENCES [dbo].[ModuleMenu] ([ApplicationModuleId], [Id])
            ON DELETE NO ACTION,
        CONSTRAINT [FK_RoleModuleMenu_RoleModule_RoleId_ApplicationModuleId]
            FOREIGN KEY ([RoleId], [ApplicationModuleId])
            REFERENCES [dbo].[RoleModule] ([RoleId], [ApplicationModuleId])
            ON DELETE CASCADE
    );

    CREATE INDEX [IX_RoleModuleMenu_ApplicationModuleId_ModuleMenuId_IsActive]
        ON [dbo].[RoleModuleMenu]
        ([ApplicationModuleId], [ModuleMenuId], [IsActive]);

    -- Preserve menus currently available through existing active role-module grants.
    INSERT INTO [dbo].[RoleModuleMenu]
        ([RoleId], [ApplicationModuleId], [ModuleMenuId], [IsActive], [AssignedAtUtc])
    SELECT rm.[RoleId], rm.[ApplicationModuleId], mm.[Id], CAST(1 AS bit), SYSUTCDATETIME()
    FROM [dbo].[RoleModule] rm
    INNER JOIN [dbo].[ModuleMenu] mm
        ON mm.[ApplicationModuleId] = rm.[ApplicationModuleId]
    WHERE rm.[IsActive] = 1 AND mm.[IsActive] = 1;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260829160000_AddRoleModuleMenuAssignments', N'10.0.9');
END;

COMMIT;
GO
