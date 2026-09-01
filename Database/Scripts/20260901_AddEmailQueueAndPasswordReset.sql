BEGIN TRANSACTION;
CREATE TABLE [dbo].[EmailQueue] (
    [Id] bigint NOT NULL IDENTITY,
    [ToEmail] nvarchar(320) NOT NULL,
    [ToName] nvarchar(200) NULL,
    [Subject] nvarchar(500) NOT NULL,
    [TextBody] nvarchar(max) NULL,
    [HtmlBody] nvarchar(max) NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Pending',
    [AttemptCount] int NOT NULL DEFAULT 0,
    [MaxAttempts] int NOT NULL DEFAULT 5,
    [NextAttemptAtUtc] datetime2(0) NOT NULL,
    [LockToken] uniqueidentifier NULL,
    [LockedUntilUtc] datetime2(0) NULL,
    [SentAtUtc] datetime2(0) NULL,
    [LastError] nvarchar(2000) NULL,
    [CorrelationId] nvarchar(100) NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL,
    [UpdatedAtUtc] datetime2(0) NULL,
    CONSTRAINT [PK_EmailQueue] PRIMARY KEY ([Id])
);

CREATE TABLE [dbo].[PasswordResetToken] (
    [Id] bigint NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [TokenHash] char(64) NOT NULL,
    [CreatedAtUtc] datetime2(0) NOT NULL,
    [ExpiresAtUtc] datetime2(0) NOT NULL,
    [UsedAtUtc] datetime2(0) NULL,
    [RevokedAtUtc] datetime2(0) NULL,
    [RequestedByIp] nvarchar(45) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_PasswordResetToken] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PasswordResetToken_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_EmailQueue_Status_NextAttemptAtUtc_CreatedAtUtc] ON [dbo].[EmailQueue] ([Status], [NextAttemptAtUtc], [CreatedAtUtc]);

CREATE INDEX [IX_EmailQueue_LockToken] ON [dbo].[EmailQueue] ([LockToken]) WHERE [LockToken] IS NOT NULL;

CREATE INDEX [IX_EmailQueue_CorrelationId] ON [dbo].[EmailQueue] ([CorrelationId]) WHERE [CorrelationId] IS NOT NULL;

CREATE UNIQUE INDEX [IX_PasswordResetToken_TokenHash] ON [dbo].[PasswordResetToken] ([TokenHash]) WHERE [TokenHash] IS NOT NULL;

CREATE INDEX [IX_PasswordResetToken_UserId_ExpiresAtUtc] ON [dbo].[PasswordResetToken] ([UserId], [ExpiresAtUtc]);

ALTER TABLE [dbo].[EmailQueue] ADD CONSTRAINT [CK_EmailQueue_Status] CHECK ([Status] IN (N'Pending',N'Processing',N'Failed',N'Sent',N'DeadLetter'));

ALTER TABLE [dbo].[EmailQueue] ADD CONSTRAINT [CK_EmailQueue_Attempts] CHECK ([AttemptCount] >= 0 AND [MaxAttempts] BETWEEN 1 AND 20);

ALTER TABLE [dbo].[EmailQueue] ADD CONSTRAINT [CK_EmailQueue_Body] CHECK ([Status] = N'Sent' OR [TextBody] IS NOT NULL OR [HtmlBody] IS NOT NULL);

GO
CREATE OR ALTER PROCEDURE dbo.SPEmailQueueClaim @BatchSize int = 20, @LeaseSeconds int = 300
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    SET @BatchSize = CASE WHEN @BatchSize < 1 THEN 1 WHEN @BatchSize > 100 THEN 100 ELSE @BatchSize END;
    SET @LeaseSeconds = CASE WHEN @LeaseSeconds < 30 THEN 30 WHEN @LeaseSeconds > 3600 THEN 3600 ELSE @LeaseSeconds END;
    DECLARE @Now datetime2(0) = SYSUTCDATETIME(), @LockToken uniqueidentifier = NEWID();
    ;WITH Claimable AS
    (
        SELECT TOP (@BatchSize) * FROM dbo.EmailQueue WITH (UPDLOCK, READPAST, ROWLOCK)
        WHERE AttemptCount < MaxAttempts AND
            (((Status = N'Pending' OR Status = N'Failed') AND NextAttemptAtUtc <= @Now)
             OR (Status = N'Processing' AND LockedUntilUtc <= @Now))
        ORDER BY NextAttemptAtUtc, CreatedAtUtc, Id
    )
    UPDATE Claimable SET Status = N'Processing', AttemptCount = AttemptCount + 1,
        LockToken = @LockToken, LockedUntilUtc = DATEADD(SECOND, @LeaseSeconds, @Now), UpdatedAtUtc = @Now
    OUTPUT inserted.Id, inserted.LockToken, inserted.ToEmail, inserted.ToName, inserted.Subject,
        inserted.HtmlBody, inserted.TextBody, inserted.AttemptCount, inserted.MaxAttempts;
END;

GO
CREATE OR ALTER PROCEDURE dbo.SPEmailQueueMarkSent @Id bigint, @LockToken uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.EmailQueue SET Status = N'Sent', SentAtUtc = SYSUTCDATETIME(), LastError = NULL,
        TextBody = NULL, HtmlBody = NULL, LockToken = NULL, LockedUntilUtc = NULL, UpdatedAtUtc = SYSUTCDATETIME()
    WHERE Id = @Id AND Status = N'Processing' AND LockToken = @LockToken;
END;

GO
CREATE OR ALTER PROCEDURE dbo.SPEmailQueueMarkFailed @Id bigint, @LockToken uniqueidentifier, @Error nvarchar(2000)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.EmailQueue SET Status = CASE WHEN AttemptCount >= MaxAttempts THEN N'DeadLetter' ELSE N'Failed' END,
        LastError = LEFT(COALESCE(@Error, N'Unknown email delivery error.'), 2000),
        NextAttemptAtUtc = DATEADD(SECOND, 30 * POWER(CONVERT(float, 2), CASE WHEN AttemptCount > 8 THEN 8 ELSE AttemptCount END), SYSUTCDATETIME()),
        LockToken = NULL, LockedUntilUtc = NULL, UpdatedAtUtc = SYSUTCDATETIME()
    WHERE Id = @Id AND Status = N'Processing' AND LockToken = @LockToken;
END;

GO
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260901160000_AddEmailQueueAndPasswordReset', N'10.0.9');

COMMIT;
GO

