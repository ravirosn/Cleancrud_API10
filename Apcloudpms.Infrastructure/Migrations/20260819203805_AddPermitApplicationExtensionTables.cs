using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPermitApplicationExtensionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PermitApplicationConfinedSpace",
                schema: "dbo",
                columns: table => new
                {
                    PermitApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    WorkingInConfinedSpaceListItemId = table.Column<int>(type: "int", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermitApplicationConfinedSpace", x => new { x.PermitApplicationId, x.WorkingInConfinedSpaceListItemId });
                    table.ForeignKey(
                        name: "FK_PermitApplicationConfinedSpace_ListItem_WorkingInConfinedSpaceListItemId",
                        column: x => x.WorkingInConfinedSpaceListItemId,
                        principalSchema: "dbo",
                        principalTable: "ListItem",
                        principalColumn: "ListItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermitApplicationConfinedSpace_PermitApplication_PermitApplicationId",
                        column: x => x.PermitApplicationId,
                        principalSchema: "dbo",
                        principalTable: "PermitApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PermitApplicationInspectionPriorToComm",
                schema: "dbo",
                columns: table => new
                {
                    PermitApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    InspectionPriorToCommListItemId = table.Column<int>(type: "int", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermitApplicationInspectionPriorToComm", x => new { x.PermitApplicationId, x.InspectionPriorToCommListItemId });
                    table.ForeignKey(
                        name: "FK_PermitApplicationInspectionPriorToComm_ListItem_InspectionPriorToCommListItemId",
                        column: x => x.InspectionPriorToCommListItemId,
                        principalSchema: "dbo",
                        principalTable: "ListItem",
                        principalColumn: "ListItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermitApplicationInspectionPriorToComm_PermitApplication_PermitApplicationId",
                        column: x => x.PermitApplicationId,
                        principalSchema: "dbo",
                        principalTable: "PermitApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PermitApplicationWallWorks",
                schema: "dbo",
                columns: table => new
                {
                    PermitApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    WorksonWallListItemId = table.Column<int>(type: "int", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermitApplicationWallWorks", x => new { x.PermitApplicationId, x.WorksonWallListItemId });
                    table.ForeignKey(
                        name: "FK_PermitApplicationWallWorks_ListItem_WorksonWallListItemId",
                        column: x => x.WorksonWallListItemId,
                        principalSchema: "dbo",
                        principalTable: "ListItem",
                        principalColumn: "ListItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermitApplicationWallWorks_PermitApplication_PermitApplicationId",
                        column: x => x.PermitApplicationId,
                        principalSchema: "dbo",
                        principalTable: "PermitApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PermitApplicationConfinedSpace_WorkingInConfinedSpaceListItemId",
                schema: "dbo",
                table: "PermitApplicationConfinedSpace",
                column: "WorkingInConfinedSpaceListItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApplicationInspectionPriorToComm_InspectionPriorToCommListItemId",
                schema: "dbo",
                table: "PermitApplicationInspectionPriorToComm",
                column: "InspectionPriorToCommListItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApplicationWallWorks_WorksonWallListItemId",
                schema: "dbo",
                table: "PermitApplicationWallWorks",
                column: "WorksonWallListItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermitApplicationConfinedSpace",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PermitApplicationInspectionPriorToComm",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PermitApplicationWallWorks",
                schema: "dbo");
        }
    }
}
