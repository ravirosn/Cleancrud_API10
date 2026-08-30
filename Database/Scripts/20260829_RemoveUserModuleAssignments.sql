BEGIN TRANSACTION;

IF NOT EXISTS (
    SELECT 1
    FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829170000_RemoveUserModuleAssignments'
)
BEGIN
    DROP TABLE IF EXISTS [dbo].[UserModule];

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260829170000_RemoveUserModuleAssignments', N'10.0.9');
END;

COMMIT;
GO
