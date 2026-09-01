using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260901151000_KeepOrganizationActive")]
public sealed class KeepOrganizationActive : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE dbo.SPOrganizationUpd
                @Id int, @Code nvarchar(20), @Name nvarchar(200), @Address nvarchar(500),
                @PhoneNumber nvarchar(30) = NULL, @Email nvarchar(320) = NULL,
                @Website nvarchar(500) = NULL,
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

                DECLARE @OldValues nvarchar(max) = (SELECT Code, Name, Address, PhoneNumber, Email, Website, IsActive
                    FROM dbo.Organization WHERE Id = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
                UPDATE dbo.Organization SET Code = @Code, Name = @Name, Address = @Address,
                    PhoneNumber = @PhoneNumber, Email = @Email, Website = @Website,
                    IsActive = 1, UpdatedAtUtc = SYSUTCDATETIME() WHERE Id = @Id;
                DECLARE @NewValues nvarchar(max) = (SELECT Code, Name, Address, PhoneNumber, Email, Website, IsActive
                    FROM dbo.Organization WHERE Id = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
                INSERT dbo.AuditLog(EntityName, Action, EntityKey, ChangedColumns, OldValues, NewValues,
                    ChangedByUserId, ChangedBy, TraceId, IpAddress, ChangedAtUtc)
                VALUES(N'Organization', N'UPDATE', CONCAT(N'{"Id":', @Id, N'}'),
                    N'["Code","Name","Address","PhoneNumber","Email","Website","IsActive"]',
                    @OldValues, @NewValues, @ActorUserId, @ActorName, @TraceId, @IpAddress, SYSUTCDATETIME());
                EXEC dbo.SPOrganizationGet @Id;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
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
            """);
    }
}
