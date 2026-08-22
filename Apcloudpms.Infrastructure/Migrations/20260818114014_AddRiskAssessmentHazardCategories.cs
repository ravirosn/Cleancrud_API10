using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskAssessmentHazardCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RiskAssessmentHazardCategories",
                schema: "dbo",
                columns: table => new
                {
                    RiskAssessmentId = table.Column<int>(type: "int", nullable: false),
                    HazardCategoriesListItemId = table.Column<int>(type: "int", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessmentHazardCategories", x => new { x.RiskAssessmentId, x.HazardCategoriesListItemId });
                    table.ForeignKey(
                        name: "FK_RiskAssessmentHazardCategories_ListItem_HazardCategoriesListItemId",
                        column: x => x.HazardCategoriesListItemId,
                        principalSchema: "dbo",
                        principalTable: "ListItem",
                        principalColumn: "ListItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskAssessmentHazardCategories_RiskAssessment_RiskAssessmentId",
                        column: x => x.RiskAssessmentId,
                        principalSchema: "dbo",
                        principalTable: "RiskAssessment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentHazardCategories_HazardCategoriesListItemId",
                schema: "dbo",
                table: "RiskAssessmentHazardCategories",
                column: "HazardCategoriesListItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RiskAssessmentHazardCategories",
                schema: "dbo");
        }
    }
}
