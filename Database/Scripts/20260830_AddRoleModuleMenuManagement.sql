BEGIN TRANSACTION;
ALTER TABLE [dbo].[RoleModuleMenu] ADD [AssignedBy] nvarchar(256) NULL;

ALTER TABLE [dbo].[RoleModuleMenu] ADD [ModifiedAtUtc] datetime2(0) NULL;

ALTER TABLE [dbo].[RoleModuleMenu] ADD [ModifiedBy] nvarchar(256) NULL;

UPDATE [dbo].[RoleModuleMenu] SET [AssignedBy] = N'System migration' WHERE [AssignedBy] IS NULL;

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpRoleModuleMenusGet]
    @PageNumber int=1,@PageSize int=10,@SearchTerm nvarchar(200)=NULL,
    @SortBy nvarchar(30)=N''assignedAtUtc'',@SortDirection varchar(4)=''desc'',
    @RoleId int=NULL,@ApplicationModuleId int=NULL,@IncludeInactive bit=0
AS
BEGIN
    SET NOCOUNT ON;
    SET @PageNumber=CASE WHEN @PageNumber<1 THEN 1 ELSE @PageNumber END;
    SET @PageSize=CASE WHEN @PageSize<1 THEN 10 WHEN @PageSize>100 THEN 100 ELSE @PageSize END;
    SET @SearchTerm=NULLIF(LTRIM(RTRIM(@SearchTerm)),N'''');
    SET @SortBy=LOWER(COALESCE(@SortBy,N''assignedatutc''));
    SET @SortDirection=LOWER(COALESCE(@SortDirection,''desc''));

    ;WITH MenuTree AS
    (
        SELECT m.[Id],m.[ApplicationModuleId],m.[ParentMenuId],m.[Name],m.[DisplayOrder],
            CAST(m.[Name] AS nvarchar(2000)) [MenuHierarchy]
        FROM [dbo].[ModuleMenu] m WHERE m.[ParentMenuId] IS NULL
        UNION ALL
        SELECT m.[Id],m.[ApplicationModuleId],m.[ParentMenuId],m.[Name],m.[DisplayOrder],
            CAST(CONCAT(t.[MenuHierarchy],N'' / '',m.[Name]) AS nvarchar(2000))
        FROM [dbo].[ModuleMenu] m INNER JOIN MenuTree t
            ON t.[Id]=m.[ParentMenuId] AND t.[ApplicationModuleId]=m.[ApplicationModuleId]
    )
    SELECT rmm.[RoleId],r.[Name] [RoleName],rmm.[ApplicationModuleId],am.[Name] [ModuleName],
        rmm.[ModuleMenuId],mt.[ParentMenuId],mt.[Name] [MenuName],mt.[MenuHierarchy],mt.[DisplayOrder],
        rmm.[IsActive],rmm.[AssignedAtUtc],rmm.[AssignedBy],rmm.[ModifiedAtUtc],rmm.[ModifiedBy]
    INTO #Assignments
    FROM [dbo].[RoleModuleMenu] rmm
    INNER JOIN [dbo].[Role] r ON r.[Id]=rmm.[RoleId]
    INNER JOIN [dbo].[ApplicationModule] am ON am.[Id]=rmm.[ApplicationModuleId]
    INNER JOIN MenuTree mt ON mt.[Id]=rmm.[ModuleMenuId]
        AND mt.[ApplicationModuleId]=rmm.[ApplicationModuleId]
    WHERE (@IncludeInactive=1 OR rmm.[IsActive]=1)
      AND (@RoleId IS NULL OR rmm.[RoleId]=@RoleId)
      AND (@ApplicationModuleId IS NULL OR rmm.[ApplicationModuleId]=@ApplicationModuleId)
      AND (@SearchTerm IS NULL OR r.[Name] LIKE N''%''+@SearchTerm+N''%''
           OR am.[Name] LIKE N''%''+@SearchTerm+N''%''
           OR mt.[Name] LIKE N''%''+@SearchTerm+N''%''
           OR mt.[MenuHierarchy] LIKE N''%''+@SearchTerm+N''%'')
    OPTION(MAXRECURSION 100);

    SELECT COUNT_BIG(1) [TotalRecords] FROM #Assignments;
    SELECT * FROM #Assignments
    ORDER BY
        CASE WHEN @SortBy=N''rolename'' AND @SortDirection=''asc'' THEN [RoleName] END ASC,
        CASE WHEN @SortBy=N''rolename'' AND @SortDirection=''desc'' THEN [RoleName] END DESC,
        CASE WHEN @SortBy=N''modulename'' AND @SortDirection=''asc'' THEN [ModuleName] END ASC,
        CASE WHEN @SortBy=N''modulename'' AND @SortDirection=''desc'' THEN [ModuleName] END DESC,
        CASE WHEN @SortBy=N''menuname'' AND @SortDirection=''asc'' THEN [MenuName] END ASC,
        CASE WHEN @SortBy=N''menuname'' AND @SortDirection=''desc'' THEN [MenuName] END DESC,
        CASE WHEN @SortBy=N''menuhierarchy'' AND @SortDirection=''asc'' THEN [MenuHierarchy] END ASC,
        CASE WHEN @SortBy=N''menuhierarchy'' AND @SortDirection=''desc'' THEN [MenuHierarchy] END DESC,
        CASE WHEN @SortBy=N''displayorder'' AND @SortDirection=''asc'' THEN [DisplayOrder] END ASC,
        CASE WHEN @SortBy=N''displayorder'' AND @SortDirection=''desc'' THEN [DisplayOrder] END DESC,
        CASE WHEN @SortBy=N''status'' AND @SortDirection=''asc'' THEN [IsActive] END ASC,
        CASE WHEN @SortBy=N''status'' AND @SortDirection=''desc'' THEN [IsActive] END DESC,
        CASE WHEN @SortBy=N''assignedby'' AND @SortDirection=''asc'' THEN [AssignedBy] END ASC,
        CASE WHEN @SortBy=N''assignedby'' AND @SortDirection=''desc'' THEN [AssignedBy] END DESC,
        CASE WHEN @SortBy=N''modifiedby'' AND @SortDirection=''asc'' THEN [ModifiedBy] END ASC,
        CASE WHEN @SortBy=N''modifiedby'' AND @SortDirection=''desc'' THEN [ModifiedBy] END DESC,
        CASE WHEN @SortBy=N''modifiedatutc'' AND @SortDirection=''asc'' THEN [ModifiedAtUtc] END ASC,
        CASE WHEN @SortBy=N''modifiedatutc'' AND @SortDirection=''desc'' THEN [ModifiedAtUtc] END DESC,
        CASE WHEN @SortBy=N''assignedatutc'' AND @SortDirection=''asc'' THEN [AssignedAtUtc] END ASC,
        CASE WHEN @SortBy=N''assignedatutc'' AND @SortDirection=''desc'' THEN [AssignedAtUtc] END DESC,
        [RoleName],[ModuleName],[DisplayOrder],[MenuName]
    OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpRoleModuleMenuRolesDdl]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT r.[Id],r.[Name] FROM [dbo].[Role] r
    WHERE r.[IsActive]=1 ORDER BY r.[Name];
END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpRoleModuleMenuModulesDdl] @RoleId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT am.[Id],am.[Code],am.[Name]
    FROM [dbo].[RoleModule] rm
    INNER JOIN [dbo].[Role] r ON r.[Id]=rm.[RoleId]
    INNER JOIN [dbo].[ApplicationModule] am ON am.[Id]=rm.[ApplicationModuleId]
    WHERE rm.[RoleId]=@RoleId AND rm.[IsActive]=1 AND r.[IsActive]=1 AND am.[IsActive]=1
    ORDER BY am.[DisplayOrder],am.[Name];
END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpRoleModuleMenuMenusDdl]
    @RoleId int,@ApplicationModuleId int
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS(SELECT 1 FROM [dbo].[RoleModule] rm
        INNER JOIN [dbo].[Role] r ON r.[Id]=rm.[RoleId]
        INNER JOIN [dbo].[ApplicationModule] am ON am.[Id]=rm.[ApplicationModuleId]
        WHERE rm.[RoleId]=@RoleId AND rm.[ApplicationModuleId]=@ApplicationModuleId
          AND rm.[IsActive]=1 AND r.[IsActive]=1 AND am.[IsActive]=1)
        THROW 50060,''The selected role does not have an active assignment to this module.'',1;

    ;WITH MenuTree AS
    (
        SELECT m.[Id],m.[ParentMenuId],m.[Name],m.[DisplayOrder],0 [Depth],
            CAST(m.[Name] AS nvarchar(2000)) [Hierarchy],
            CAST(CONCAT(RIGHT(''000000''+CAST(m.[DisplayOrder] AS varchar(6)),6),''-'',RIGHT(''0000000000''+CAST(m.[Id] AS varchar(10)),10)) AS nvarchar(2000)) [SortPath]
        FROM [dbo].[ModuleMenu] m
        WHERE m.[ApplicationModuleId]=@ApplicationModuleId AND m.[ParentMenuId] IS NULL AND m.[IsActive]=1
        UNION ALL
        SELECT m.[Id],m.[ParentMenuId],m.[Name],m.[DisplayOrder],t.[Depth]+1,
            CAST(CONCAT(t.[Hierarchy],N'' / '',m.[Name]) AS nvarchar(2000)),
            CAST(CONCAT(t.[SortPath],N''/'',RIGHT(''000000''+CAST(m.[DisplayOrder] AS varchar(6)),6),''-'',RIGHT(''0000000000''+CAST(m.[Id] AS varchar(10)),10)) AS nvarchar(2000))
        FROM [dbo].[ModuleMenu] m INNER JOIN MenuTree t ON t.[Id]=m.[ParentMenuId]
        WHERE m.[ApplicationModuleId]=@ApplicationModuleId AND m.[IsActive]=1
    )
    SELECT t.[Id],t.[ParentMenuId],t.[Name],t.[Hierarchy],t.[Depth],t.[DisplayOrder],
        CAST(CASE WHEN EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] x
            WHERE x.[RoleId]=@RoleId AND x.[ApplicationModuleId]=@ApplicationModuleId
              AND x.[ModuleMenuId]=t.[Id] AND x.[IsActive]=1) THEN 1 ELSE 0 END AS bit) [IsAssigned],
        CAST(CASE WHEN t.[ParentMenuId] IS NULL OR EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] p
            WHERE p.[RoleId]=@RoleId AND p.[ApplicationModuleId]=@ApplicationModuleId
              AND p.[ModuleMenuId]=t.[ParentMenuId] AND p.[IsActive]=1) THEN 1 ELSE 0 END AS bit) [CanAssign]
    FROM MenuTree t ORDER BY t.[SortPath] OPTION(MAXRECURSION 100);
END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpRoleModuleMenusAdd]
    @RoleId int,@ApplicationModuleId int,@ModuleMenuId int,@IsActive bit=1,
    @ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @Now datetime2(0)=SYSUTCDATETIME(),@ParentMenuId int;
    SET @ActorName=COALESCE(NULLIF(LTRIM(RTRIM(@ActorName)),N''''),N''System'');
    BEGIN TRANSACTION;
    IF NOT EXISTS(SELECT 1 FROM [dbo].[Role] WITH(UPDLOCK,HOLDLOCK) WHERE [Id]=@RoleId AND [IsActive]=1)
        THROW 50061,''The selected role does not exist or is inactive.'',1;
    IF NOT EXISTS(SELECT 1 FROM [dbo].[RoleModule] WITH(UPDLOCK,HOLDLOCK)
        WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId AND [IsActive]=1)
        THROW 50060,''Assign the module to this role before assigning its menus.'',1;
    SELECT @ParentMenuId=[ParentMenuId] FROM [dbo].[ModuleMenu] WITH(UPDLOCK,HOLDLOCK)
        WHERE [Id]=@ModuleMenuId AND [ApplicationModuleId]=@ApplicationModuleId AND [IsActive]=1;
    IF @@ROWCOUNT=0 THROW 50062,''The selected menu does not belong to this module or is inactive.'',1;
    IF @IsActive=1 AND @ParentMenuId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] WITH(UPDLOCK,HOLDLOCK)
        WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
          AND [ModuleMenuId]=@ParentMenuId AND [IsActive]=1)
        THROW 50063,''Assign the parent menu before assigning this child menu.'',1;

    DECLARE @Old nvarchar(max)=NULL;
    IF EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] WITH(UPDLOCK,HOLDLOCK)
        WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId AND [ModuleMenuId]=@ModuleMenuId)
    BEGIN
        SELECT @Old=(SELECT [RoleId],[ApplicationModuleId],[ModuleMenuId],[IsActive],[AssignedAtUtc],[AssignedBy],[ModifiedAtUtc],[ModifiedBy]
            FROM [dbo].[RoleModuleMenu] WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
              AND [ModuleMenuId]=@ModuleMenuId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
        IF EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] WHERE [RoleId]=@RoleId
            AND [ApplicationModuleId]=@ApplicationModuleId AND [ModuleMenuId]=@ModuleMenuId AND [IsActive]=1)
            THROW 50064,''This role already has an active assignment to the selected menu.'',1;
        UPDATE [dbo].[RoleModuleMenu] SET [IsActive]=@IsActive,[AssignedAtUtc]=@Now,[AssignedBy]=@ActorName,
            [ModifiedAtUtc]=NULL,[ModifiedBy]=NULL
        WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId AND [ModuleMenuId]=@ModuleMenuId;
    END
    ELSE
        INSERT [dbo].[RoleModuleMenu]([RoleId],[ApplicationModuleId],[ModuleMenuId],[IsActive],[AssignedAtUtc],[AssignedBy])
        VALUES(@RoleId,@ApplicationModuleId,@ModuleMenuId,@IsActive,@Now,@ActorName);

    DECLARE @New nvarchar(max)=(SELECT [RoleId],[ApplicationModuleId],[ModuleMenuId],[IsActive],[AssignedAtUtc],[AssignedBy],[ModifiedAtUtc],[ModifiedBy]
        FROM [dbo].[RoleModuleMenu] WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
          AND [ModuleMenuId]=@ModuleMenuId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
    INSERT [dbo].[AuditLog]([EntityName],[Action],[EntityKey],[ChangedColumns],[OldValues],[NewValues],[ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
    VALUES(N''RoleModuleMenu'',N''Assigned'',CONCAT(N''{"RoleId":'',@RoleId,N'',"ApplicationModuleId":'',@ApplicationModuleId,N'',"ModuleMenuId":'',@ModuleMenuId,N''}''),
        N''["IsActive","AssignedAtUtc","AssignedBy"]'',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
    COMMIT;

    ;WITH MenuTree AS
    (
        SELECT m.[Id],m.[ApplicationModuleId],m.[ParentMenuId],m.[Name],m.[DisplayOrder],CAST(m.[Name] AS nvarchar(2000)) [Hierarchy]
        FROM [dbo].[ModuleMenu] m WHERE m.[ApplicationModuleId]=@ApplicationModuleId AND m.[ParentMenuId] IS NULL
        UNION ALL
        SELECT m.[Id],m.[ApplicationModuleId],m.[ParentMenuId],m.[Name],m.[DisplayOrder],CAST(CONCAT(t.[Hierarchy],N'' / '',m.[Name]) AS nvarchar(2000))
        FROM [dbo].[ModuleMenu] m INNER JOIN MenuTree t ON t.[Id]=m.[ParentMenuId] AND t.[ApplicationModuleId]=m.[ApplicationModuleId]
    )
    SELECT x.[RoleId],r.[Name] [RoleName],x.[ApplicationModuleId],am.[Name] [ModuleName],x.[ModuleMenuId],t.[ParentMenuId],
        t.[Name] [MenuName],t.[Hierarchy] [MenuHierarchy],t.[DisplayOrder],x.[IsActive],x.[AssignedAtUtc],x.[AssignedBy],x.[ModifiedAtUtc],x.[ModifiedBy]
    FROM [dbo].[RoleModuleMenu] x INNER JOIN [dbo].[Role] r ON r.[Id]=x.[RoleId]
    INNER JOIN [dbo].[ApplicationModule] am ON am.[Id]=x.[ApplicationModuleId]
    INNER JOIN MenuTree t ON t.[Id]=x.[ModuleMenuId] AND t.[ApplicationModuleId]=x.[ApplicationModuleId]
    WHERE x.[RoleId]=@RoleId AND x.[ApplicationModuleId]=@ApplicationModuleId AND x.[ModuleMenuId]=@ModuleMenuId
    OPTION(MAXRECURSION 100);
END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpRoleModuleMenusEdit]
    @RoleId int,@ApplicationModuleId int,@ModuleMenuId int,@IsActive bit,
    @ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @Now datetime2(0)=SYSUTCDATETIME(),@ParentMenuId int;
    SET @ActorName=COALESCE(NULLIF(LTRIM(RTRIM(@ActorName)),N''''),N''System'');
    BEGIN TRANSACTION;
    IF NOT EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] WITH(UPDLOCK,HOLDLOCK)
        WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId AND [ModuleMenuId]=@ModuleMenuId)
        THROW 50004,''The role menu assignment was not found.'',1;
    SELECT @ParentMenuId=[ParentMenuId] FROM [dbo].[ModuleMenu]
        WHERE [Id]=@ModuleMenuId AND [ApplicationModuleId]=@ApplicationModuleId;
    IF @IsActive=1
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM [dbo].[RoleModule] rm INNER JOIN [dbo].[Role] r ON r.[Id]=rm.[RoleId]
            INNER JOIN [dbo].[ApplicationModule] am ON am.[Id]=rm.[ApplicationModuleId]
            INNER JOIN [dbo].[ModuleMenu] mm ON mm.[Id]=@ModuleMenuId AND mm.[ApplicationModuleId]=rm.[ApplicationModuleId]
            WHERE rm.[RoleId]=@RoleId AND rm.[ApplicationModuleId]=@ApplicationModuleId
              AND rm.[IsActive]=1 AND r.[IsActive]=1 AND am.[IsActive]=1 AND mm.[IsActive]=1)
            THROW 50065,''The role, module assignment, or menu is inactive.'',1;
        IF @ParentMenuId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] WITH(UPDLOCK,HOLDLOCK)
            WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
              AND [ModuleMenuId]=@ParentMenuId AND [IsActive]=1)
            THROW 50063,''Activate the parent menu assignment before activating this child menu.'',1;
    END
    ELSE IF EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] childAssignment WITH(UPDLOCK,HOLDLOCK)
        INNER JOIN [dbo].[ModuleMenu] childMenu ON childMenu.[Id]=childAssignment.[ModuleMenuId]
          AND childMenu.[ApplicationModuleId]=childAssignment.[ApplicationModuleId]
        WHERE childAssignment.[RoleId]=@RoleId AND childAssignment.[ApplicationModuleId]=@ApplicationModuleId
          AND childMenu.[ParentMenuId]=@ModuleMenuId AND childAssignment.[IsActive]=1)
        THROW 50066,''Deactivate the assigned child menus before deactivating their parent menu.'',1;

    DECLARE @Old nvarchar(max)=(SELECT [RoleId],[ApplicationModuleId],[ModuleMenuId],[IsActive],[AssignedAtUtc],[AssignedBy],[ModifiedAtUtc],[ModifiedBy]
        FROM [dbo].[RoleModuleMenu] WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
          AND [ModuleMenuId]=@ModuleMenuId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
    UPDATE [dbo].[RoleModuleMenu] SET [IsActive]=@IsActive,[ModifiedAtUtc]=@Now,[ModifiedBy]=@ActorName
    WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId AND [ModuleMenuId]=@ModuleMenuId;
    DECLARE @New nvarchar(max)=(SELECT [RoleId],[ApplicationModuleId],[ModuleMenuId],[IsActive],[AssignedAtUtc],[AssignedBy],[ModifiedAtUtc],[ModifiedBy]
        FROM [dbo].[RoleModuleMenu] WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
          AND [ModuleMenuId]=@ModuleMenuId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
    INSERT [dbo].[AuditLog]([EntityName],[Action],[EntityKey],[ChangedColumns],[OldValues],[NewValues],[ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
    VALUES(N''RoleModuleMenu'',N''Modified'',CONCAT(N''{"RoleId":'',@RoleId,N'',"ApplicationModuleId":'',@ApplicationModuleId,N'',"ModuleMenuId":'',@ModuleMenuId,N''}''),
        N''["IsActive","ModifiedAtUtc","ModifiedBy"]'',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
    COMMIT;

    ;WITH MenuTree AS
    (
        SELECT m.[Id],m.[ApplicationModuleId],m.[ParentMenuId],m.[Name],m.[DisplayOrder],CAST(m.[Name] AS nvarchar(2000)) [Hierarchy]
        FROM [dbo].[ModuleMenu] m WHERE m.[ApplicationModuleId]=@ApplicationModuleId AND m.[ParentMenuId] IS NULL
        UNION ALL
        SELECT m.[Id],m.[ApplicationModuleId],m.[ParentMenuId],m.[Name],m.[DisplayOrder],CAST(CONCAT(t.[Hierarchy],N'' / '',m.[Name]) AS nvarchar(2000))
        FROM [dbo].[ModuleMenu] m INNER JOIN MenuTree t ON t.[Id]=m.[ParentMenuId] AND t.[ApplicationModuleId]=m.[ApplicationModuleId]
    )
    SELECT x.[RoleId],r.[Name] [RoleName],x.[ApplicationModuleId],am.[Name] [ModuleName],x.[ModuleMenuId],t.[ParentMenuId],
        t.[Name] [MenuName],t.[Hierarchy] [MenuHierarchy],t.[DisplayOrder],x.[IsActive],x.[AssignedAtUtc],x.[AssignedBy],x.[ModifiedAtUtc],x.[ModifiedBy]
    FROM [dbo].[RoleModuleMenu] x INNER JOIN [dbo].[Role] r ON r.[Id]=x.[RoleId]
    INNER JOIN [dbo].[ApplicationModule] am ON am.[Id]=x.[ApplicationModuleId]
    INNER JOIN MenuTree t ON t.[Id]=x.[ModuleMenuId] AND t.[ApplicationModuleId]=x.[ApplicationModuleId]
    WHERE x.[RoleId]=@RoleId AND x.[ApplicationModuleId]=@ApplicationModuleId AND x.[ModuleMenuId]=@ModuleMenuId
    OPTION(MAXRECURSION 100);
END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpRoleModuleMenusDelete]
    @RoleId int,@ApplicationModuleId int,@ModuleMenuId int,
    @ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @Now datetime2(0)=SYSUTCDATETIME();
    SET @ActorName=COALESCE(NULLIF(LTRIM(RTRIM(@ActorName)),N''''),N''System'');
    BEGIN TRANSACTION;
    IF NOT EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] WITH(UPDLOCK,HOLDLOCK)
        WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId AND [ModuleMenuId]=@ModuleMenuId)
    BEGIN COMMIT; SELECT CAST(0 AS int); RETURN; END;
    IF EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] childAssignment WITH(UPDLOCK,HOLDLOCK)
        INNER JOIN [dbo].[ModuleMenu] childMenu ON childMenu.[Id]=childAssignment.[ModuleMenuId]
          AND childMenu.[ApplicationModuleId]=childAssignment.[ApplicationModuleId]
        WHERE childAssignment.[RoleId]=@RoleId AND childAssignment.[ApplicationModuleId]=@ApplicationModuleId
          AND childMenu.[ParentMenuId]=@ModuleMenuId AND childAssignment.[IsActive]=1)
        THROW 50066,''Deactivate the assigned child menus before deactivating their parent menu.'',1;
    DECLARE @Old nvarchar(max)=(SELECT [RoleId],[ApplicationModuleId],[ModuleMenuId],[IsActive],[AssignedAtUtc],[AssignedBy],[ModifiedAtUtc],[ModifiedBy]
        FROM [dbo].[RoleModuleMenu] WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
          AND [ModuleMenuId]=@ModuleMenuId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
    UPDATE [dbo].[RoleModuleMenu] SET [IsActive]=0,[ModifiedAtUtc]=@Now,[ModifiedBy]=@ActorName
    WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId AND [ModuleMenuId]=@ModuleMenuId;
    DECLARE @New nvarchar(max)=(SELECT [RoleId],[ApplicationModuleId],[ModuleMenuId],[IsActive],[AssignedAtUtc],[AssignedBy],[ModifiedAtUtc],[ModifiedBy]
        FROM [dbo].[RoleModuleMenu] WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
          AND [ModuleMenuId]=@ModuleMenuId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
    INSERT [dbo].[AuditLog]([EntityName],[Action],[EntityKey],[ChangedColumns],[OldValues],[NewValues],[ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
    VALUES(N''RoleModuleMenu'',N''Deactivated'',CONCAT(N''{"RoleId":'',@RoleId,N'',"ApplicationModuleId":'',@ApplicationModuleId,N'',"ModuleMenuId":'',@ModuleMenuId,N''}''),
        N''["IsActive","ModifiedAtUtc","ModifiedBy"]'',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
    COMMIT; SELECT CAST(1 AS int);
END;');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260830184001_AddRoleModuleMenuManagement', N'10.0.9');

COMMIT;
GO

