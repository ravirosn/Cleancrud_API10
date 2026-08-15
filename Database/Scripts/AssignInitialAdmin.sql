SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @NormalizedUserName nvarchar(100) = N'CHANGE_ME';
DECLARE @UserId int;
DECLARE @AdminRoleId int;

SELECT @UserId = [Id]
FROM [dbo].[Users]
WHERE [NormalizedUserName] = UPPER(LTRIM(RTRIM(@NormalizedUserName)));

SELECT @AdminRoleId = [Id]
FROM [dbo].[Role]
WHERE [NormalizedName] = N'ADMIN' AND [IsActive] = 1;

IF @UserId IS NULL
    THROW 51000, 'The requested user does not exist.', 1;
IF @AdminRoleId IS NULL
    THROW 51000, 'The active Admin role does not exist.', 1;

IF EXISTS (
    SELECT 1 FROM [dbo].[UserRole]
    WHERE [UserId] = @UserId AND [RoleId] = @AdminRoleId)
BEGIN
    UPDATE [dbo].[UserRole]
    SET [IsActive] = 1, [AssignedAtUtc] = SYSUTCDATETIME()
    WHERE [UserId] = @UserId AND [RoleId] = @AdminRoleId;
END
ELSE
BEGIN
    INSERT INTO [dbo].[UserRole] ([UserId], [RoleId], [IsActive], [AssignedAtUtc])
    VALUES (@UserId, @AdminRoleId, 1, SYSUTCDATETIME());
END;
