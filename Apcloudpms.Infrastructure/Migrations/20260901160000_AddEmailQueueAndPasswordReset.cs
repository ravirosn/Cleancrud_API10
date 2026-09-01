using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260901160000_AddEmailQueueAndPasswordReset")]
public sealed class AddEmailQueueAndPasswordReset : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EmailQueue", schema: "dbo",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ToEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                ToName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                TextBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                HtmlBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                MaxAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                LockToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LockedUntilUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                SentAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_EmailQueue", x => x.Id));

        migrationBuilder.CreateTable(
            name: "PasswordResetToken", schema: "dbo",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<int>(type: "int", nullable: false),
                TokenHash = table.Column<string>(type: "char(64)", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                UsedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                RevokedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                RequestedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PasswordResetToken", x => x.Id);
                table.ForeignKey(
                    name: "FK_PasswordResetToken_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "dbo",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_EmailQueue_Status_NextAttemptAtUtc_CreatedAtUtc", "EmailQueue",
            new[] { "Status", "NextAttemptAtUtc", "CreatedAtUtc" }, schema: "dbo");
        migrationBuilder.CreateIndex("IX_EmailQueue_LockToken", "EmailQueue", "LockToken", schema: "dbo", filter: "[LockToken] IS NOT NULL");
        migrationBuilder.CreateIndex("IX_EmailQueue_CorrelationId", "EmailQueue", "CorrelationId", schema: "dbo", filter: "[CorrelationId] IS NOT NULL");
        migrationBuilder.CreateIndex("IX_PasswordResetToken_TokenHash", "PasswordResetToken", "TokenHash", schema: "dbo", unique: true);
        migrationBuilder.CreateIndex("IX_PasswordResetToken_UserId_ExpiresAtUtc", "PasswordResetToken", new[] { "UserId", "ExpiresAtUtc" }, schema: "dbo");
        migrationBuilder.AddCheckConstraint("CK_EmailQueue_Status", "EmailQueue",
            "[Status] IN (N'Pending',N'Processing',N'Failed',N'Sent',N'DeadLetter')", schema: "dbo");
        migrationBuilder.AddCheckConstraint("CK_EmailQueue_Attempts", "EmailQueue",
            "[AttemptCount] >= 0 AND [MaxAttempts] BETWEEN 1 AND 20", schema: "dbo");
        migrationBuilder.AddCheckConstraint("CK_EmailQueue_Body", "EmailQueue",
            "[Status] = N'Sent' OR [TextBody] IS NOT NULL OR [HtmlBody] IS NOT NULL", schema: "dbo");

        migrationBuilder.Sql("""
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
            """);
        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE dbo.SPEmailQueueMarkSent @Id bigint, @LockToken uniqueidentifier
            AS
            BEGIN
                SET NOCOUNT ON;
                UPDATE dbo.EmailQueue SET Status = N'Sent', SentAtUtc = SYSUTCDATETIME(), LastError = NULL,
                    TextBody = NULL, HtmlBody = NULL, LockToken = NULL, LockedUntilUtc = NULL, UpdatedAtUtc = SYSUTCDATETIME()
                WHERE Id = @Id AND Status = N'Processing' AND LockToken = @LockToken;
            END;
            """);
        migrationBuilder.Sql("""
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
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPEmailQueueMarkFailed;");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPEmailQueueMarkSent;");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPEmailQueueClaim;");
        migrationBuilder.DropTable("PasswordResetToken", "dbo");
        migrationBuilder.DropTable("EmailQueue", "dbo");
    }
}
