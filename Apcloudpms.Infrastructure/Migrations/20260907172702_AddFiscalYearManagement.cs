using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalYearManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FiscalYear",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYear", x => x.Id);
                    table.CheckConstraint("CK_FiscalYear_ClosedNotActive", "[IsClosed] = 0 OR [IsActive] = 0");
                    table.CheckConstraint("CK_FiscalYear_DateRange", "[EndDate] >= [StartDate]");
                });

            migrationBuilder.CreateTable(
                name: "FiscalYearSetting",
                schema: "dbo",
                columns: table => new
                {
                    FiscalYearId = table.Column<int>(type: "int", nullable: false),
                    RaPrefix = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaPrefix = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NextRaNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NextPaNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYearSetting", x => x.FiscalYearId);
                    table.ForeignKey(
                        name: "FK_FiscalYearSetting_FiscalYear_FiscalYearId",
                        column: x => x.FiscalYearId,
                        principalSchema: "dbo",
                        principalTable: "FiscalYear",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYear_DisplayName",
                schema: "dbo",
                table: "FiscalYear",
                column: "DisplayName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYear_IsActive",
                schema: "dbo",
                table: "FiscalYear",
                column: "IsActive",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYear_StartDate_EndDate",
                schema: "dbo",
                table: "FiscalYear",
                columns: new[] { "StartDate", "EndDate" },
                unique: true);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SPFiscalYearGet
                    @PageNumber int = 1, @PageSize int = 20, @SearchTerm nvarchar(200) = NULL,
                    @IncludeInactive bit = 0, @SortBy nvarchar(32) = N'startdate', @SortDirection varchar(4) = 'desc'
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF @PageNumber < 1 THROW 50200, 'PageNumber must be greater than zero.', 1;
                    IF @PageSize < 1 OR @PageSize > 100 THROW 50201, 'PageSize must be between 1 and 100.', 1;
                    SET @SearchTerm=NULLIF(LTRIM(RTRIM(@SearchTerm)),N''); SET @SortBy=LOWER(NULLIF(LTRIM(RTRIM(@SortBy)),N''));
                    SET @SortDirection=LOWER(NULLIF(LTRIM(RTRIM(@SortDirection)),''));
                    IF @SortBy NOT IN (N'displayname',N'startdate',N'enddate',N'raprefix',N'paprefix',N'nextranumber',N'nextpanumber',N'status') SET @SortBy=N'startdate';
                    IF @SortDirection NOT IN ('asc','desc') SET @SortDirection='desc';
                    DECLARE @Pattern nvarchar(402)=NULL;
                    IF @SearchTerm IS NOT NULL SET @Pattern=N'%'+REPLACE(REPLACE(REPLACE(REPLACE(@SearchTerm,N'\',N'\\'),N'%',N'\%'),N'_',N'\_'),N'[',N'\[')+N'%';

                    SELECT COUNT_BIG(1) TotalRecords FROM dbo.FiscalYear fy INNER JOIN dbo.FiscalYearSetting fys ON fys.FiscalYearId=fy.Id
                    WHERE (@IncludeInactive=1 OR fy.IsActive=1) AND (@Pattern IS NULL OR fy.DisplayName LIKE @Pattern ESCAPE N'\'
                      OR fys.RaPrefix LIKE @Pattern ESCAPE N'\' OR fys.PaPrefix LIKE @Pattern ESCAPE N'\'
                      OR fys.NextRaNumber LIKE @Pattern ESCAPE N'\' OR fys.NextPaNumber LIKE @Pattern ESCAPE N'\');

                    SELECT fy.Id,fy.DisplayName,fy.StartDate,fy.EndDate,fys.RaPrefix,fys.PaPrefix,fys.NextRaNumber,fys.NextPaNumber,
                        fy.IsActive,fy.IsClosed,fy.CreatedAtUtc,fy.UpdatedAtUtc,fy.ClosedAtUtc
                    FROM dbo.FiscalYear fy INNER JOIN dbo.FiscalYearSetting fys ON fys.FiscalYearId=fy.Id
                    WHERE (@IncludeInactive=1 OR fy.IsActive=1) AND (@Pattern IS NULL OR fy.DisplayName LIKE @Pattern ESCAPE N'\'
                      OR fys.RaPrefix LIKE @Pattern ESCAPE N'\' OR fys.PaPrefix LIKE @Pattern ESCAPE N'\'
                      OR fys.NextRaNumber LIKE @Pattern ESCAPE N'\' OR fys.NextPaNumber LIKE @Pattern ESCAPE N'\')
                    ORDER BY
                      CASE WHEN @SortBy=N'displayname' AND @SortDirection='asc' THEN fy.DisplayName END ASC,
                      CASE WHEN @SortBy=N'displayname' AND @SortDirection='desc' THEN fy.DisplayName END DESC,
                      CASE WHEN @SortBy=N'startdate' AND @SortDirection='asc' THEN fy.StartDate END ASC,
                      CASE WHEN @SortBy=N'startdate' AND @SortDirection='desc' THEN fy.StartDate END DESC,
                      CASE WHEN @SortBy=N'enddate' AND @SortDirection='asc' THEN fy.EndDate END ASC,
                      CASE WHEN @SortBy=N'enddate' AND @SortDirection='desc' THEN fy.EndDate END DESC,
                      CASE WHEN @SortBy=N'raprefix' AND @SortDirection='asc' THEN fys.RaPrefix END ASC,
                      CASE WHEN @SortBy=N'raprefix' AND @SortDirection='desc' THEN fys.RaPrefix END DESC,
                      CASE WHEN @SortBy=N'paprefix' AND @SortDirection='asc' THEN fys.PaPrefix END ASC,
                      CASE WHEN @SortBy=N'paprefix' AND @SortDirection='desc' THEN fys.PaPrefix END DESC,
                      CASE WHEN @SortBy=N'nextranumber' AND @SortDirection='asc' THEN fys.NextRaNumber END ASC,
                      CASE WHEN @SortBy=N'nextranumber' AND @SortDirection='desc' THEN fys.NextRaNumber END DESC,
                      CASE WHEN @SortBy=N'nextpanumber' AND @SortDirection='asc' THEN fys.NextPaNumber END ASC,
                      CASE WHEN @SortBy=N'nextpanumber' AND @SortDirection='desc' THEN fys.NextPaNumber END DESC,
                      CASE WHEN @SortBy=N'status' AND @SortDirection='asc' THEN CASE WHEN fy.IsClosed=1 THEN 2 WHEN fy.IsActive=1 THEN 1 ELSE 0 END END ASC,
                      CASE WHEN @SortBy=N'status' AND @SortDirection='desc' THEN CASE WHEN fy.IsClosed=1 THEN 2 WHEN fy.IsActive=1 THEN 1 ELSE 0 END END DESC, fy.Id DESC
                    OFFSET (CONVERT(bigint,@PageNumber)-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SPFiscalYearGetById @Id int
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT fy.Id,fy.DisplayName,fy.StartDate,fy.EndDate,fys.RaPrefix,fys.PaPrefix,fys.NextRaNumber,fys.NextPaNumber,
                        fy.IsActive,fy.IsClosed,fy.CreatedAtUtc,fy.UpdatedAtUtc,fy.ClosedAtUtc
                    FROM dbo.FiscalYear fy INNER JOIN dbo.FiscalYearSetting fys ON fys.FiscalYearId=fy.Id WHERE fy.Id=@Id;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SPFiscalYearIns
                    @DisplayName nvarchar(100),@StartDate date,@EndDate date,@RaPrefix nvarchar(50),@PaPrefix nvarchar(50),
                    @NextRaNumber nvarchar(50),@NextPaNumber nvarchar(50),@IsActive bit=0,@IsClosed bit=0,
                    @ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
                    SET @DisplayName=LTRIM(RTRIM(@DisplayName)); SET @RaPrefix=LTRIM(RTRIM(@RaPrefix)); SET @PaPrefix=LTRIM(RTRIM(@PaPrefix));
                    SET @NextRaNumber=LTRIM(RTRIM(@NextRaNumber)); SET @NextPaNumber=LTRIM(RTRIM(@NextPaNumber));
                    IF NULLIF(@DisplayName,N'') IS NULL THROW 50210,'Fiscal year display name is required.',1;
                    IF @StartDate IS NULL OR @EndDate IS NULL OR @EndDate<@StartDate THROW 50211,'Fiscal year end date must be on or after its start date.',1;
                    IF NULLIF(@RaPrefix,N'') IS NULL OR NULLIF(@PaPrefix,N'') IS NULL OR NULLIF(@NextRaNumber,N'') IS NULL OR NULLIF(@NextPaNumber,N'') IS NULL THROW 50212,'All fiscal year numbering settings are required.',1;
                    IF @IsClosed=1 THROW 50213,'A new fiscal year cannot be created as closed.',1;
                    BEGIN TRANSACTION;
                    IF EXISTS(SELECT 1 FROM dbo.FiscalYear WITH(UPDLOCK,HOLDLOCK) WHERE DisplayName=@DisplayName) THROW 50214,'A fiscal year with this display name already exists.',1;
                    IF EXISTS(SELECT 1 FROM dbo.FiscalYear WITH(UPDLOCK,HOLDLOCK) WHERE @StartDate<=EndDate AND @EndDate>=StartDate) THROW 50215,'Fiscal year dates cannot overlap another fiscal year.',1;
                    IF @IsActive=1 AND EXISTS(SELECT 1 FROM dbo.FiscalYear WITH(UPDLOCK,HOLDLOCK) WHERE IsActive=1) THROW 50216,'Close the active fiscal year before activating another fiscal year.',1;
                    DECLARE @Now datetime2(0)=SYSUTCDATETIME();
                    INSERT dbo.FiscalYear(DisplayName,StartDate,EndDate,IsActive,IsClosed,CreatedAtUtc,CreatedByUserId) VALUES(@DisplayName,@StartDate,@EndDate,@IsActive,0,@Now,@ActorUserId);
                    DECLARE @Id int=SCOPE_IDENTITY();
                    INSERT dbo.FiscalYearSetting(FiscalYearId,RaPrefix,PaPrefix,NextRaNumber,NextPaNumber,CreatedAtUtc,CreatedByUserId) VALUES(@Id,@RaPrefix,@PaPrefix,@NextRaNumber,@NextPaNumber,@Now,@ActorUserId);
                    DECLARE @New nvarchar(max)=(SELECT fy.DisplayName,fy.StartDate,fy.EndDate,fy.IsActive,fy.IsClosed,fys.RaPrefix,fys.PaPrefix,fys.NextRaNumber,fys.NextPaNumber FROM dbo.FiscalYear fy INNER JOIN dbo.FiscalYearSetting fys ON fys.FiscalYearId=fy.Id WHERE fy.Id=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
                    INSERT dbo.AuditLog(EntityName,Action,EntityKey,ChangedColumns,NewValues,ChangedByUserId,ChangedBy,TraceId,IpAddress,ChangedAtUtc)
                    VALUES(N'FiscalYear',N'INSERT',CONCAT(N'{"Id":',@Id,N'}'),N'["DisplayName","StartDate","EndDate","IsActive","IsClosed","RaPrefix","PaPrefix","NextRaNumber","NextPaNumber"]',@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
                    COMMIT; EXEC dbo.SPFiscalYearGetById @Id;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SPFiscalYearUpd
                    @Id int,@DisplayName nvarchar(100),@StartDate date,@EndDate date,@RaPrefix nvarchar(50),@PaPrefix nvarchar(50),
                    @NextRaNumber nvarchar(50),@NextPaNumber nvarchar(50),@IsActive bit=0,@IsClosed bit=0,
                    @ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
                    SET @DisplayName=LTRIM(RTRIM(@DisplayName)); SET @RaPrefix=LTRIM(RTRIM(@RaPrefix)); SET @PaPrefix=LTRIM(RTRIM(@PaPrefix));
                    SET @NextRaNumber=LTRIM(RTRIM(@NextRaNumber)); SET @NextPaNumber=LTRIM(RTRIM(@NextPaNumber));
                    IF NULLIF(@DisplayName,N'') IS NULL THROW 50210,'Fiscal year display name is required.',1;
                    IF @StartDate IS NULL OR @EndDate IS NULL OR @EndDate<@StartDate THROW 50211,'Fiscal year end date must be on or after its start date.',1;
                    IF NULLIF(@RaPrefix,N'') IS NULL OR NULLIF(@PaPrefix,N'') IS NULL OR NULLIF(@NextRaNumber,N'') IS NULL OR NULLIF(@NextPaNumber,N'') IS NULL THROW 50212,'All fiscal year numbering settings are required.',1;
                    IF @IsActive=1 AND @IsClosed=1 THROW 50217,'A closed fiscal year cannot be active.',1;
                    BEGIN TRANSACTION;
                    DECLARE @WasActive bit,@WasClosed bit;
                    SELECT @WasActive=IsActive,@WasClosed=IsClosed FROM dbo.FiscalYear WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id;
                    IF @WasClosed IS NULL BEGIN ROLLBACK; RETURN; END;
                    IF @WasClosed=1 THROW 50218,'A closed fiscal year cannot be modified.',1;
                    IF @WasActive=1 AND @IsActive=0 AND @IsClosed=0 THROW 50219,'An active fiscal year must be closed before it can be deactivated.',1;
                    IF EXISTS(SELECT 1 FROM dbo.FiscalYear WITH(UPDLOCK,HOLDLOCK) WHERE DisplayName=@DisplayName AND Id<>@Id) THROW 50214,'A fiscal year with this display name already exists.',1;
                    IF EXISTS(SELECT 1 FROM dbo.FiscalYear WITH(UPDLOCK,HOLDLOCK) WHERE Id<>@Id AND @StartDate<=EndDate AND @EndDate>=StartDate) THROW 50215,'Fiscal year dates cannot overlap another fiscal year.',1;
                    IF @IsActive=1 AND EXISTS(SELECT 1 FROM dbo.FiscalYear WITH(UPDLOCK,HOLDLOCK) WHERE IsActive=1 AND Id<>@Id) THROW 50216,'Close the active fiscal year before activating another fiscal year.',1;
                    DECLARE @Old nvarchar(max)=(SELECT fy.DisplayName,fy.StartDate,fy.EndDate,fy.IsActive,fy.IsClosed,fys.RaPrefix,fys.PaPrefix,fys.NextRaNumber,fys.NextPaNumber FROM dbo.FiscalYear fy INNER JOIN dbo.FiscalYearSetting fys ON fys.FiscalYearId=fy.Id WHERE fy.Id=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
                    DECLARE @Now datetime2(0)=SYSUTCDATETIME();
                    UPDATE dbo.FiscalYear SET DisplayName=@DisplayName,StartDate=@StartDate,EndDate=@EndDate,IsActive=CASE WHEN @IsClosed=1 THEN 0 ELSE @IsActive END,IsClosed=@IsClosed,UpdatedAtUtc=@Now,UpdatedByUserId=@ActorUserId,ClosedAtUtc=CASE WHEN @IsClosed=1 THEN @Now ELSE NULL END,ClosedByUserId=CASE WHEN @IsClosed=1 THEN @ActorUserId ELSE NULL END WHERE Id=@Id;
                    UPDATE dbo.FiscalYearSetting SET RaPrefix=@RaPrefix,PaPrefix=@PaPrefix,NextRaNumber=@NextRaNumber,NextPaNumber=@NextPaNumber,UpdatedAtUtc=@Now,UpdatedByUserId=@ActorUserId WHERE FiscalYearId=@Id;
                    DECLARE @New nvarchar(max)=(SELECT fy.DisplayName,fy.StartDate,fy.EndDate,fy.IsActive,fy.IsClosed,fys.RaPrefix,fys.PaPrefix,fys.NextRaNumber,fys.NextPaNumber FROM dbo.FiscalYear fy INNER JOIN dbo.FiscalYearSetting fys ON fys.FiscalYearId=fy.Id WHERE fy.Id=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
                    INSERT dbo.AuditLog(EntityName,Action,EntityKey,ChangedColumns,OldValues,NewValues,ChangedByUserId,ChangedBy,TraceId,IpAddress,ChangedAtUtc)
                    VALUES(N'FiscalYear',CASE WHEN @IsClosed=1 THEN N'CLOSE' ELSE N'UPDATE' END,CONCAT(N'{"Id":',@Id,N'}'),N'["DisplayName","StartDate","EndDate","IsActive","IsClosed","RaPrefix","PaPrefix","NextRaNumber","NextPaNumber"]',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
                    COMMIT; EXEC dbo.SPFiscalYearGetById @Id;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SPFiscalYearDel
                    @Id int,@ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON; BEGIN TRANSACTION;
                    DECLARE @IsActive bit,@IsClosed bit;
                    SELECT @IsActive=IsActive,@IsClosed=IsClosed FROM dbo.FiscalYear WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id;
                    IF @IsActive IS NULL BEGIN ROLLBACK; SELECT CAST(0 AS bit); RETURN; END;
                    IF @IsActive=1 THROW 50220,'An active fiscal year cannot be deleted. Close it first.',1;
                    IF @IsClosed=1 THROW 50221,'A closed fiscal year cannot be deleted because it is part of the financial history.',1;
                    DECLARE @Old nvarchar(max)=(SELECT fy.DisplayName,fy.StartDate,fy.EndDate,fy.IsActive,fy.IsClosed,fys.RaPrefix,fys.PaPrefix,fys.NextRaNumber,fys.NextPaNumber FROM dbo.FiscalYear fy INNER JOIN dbo.FiscalYearSetting fys ON fys.FiscalYearId=fy.Id WHERE fy.Id=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
                    DELETE dbo.FiscalYear WHERE Id=@Id;
                    INSERT dbo.AuditLog(EntityName,Action,EntityKey,ChangedColumns,OldValues,ChangedByUserId,ChangedBy,TraceId,IpAddress,ChangedAtUtc)
                    VALUES(N'FiscalYear',N'DELETE',CONCAT(N'{"Id":',@Id,N'}'),N'["DisplayName","StartDate","EndDate","IsActive","IsClosed","RaPrefix","PaPrefix","NextRaNumber","NextPaNumber"]',@Old,@ActorUserId,@ActorName,@TraceId,@IpAddress,SYSUTCDATETIME());
                    COMMIT; SELECT CAST(1 AS bit);
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPFiscalYearDel;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPFiscalYearUpd;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPFiscalYearIns;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPFiscalYearGetById;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPFiscalYearGet;");

            migrationBuilder.DropTable(
                name: "FiscalYearSetting",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FiscalYear",
                schema: "dbo");
        }
    }
}
