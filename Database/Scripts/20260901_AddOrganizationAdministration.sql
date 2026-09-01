BEGIN TRANSACTION;
GO
CREATE OR ALTER PROCEDURE dbo.SPOrganizationGet @Id int = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) Id, Code, Name, Address, PhoneNumber, Email, Website,
        IsActive, CreatedAtUtc, UpdatedAtUtc
    FROM dbo.Organization
    WHERE @Id IS NULL OR Id = @Id
    ORDER BY CASE WHEN IsActive = 1 THEN 0 ELSE 1 END, Id;
END;

GO
CREATE OR ALTER PROCEDURE dbo.SPOrganizationUpd
    @Id int, @Code nvarchar(20), @Name nvarchar(200), @Address nvarchar(500),
    @PhoneNumber nvarchar(30) = NULL, @Email nvarchar(320) = NULL,
    @Website nvarchar(500) = NULL, @IsActive bit = 1,
    @ActorUserId int = NULL, @ActorName nvarchar(256) = NULL,
    @TraceId nvarchar(100) = NULL, @IpAddress nvarchar(45) = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Organization WHERE Id = @Id) RETURN;
    SET @Code = UPPER(LTRIM(RTRIM(@Code))); SET @Name = LTRIM(RTRIM(@Name));
    SET @Address = LTRIM(RTRIM(@Address));
    SET @PhoneNumber = NULLIF(LTRIM(RTRIM(@PhoneNumber)), N'');
    SET @Email = NULLIF(LTRIM(RTRIM(@Email)), N'');
    SET @Website = NULLIF(LTRIM(RTRIM(@Website)), N'');
    IF NULLIF(@Code, N'') IS NULL THROW 50100, 'Organization code is required.', 1;
    IF NULLIF(@Name, N'') IS NULL THROW 50101, 'Organization name is required.', 1;
    IF NULLIF(@Address, N'') IS NULL THROW 50102, 'Organization address is required.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Organization WHERE Code = @Code AND Id <> @Id)
        THROW 50103, 'An organization with this code already exists.', 1;
    IF @IsActive = 0 AND EXISTS (SELECT 1 FROM dbo.OfficeBranch WHERE OrganizationId = @Id AND IsActive = 1)
        THROW 50104, 'Disable the organization''s active office branches before disabling the organization.', 1;

    DECLARE @OldValues nvarchar(max) = (SELECT Code, Name, Address, PhoneNumber, Email, Website, IsActive
        FROM dbo.Organization WHERE Id = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    UPDATE dbo.Organization SET Code = @Code, Name = @Name, Address = @Address,
        PhoneNumber = @PhoneNumber, Email = @Email, Website = @Website,
        IsActive = @IsActive, UpdatedAtUtc = SYSUTCDATETIME() WHERE Id = @Id;
    DECLARE @NewValues nvarchar(max) = (SELECT Code, Name, Address, PhoneNumber, Email, Website, IsActive
        FROM dbo.Organization WHERE Id = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    INSERT dbo.AuditLog(EntityName, Action, EntityKey, ChangedColumns, OldValues, NewValues,
        ChangedByUserId, ChangedBy, TraceId, IpAddress, ChangedAtUtc)
    VALUES(N'Organization', N'UPDATE', CONCAT(N'{"Id":', @Id, N'}'),
        N'["Code","Name","Address","PhoneNumber","Email","Website","IsActive"]',
        @OldValues, @NewValues, @ActorUserId, @ActorName, @TraceId, @IpAddress, SYSUTCDATETIME());
    EXEC dbo.SPOrganizationGet @Id;
END;

GO
CREATE OR ALTER PROCEDURE dbo.SPOfficeBranchGet
    @PageNumber int = 1, @PageSize int = 20, @SearchTerm nvarchar(200) = NULL,
    @IncludeInactive bit = 0, @SortBy nvarchar(32) = N'name', @SortDirection varchar(4) = 'asc'
AS
BEGIN
    SET NOCOUNT ON;
    IF @PageNumber < 1 THROW 50010, 'PageNumber must be greater than zero.', 1;
    IF @PageSize < 1 OR @PageSize > 100 THROW 50011, 'PageSize must be between 1 and 100.', 1;
    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
    SET @SortBy = LOWER(NULLIF(LTRIM(RTRIM(@SortBy)), N''));
    SET @SortDirection = LOWER(NULLIF(LTRIM(RTRIM(@SortDirection)), ''));
    IF @SortBy NOT IN (N'organizationname', N'code', N'name', N'address', N'isheadoffice', N'headoffice', N'status') SET @SortBy = N'name';
    IF @SortDirection NOT IN ('asc', 'desc') SET @SortDirection = 'asc';
    DECLARE @Pattern nvarchar(402) = NULL;
    IF @SearchTerm IS NOT NULL SET @Pattern = N'%' + REPLACE(REPLACE(REPLACE(REPLACE(@SearchTerm,N'\',N'\\'),N'%',N'\%'),N'_',N'\_'),N'[',N'\[') + N'%';

    SELECT COUNT_BIG(1) TotalRecords FROM dbo.OfficeBranch b
    INNER JOIN dbo.Organization o ON o.Id = b.OrganizationId
    WHERE (@IncludeInactive = 1 OR b.IsActive = 1)
      AND (@Pattern IS NULL OR b.Code LIKE @Pattern ESCAPE N'\' OR b.Name LIKE @Pattern ESCAPE N'\'
        OR b.Address LIKE @Pattern ESCAPE N'\' OR o.Name LIKE @Pattern ESCAPE N'\');

    SELECT b.Id, b.OrganizationId, o.Name OrganizationName, b.Code, b.Name, b.Address, b.IsHeadOffice, b.IsActive
    FROM dbo.OfficeBranch b INNER JOIN dbo.Organization o ON o.Id = b.OrganizationId
    WHERE (@IncludeInactive = 1 OR b.IsActive = 1)
      AND (@Pattern IS NULL OR b.Code LIKE @Pattern ESCAPE N'\' OR b.Name LIKE @Pattern ESCAPE N'\'
        OR b.Address LIKE @Pattern ESCAPE N'\' OR o.Name LIKE @Pattern ESCAPE N'\')
    ORDER BY
      CASE WHEN @SortBy=N'organizationname' AND @SortDirection='asc' THEN o.Name END ASC,
      CASE WHEN @SortBy=N'organizationname' AND @SortDirection='desc' THEN o.Name END DESC,
      CASE WHEN @SortBy=N'code' AND @SortDirection='asc' THEN b.Code END ASC,
      CASE WHEN @SortBy=N'code' AND @SortDirection='desc' THEN b.Code END DESC,
      CASE WHEN @SortBy=N'name' AND @SortDirection='asc' THEN b.Name END ASC,
      CASE WHEN @SortBy=N'name' AND @SortDirection='desc' THEN b.Name END DESC,
      CASE WHEN @SortBy=N'address' AND @SortDirection='asc' THEN b.Address END ASC,
      CASE WHEN @SortBy=N'address' AND @SortDirection='desc' THEN b.Address END DESC,
      CASE WHEN @SortBy IN (N'isheadoffice',N'headoffice') AND @SortDirection='asc' THEN b.IsHeadOffice END ASC,
      CASE WHEN @SortBy IN (N'isheadoffice',N'headoffice') AND @SortDirection='desc' THEN b.IsHeadOffice END DESC,
      CASE WHEN @SortBy=N'status' AND @SortDirection='asc' THEN b.IsActive END ASC,
      CASE WHEN @SortBy=N'status' AND @SortDirection='desc' THEN b.IsActive END DESC, b.Id
    OFFSET (CONVERT(bigint,@PageNumber)-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;

GO
CREATE OR ALTER PROCEDURE dbo.SPOfficeBranchIns
  @OrganizationId int,@Code nvarchar(20),@Name nvarchar(150),@Address nvarchar(500)=NULL,@IsHeadOffice bit=0,@IsActive bit=1,
  @ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
AS
BEGIN
  SET NOCOUNT ON; SET XACT_ABORT ON; SET @Code=UPPER(LTRIM(RTRIM(@Code))); SET @Name=LTRIM(RTRIM(@Name)); SET @Address=NULLIF(LTRIM(RTRIM(@Address)),N'');
  IF NOT EXISTS(SELECT 1 FROM dbo.Organization WHERE Id=@OrganizationId) THROW 50040,'Organization was not found.',1;
  IF @IsActive=1 AND NOT EXISTS(SELECT 1 FROM dbo.Organization WHERE Id=@OrganizationId AND IsActive=1) THROW 50041,'An active branch cannot belong to an inactive organization.',1;
  IF NULLIF(@Code,N'') IS NULL THROW 50020,'Code is required.',1; IF NULLIF(@Name,N'') IS NULL THROW 50021,'Name is required.',1;
  IF @IsHeadOffice=1 AND @IsActive=0 THROW 50022,'The head office must be active.',1;
  IF EXISTS(SELECT 1 FROM dbo.OfficeBranch WHERE Code=@Code) THROW 50023,'An office branch with this code already exists.',1;
  IF @IsActive=1 AND @IsHeadOffice=0 AND NOT EXISTS(SELECT 1 FROM dbo.OfficeBranch WHERE OrganizationId=@OrganizationId AND IsHeadOffice=1 AND IsActive=1) THROW 50024,'The first active office branch must be the head office.',1;
  BEGIN TRANSACTION;
  IF @IsHeadOffice=1 UPDATE dbo.OfficeBranch SET IsHeadOffice=0 WHERE OrganizationId=@OrganizationId AND IsHeadOffice=1;
  INSERT dbo.OfficeBranch(OrganizationId,Code,Name,Address,IsHeadOffice,IsActive,CreatedAtUtc) VALUES(@OrganizationId,@Code,@Name,@Address,@IsHeadOffice,@IsActive,SYSUTCDATETIME());
  DECLARE @Id int=SCOPE_IDENTITY();
  DECLARE @New nvarchar(max)=(SELECT OrganizationId,Code,Name,Address,IsHeadOffice,IsActive FROM dbo.OfficeBranch WHERE Id=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
  INSERT dbo.AuditLog(EntityName,Action,EntityKey,ChangedColumns,NewValues,ChangedByUserId,ChangedBy,TraceId,IpAddress,ChangedAtUtc)
  VALUES(N'OfficeBranch',N'INSERT',CONCAT(N'{"Id":',@Id,N'}'),N'["OrganizationId","Code","Name","Address","IsHeadOffice","IsActive"]',@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,SYSUTCDATETIME());
  COMMIT; EXEC dbo.SPOfficeBranchGetById @Id;
END;




GO
CREATE OR ALTER PROCEDURE dbo.SPOfficeBranchUpd
  @Id int,@OrganizationId int,@Code nvarchar(20),@Name nvarchar(150),@Address nvarchar(500)=NULL,@IsHeadOffice bit=0,@IsActive bit=1,
  @ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
AS
BEGIN
  SET NOCOUNT ON; SET XACT_ABORT ON; IF NOT EXISTS(SELECT 1 FROM dbo.OfficeBranch WHERE Id=@Id) RETURN;
  SET @Code=UPPER(LTRIM(RTRIM(@Code))); SET @Name=LTRIM(RTRIM(@Name)); SET @Address=NULLIF(LTRIM(RTRIM(@Address)),N'');
  IF NOT EXISTS(SELECT 1 FROM dbo.Organization WHERE Id=@OrganizationId) THROW 50040,'Organization was not found.',1;
  IF @IsActive=1 AND NOT EXISTS(SELECT 1 FROM dbo.Organization WHERE Id=@OrganizationId AND IsActive=1) THROW 50041,'An active branch cannot belong to an inactive organization.',1;
  IF NULLIF(@Code,N'') IS NULL THROW 50020,'Code is required.',1; IF NULLIF(@Name,N'') IS NULL THROW 50021,'Name is required.',1;
  IF @IsHeadOffice=1 AND @IsActive=0 THROW 50022,'The head office must be active.',1;
  IF EXISTS(SELECT 1 FROM dbo.OfficeBranch WHERE Code=@Code AND Id<>@Id) THROW 50023,'An office branch with this code already exists.',1;
  IF @IsActive=0 AND EXISTS(SELECT 1 FROM dbo.Department WHERE OfficeBranchId=@Id AND IsActive=1) THROW 50025,'Disable the branch''s active departments before disabling the branch.',1;
  IF EXISTS(SELECT 1 FROM dbo.OfficeBranch WHERE Id=@Id AND IsHeadOffice=1 AND IsActive=1) AND (@IsHeadOffice=0 OR @IsActive=0) THROW 50026,'Assign another active branch as head office before disabling this one.',1;
  DECLARE @Old nvarchar(max)=(SELECT OrganizationId,Code,Name,Address,IsHeadOffice,IsActive FROM dbo.OfficeBranch WHERE Id=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
  BEGIN TRANSACTION;
  IF @IsHeadOffice=1 UPDATE dbo.OfficeBranch SET IsHeadOffice=0 WHERE OrganizationId=@OrganizationId AND Id<>@Id;
  UPDATE dbo.OfficeBranch SET OrganizationId=@OrganizationId,Code=@Code,Name=@Name,Address=@Address,IsHeadOffice=@IsHeadOffice,IsActive=@IsActive WHERE Id=@Id;
  DECLARE @New nvarchar(max)=(SELECT OrganizationId,Code,Name,Address,IsHeadOffice,IsActive FROM dbo.OfficeBranch WHERE Id=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
  INSERT dbo.AuditLog(EntityName,Action,EntityKey,ChangedColumns,OldValues,NewValues,ChangedByUserId,ChangedBy,TraceId,IpAddress,ChangedAtUtc)
  VALUES(N'OfficeBranch',N'UPDATE',CONCAT(N'{"Id":',@Id,N'}'),N'["OrganizationId","Code","Name","Address","IsHeadOffice","IsActive"]',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,SYSUTCDATETIME());
  COMMIT; EXEC dbo.SPOfficeBranchGetById @Id;
END;




GO
CREATE OR ALTER PROCEDURE dbo.SPOfficeBranchDel @Id int,@ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
AS
BEGIN
  SET NOCOUNT ON; IF NOT EXISTS(SELECT 1 FROM dbo.OfficeBranch WHERE Id=@Id) BEGIN SELECT CAST(0 AS bit); RETURN; END
  IF EXISTS(SELECT 1 FROM dbo.OfficeBranch WHERE Id=@Id AND IsHeadOffice=1) THROW 50027,'The head office cannot be deactivated. Assign another head office first.',1;
  IF EXISTS(SELECT 1 FROM dbo.Department WHERE OfficeBranchId=@Id AND IsActive=1) THROW 50025,'Disable the branch''s active departments before deactivating the branch.',1;
  DECLARE @Old nvarchar(max)=(SELECT IsActive FROM dbo.OfficeBranch WHERE Id=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
  UPDATE dbo.OfficeBranch SET IsActive=0 WHERE Id=@Id;
  INSERT dbo.AuditLog(EntityName,Action,EntityKey,ChangedColumns,OldValues,NewValues,ChangedByUserId,ChangedBy,TraceId,IpAddress,ChangedAtUtc)
  VALUES(N'OfficeBranch',N'DELETE',CONCAT(N'{"Id":',@Id,N'}'),N'["IsActive"]',@Old,N'{"IsActive":false}',@ActorUserId,@ActorName,@TraceId,@IpAddress,SYSUTCDATETIME());
  SELECT CAST(1 AS bit);
END;

GO
CREATE OR ALTER PROCEDURE dbo.SPDepartmentGet
    @PageNumber int = 1, @PageSize int = 20, @SearchTerm nvarchar(200) = NULL,
    @IncludeInactive bit = 0, @OfficeBranchId int = NULL,
    @SortBy nvarchar(32) = N'name', @SortDirection varchar(4) = 'asc'
AS
BEGIN
    SET NOCOUNT ON;
    IF @PageNumber < 1 THROW 50010, 'PageNumber must be greater than zero.', 1;
    IF @PageSize < 1 OR @PageSize > 100 THROW 50011, 'PageSize must be between 1 and 100.', 1;
    SET @SearchTerm=NULLIF(LTRIM(RTRIM(@SearchTerm)),N''); SET @SortBy=LOWER(NULLIF(LTRIM(RTRIM(@SortBy)),N''));
    SET @SortDirection=LOWER(NULLIF(LTRIM(RTRIM(@SortDirection)),''));
    IF @SortBy NOT IN (N'branchname',N'code',N'name',N'status') SET @SortBy=N'name';
    IF @SortDirection NOT IN ('asc','desc') SET @SortDirection='asc';
    DECLARE @Pattern nvarchar(402)=NULL;
    IF @SearchTerm IS NOT NULL SET @Pattern=N'%'+REPLACE(REPLACE(REPLACE(REPLACE(@SearchTerm,N'\',N'\\'),N'%',N'\%'),N'_',N'\_'),N'[',N'\[')+N'%';
    SELECT COUNT_BIG(1) TotalRecords FROM dbo.Department d INNER JOIN dbo.OfficeBranch b ON b.Id=d.OfficeBranchId
    WHERE (@IncludeInactive=1 OR d.IsActive=1) AND (@OfficeBranchId IS NULL OR d.OfficeBranchId=@OfficeBranchId)
      AND (@Pattern IS NULL OR d.Code LIKE @Pattern ESCAPE N'\' OR d.Name LIKE @Pattern ESCAPE N'\' OR b.Name LIKE @Pattern ESCAPE N'\');
    SELECT d.Id,d.OfficeBranchId,b.Name BranchName,d.Code,d.Name,d.IsActive
    FROM dbo.Department d INNER JOIN dbo.OfficeBranch b ON b.Id=d.OfficeBranchId
    WHERE (@IncludeInactive=1 OR d.IsActive=1) AND (@OfficeBranchId IS NULL OR d.OfficeBranchId=@OfficeBranchId)
      AND (@Pattern IS NULL OR d.Code LIKE @Pattern ESCAPE N'\' OR d.Name LIKE @Pattern ESCAPE N'\' OR b.Name LIKE @Pattern ESCAPE N'\')
    ORDER BY
      CASE WHEN @SortBy=N'branchname' AND @SortDirection='asc' THEN b.Name END ASC,
      CASE WHEN @SortBy=N'branchname' AND @SortDirection='desc' THEN b.Name END DESC,
      CASE WHEN @SortBy=N'code' AND @SortDirection='asc' THEN d.Code END ASC,
      CASE WHEN @SortBy=N'code' AND @SortDirection='desc' THEN d.Code END DESC,
      CASE WHEN @SortBy=N'name' AND @SortDirection='asc' THEN d.Name END ASC,
      CASE WHEN @SortBy=N'name' AND @SortDirection='desc' THEN d.Name END DESC,
      CASE WHEN @SortBy=N'status' AND @SortDirection='asc' THEN d.IsActive END ASC,
      CASE WHEN @SortBy=N'status' AND @SortDirection='desc' THEN d.IsActive END DESC,d.Id
    OFFSET (CONVERT(bigint,@PageNumber)-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;

GO
CREATE OR ALTER PROCEDURE dbo.SPDepartmentIns @OfficeBranchId int,@Code nvarchar(20),@Name nvarchar(150),@IsActive bit=1,
  @ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
AS
BEGIN
  SET NOCOUNT ON; SET @Code=UPPER(LTRIM(RTRIM(@Code))); SET @Name=LTRIM(RTRIM(@Name));
  IF NULLIF(@Code,N'') IS NULL THROW 50020,'Code is required.',1; IF NULLIF(@Name,N'') IS NULL THROW 50021,'Name is required.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.OfficeBranch WHERE Id=@OfficeBranchId) THROW 50030,'Office branch was not found.',1;
  IF @IsActive=1 AND NOT EXISTS(SELECT 1 FROM dbo.OfficeBranch WHERE Id=@OfficeBranchId AND IsActive=1) THROW 50031,'An active department cannot belong to an inactive branch.',1;
  IF EXISTS(SELECT 1 FROM dbo.Department WHERE OfficeBranchId=@OfficeBranchId AND Code=@Code) THROW 50032,'This department code already exists in the selected branch.',1;
  INSERT dbo.Department(OfficeBranchId,Code,Name,IsActive,CreatedAtUtc) VALUES(@OfficeBranchId,@Code,@Name,@IsActive,SYSUTCDATETIME()); DECLARE @Id int=SCOPE_IDENTITY();
  DECLARE @New nvarchar(max)=(SELECT OfficeBranchId,Code,Name,IsActive FROM dbo.Department WHERE Id=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
  INSERT dbo.AuditLog(EntityName,Action,EntityKey,ChangedColumns,NewValues,ChangedByUserId,ChangedBy,TraceId,IpAddress,ChangedAtUtc)
  VALUES(N'Department',N'INSERT',CONCAT(N'{"Id":',@Id,N'}'),N'["OfficeBranchId","Code","Name","IsActive"]',@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,SYSUTCDATETIME()); EXEC dbo.SPDepartmentGetById @Id;
END;




GO
CREATE OR ALTER PROCEDURE dbo.SPDepartmentUpd @Id int,@OfficeBranchId int,@Code nvarchar(20),@Name nvarchar(150),@IsActive bit=1,
  @ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
AS
BEGIN
  SET NOCOUNT ON; IF NOT EXISTS(SELECT 1 FROM dbo.Department WHERE Id=@Id) RETURN; SET @Code=UPPER(LTRIM(RTRIM(@Code))); SET @Name=LTRIM(RTRIM(@Name));
  IF NULLIF(@Code,N'') IS NULL THROW 50020,'Code is required.',1; IF NULLIF(@Name,N'') IS NULL THROW 50021,'Name is required.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.OfficeBranch WHERE Id=@OfficeBranchId) THROW 50030,'Office branch was not found.',1;
  IF @IsActive=1 AND NOT EXISTS(SELECT 1 FROM dbo.OfficeBranch WHERE Id=@OfficeBranchId AND IsActive=1) THROW 50031,'An active department cannot belong to an inactive branch.',1;
  IF EXISTS(SELECT 1 FROM dbo.Department WHERE OfficeBranchId=@OfficeBranchId AND Code=@Code AND Id<>@Id) THROW 50032,'This department code already exists in the selected branch.',1;
  DECLARE @Old nvarchar(max)=(SELECT OfficeBranchId,Code,Name,IsActive FROM dbo.Department WHERE Id=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
  UPDATE dbo.Department SET OfficeBranchId=@OfficeBranchId,Code=@Code,Name=@Name,IsActive=@IsActive WHERE Id=@Id;
  DECLARE @New nvarchar(max)=(SELECT OfficeBranchId,Code,Name,IsActive FROM dbo.Department WHERE Id=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
  INSERT dbo.AuditLog(EntityName,Action,EntityKey,ChangedColumns,OldValues,NewValues,ChangedByUserId,ChangedBy,TraceId,IpAddress,ChangedAtUtc)
  VALUES(N'Department',N'UPDATE',CONCAT(N'{"Id":',@Id,N'}'),N'["OfficeBranchId","Code","Name","IsActive"]',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,SYSUTCDATETIME()); EXEC dbo.SPDepartmentGetById @Id;
END;




GO
CREATE OR ALTER PROCEDURE dbo.SPDepartmentDel @Id int,@ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
AS
BEGIN
  SET NOCOUNT ON; IF NOT EXISTS(SELECT 1 FROM dbo.Department WHERE Id=@Id) BEGIN SELECT CAST(0 AS bit); RETURN; END
  IF EXISTS(SELECT 1 FROM dbo.Users WHERE DepartmentId=@Id AND IsActive=1) THROW 50033,'Reassign or deactivate active users in this department first.',1;
  DECLARE @Old nvarchar(max)=(SELECT IsActive FROM dbo.Department WHERE Id=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
  UPDATE dbo.Department SET IsActive=0 WHERE Id=@Id;
  INSERT dbo.AuditLog(EntityName,Action,EntityKey,ChangedColumns,OldValues,NewValues,ChangedByUserId,ChangedBy,TraceId,IpAddress,ChangedAtUtc)
  VALUES(N'Department',N'DELETE',CONCAT(N'{"Id":',@Id,N'}'),N'["IsActive"]',@Old,N'{"IsActive":false}',@ActorUserId,@ActorName,@TraceId,@IpAddress,SYSUTCDATETIME()); SELECT CAST(1 AS bit);
END;

GO
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260901140000_AddOrganizationAdministration', N'10.0.9');

COMMIT;
GO


