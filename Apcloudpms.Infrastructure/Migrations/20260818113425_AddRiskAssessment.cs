using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RiskAssessment",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PreRiskAssessmentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PermitIssuerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PermitIssuerContact = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PermitReceiverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PermitReceiverContact = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AreaResponsibleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AreaResponsibleContact = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LocationOfWork = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DescriptionOfWork = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecialInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlannedStartDateTime = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    PlannedEndDateTime = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessment", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RiskAssessment",
                schema: "dbo");
        }
    }
}
