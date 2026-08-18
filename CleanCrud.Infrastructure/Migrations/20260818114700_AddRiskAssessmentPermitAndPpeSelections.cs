using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCrud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskAssessmentPermitAndPpeSelections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RiskAssessmentAdditionalPPE",
                schema: "dbo",
                columns: table => new
                {
                    RiskAssessmentId = table.Column<int>(type: "int", nullable: false),
                    AdditionalProtectiveMeasuresListItemId = table.Column<int>(type: "int", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessmentAdditionalPPE", x => new { x.RiskAssessmentId, x.AdditionalProtectiveMeasuresListItemId });
                    table.ForeignKey(
                        name: "FK_RiskAssessmentAdditionalPPE_ListItem_AdditionalProtectiveMeasuresListItemId",
                        column: x => x.AdditionalProtectiveMeasuresListItemId,
                        principalSchema: "dbo",
                        principalTable: "ListItem",
                        principalColumn: "ListItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskAssessmentAdditionalPPE_RiskAssessment_RiskAssessmentId",
                        column: x => x.RiskAssessmentId,
                        principalSchema: "dbo",
                        principalTable: "RiskAssessment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiskAssessmentPPE",
                schema: "dbo",
                columns: table => new
                {
                    RiskAssessmentId = table.Column<int>(type: "int", nullable: false),
                    SpecialPermitListItemId = table.Column<int>(type: "int", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessmentPPE", x => new { x.RiskAssessmentId, x.SpecialPermitListItemId });
                    table.ForeignKey(
                        name: "FK_RiskAssessmentPPE_ListItem_SpecialPermitListItemId",
                        column: x => x.SpecialPermitListItemId,
                        principalSchema: "dbo",
                        principalTable: "ListItem",
                        principalColumn: "ListItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskAssessmentPPE_RiskAssessment_RiskAssessmentId",
                        column: x => x.RiskAssessmentId,
                        principalSchema: "dbo",
                        principalTable: "RiskAssessment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiskAssessmentSpecialPermit",
                schema: "dbo",
                columns: table => new
                {
                    RiskAssessmentId = table.Column<int>(type: "int", nullable: false),
                    SpecialPermitListItemId = table.Column<int>(type: "int", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessmentSpecialPermit", x => new { x.RiskAssessmentId, x.SpecialPermitListItemId });
                    table.ForeignKey(
                        name: "FK_RiskAssessmentSpecialPermit_ListItem_SpecialPermitListItemId",
                        column: x => x.SpecialPermitListItemId,
                        principalSchema: "dbo",
                        principalTable: "ListItem",
                        principalColumn: "ListItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskAssessmentSpecialPermit_RiskAssessment_RiskAssessmentId",
                        column: x => x.RiskAssessmentId,
                        principalSchema: "dbo",
                        principalTable: "RiskAssessment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentAdditionalPPE_AdditionalProtectiveMeasuresListItemId",
                schema: "dbo",
                table: "RiskAssessmentAdditionalPPE",
                column: "AdditionalProtectiveMeasuresListItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentPPE_SpecialPermitListItemId",
                schema: "dbo",
                table: "RiskAssessmentPPE",
                column: "SpecialPermitListItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentSpecialPermit_SpecialPermitListItemId",
                schema: "dbo",
                table: "RiskAssessmentSpecialPermit",
                column: "SpecialPermitListItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RiskAssessmentAdditionalPPE",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RiskAssessmentPPE",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RiskAssessmentSpecialPermit",
                schema: "dbo");
        }
    }
}
