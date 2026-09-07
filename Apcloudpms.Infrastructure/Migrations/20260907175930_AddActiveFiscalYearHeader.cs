using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveFiscalYearHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SPFiscalYearGetActive
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT TOP (1) Id,DisplayName,StartDate,EndDate
                    FROM dbo.FiscalYear
                    WHERE IsActive=1 AND IsClosed=0;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPFiscalYearGetActive;");
        }
    }
}
