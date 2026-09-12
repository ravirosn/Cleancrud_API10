BEGIN TRANSACTION;
CREATE TABLE [dbo].[PermitApplicationGuidelines] (
    [Id] int NOT NULL IDENTITY,
    [PermitTypeListItemId] int NOT NULL,
    [Guidelines] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedByUserId] int NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(0) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_PermitApplicationGuidelines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PermitApplicationGuidelines_ListItem_PermitTypeListItemId] FOREIGN KEY ([PermitTypeListItemId]) REFERENCES [dbo].[ListItem] ([ListItemId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PermitApplicationGuidelines_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PermitApplicationGuidelines_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_PermitApplicationGuidelines_CreatedByUserId] ON [dbo].[PermitApplicationGuidelines] ([CreatedByUserId]);

CREATE INDEX [IX_PermitApplicationGuidelines_UpdatedByUserId] ON [dbo].[PermitApplicationGuidelines] ([UpdatedByUserId]);

CREATE UNIQUE INDEX [UX_PermitApplicationGuidelines_ActivePermitType] ON [dbo].[PermitApplicationGuidelines] ([PermitTypeListItemId]) WHERE [IsActive] = 1;

MERGE [dbo].[PermissionPolicy] AS target
USING (VALUES
    (N'PermitApplicationGuideline.View',N'View permit application guidelines',N'View and search permit-type guidelines.'),
    (N'PermitApplicationGuideline.Create',N'Create permit application guidelines',N'Create permit-type guidelines.'),
    (N'PermitApplicationGuideline.Edit',N'Edit permit application guidelines',N'Edit permit-type guidelines.'),
    (N'PermitApplicationGuideline.Delete',N'Deactivate permit application guidelines',N'Deactivate permit-type guidelines.')
) AS source([Code],[Name],[Description])
ON target.[Code]=source.[Code]
WHEN MATCHED THEN UPDATE SET
    [Name]=source.[Name],[Description]=source.[Description],[ModuleCode]=N'ORGANIZATION',
    [MenuController]=N'Setup',[MenuAction]=N'PermitApplicationGuidelines',
    [RequiresMenuAccess]=0,[IsActive]=1,[UpdatedAtUtc]=SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT ([Code],[Name],[Description],[ModuleCode],[MenuController],[MenuAction],
            [RequiresMenuAccess],[IsActive],[CreatedAtUtc])
    VALUES (source.[Code],source.[Name],source.[Description],N'ORGANIZATION',N'Setup',
            N'PermitApplicationGuidelines',0,1,SYSUTCDATETIME());

INSERT [dbo].[RolePermission]
    ([RoleId],[PermissionPolicyId],[IsActive],[AssignedAtUtc],[AssignedBy])
SELECT role.[Id],policy.[Id],1,SYSUTCDATETIME(),N'Permit application guidelines migration'
FROM [dbo].[Role] role
CROSS JOIN [dbo].[PermissionPolicy] policy
WHERE role.[NormalizedName]=N'ADMIN' AND role.[IsActive]=1
  AND policy.[Code] IN (N'PermitApplicationGuideline.View',N'PermitApplicationGuideline.Create',
      N'PermitApplicationGuideline.Edit',N'PermitApplicationGuideline.Delete')
  AND NOT EXISTS
  (
      SELECT 1 FROM [dbo].[RolePermission] existing
      WHERE existing.[RoleId]=role.[Id] AND existing.[PermissionPolicyId]=policy.[Id]
  );

UPDATE assignment SET [IsActive]=1,[ModifiedAtUtc]=SYSUTCDATETIME(),
    [ModifiedBy]=N'Permit application guidelines migration'
FROM [dbo].[RolePermission] assignment
INNER JOIN [dbo].[Role] role ON role.[Id]=assignment.[RoleId]
INNER JOIN [dbo].[PermissionPolicy] policy ON policy.[Id]=assignment.[PermissionPolicyId]
WHERE role.[NormalizedName]=N'ADMIN' AND role.[IsActive]=1
  AND assignment.[IsActive]=0
  AND policy.[Code] IN (N'PermitApplicationGuideline.View',N'PermitApplicationGuideline.Create',
      N'PermitApplicationGuideline.Edit',N'PermitApplicationGuideline.Delete');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260911114613_AddPermitApplicationGuidelines', N'10.0.9');

COMMIT;
GO

