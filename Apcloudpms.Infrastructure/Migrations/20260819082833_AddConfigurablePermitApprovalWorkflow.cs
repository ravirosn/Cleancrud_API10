using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurablePermitApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalWorkflow",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermitTypeListItemId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflow_ListItem_PermitTypeListItemId",
                        column: x => x.PermitTypeListItemId,
                        principalSchema: "dbo",
                        principalTable: "ListItem",
                        principalColumn: "ListItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflow_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PermitApproval",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermitApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    LevelNumber = table.Column<byte>(type: "tinyint", nullable: false),
                    PrimaryApproverRoleId = table.Column<int>(type: "int", nullable: false),
                    AlternateApproverRoleId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActionedByUserId = table.Column<int>(type: "int", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    ActionedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermitApproval", x => x.Id);
                    table.CheckConstraint("CK_PermitApproval_LevelNumber", "[LevelNumber] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_PermitApproval_PermitApplication_PermitApplicationId",
                        column: x => x.PermitApplicationId,
                        principalSchema: "dbo",
                        principalTable: "PermitApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermitApproval_Role_AlternateApproverRoleId",
                        column: x => x.AlternateApproverRoleId,
                        principalSchema: "dbo",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermitApproval_Role_PrimaryApproverRoleId",
                        column: x => x.PrimaryApproverRoleId,
                        principalSchema: "dbo",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermitApproval_Users_ActionedByUserId",
                        column: x => x.ActionedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowLevel",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovalWorkflowId = table.Column<int>(type: "int", nullable: false),
                    LevelNumber = table.Column<byte>(type: "tinyint", nullable: false),
                    PrimaryApproverRoleId = table.Column<int>(type: "int", nullable: false),
                    AlternateApproverRoleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowLevel", x => x.Id);
                    table.CheckConstraint("CK_ApprovalWorkflowLevel_LevelNumber", "[LevelNumber] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowLevel_ApprovalWorkflow_ApprovalWorkflowId",
                        column: x => x.ApprovalWorkflowId,
                        principalSchema: "dbo",
                        principalTable: "ApprovalWorkflow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowLevel_Role_AlternateApproverRoleId",
                        column: x => x.AlternateApproverRoleId,
                        principalSchema: "dbo",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowLevel_Role_PrimaryApproverRoleId",
                        column: x => x.PrimaryApproverRoleId,
                        principalSchema: "dbo",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalNotification",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermitApprovalId = table.Column<long>(type: "bigint", nullable: false),
                    RecipientUserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalNotification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalNotification_PermitApproval_PermitApprovalId",
                        column: x => x.PermitApprovalId,
                        principalSchema: "dbo",
                        principalTable: "PermitApproval",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalNotification_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotification_PermitApprovalId_RecipientUserId",
                schema: "dbo",
                table: "ApprovalNotification",
                columns: new[] { "PermitApprovalId", "RecipientUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotification_RecipientUserId_ReadAtUtc_CreatedAtUtc",
                schema: "dbo",
                table: "ApprovalNotification",
                columns: new[] { "RecipientUserId", "ReadAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotification_Status_CreatedAtUtc",
                schema: "dbo",
                table: "ApprovalNotification",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflow_CreatedByUserId",
                schema: "dbo",
                table: "ApprovalWorkflow",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflow_PermitTypeListItemId",
                schema: "dbo",
                table: "ApprovalWorkflow",
                column: "PermitTypeListItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowLevel_AlternateApproverRoleId",
                schema: "dbo",
                table: "ApprovalWorkflowLevel",
                column: "AlternateApproverRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowLevel_ApprovalWorkflowId_LevelNumber",
                schema: "dbo",
                table: "ApprovalWorkflowLevel",
                columns: new[] { "ApprovalWorkflowId", "LevelNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowLevel_PrimaryApproverRoleId",
                schema: "dbo",
                table: "ApprovalWorkflowLevel",
                column: "PrimaryApproverRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApproval_ActionedByUserId",
                schema: "dbo",
                table: "PermitApproval",
                column: "ActionedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApproval_AlternateApproverRoleId",
                schema: "dbo",
                table: "PermitApproval",
                column: "AlternateApproverRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApproval_PermitApplicationId_LevelNumber",
                schema: "dbo",
                table: "PermitApproval",
                columns: new[] { "PermitApplicationId", "LevelNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermitApproval_PrimaryApproverRoleId",
                schema: "dbo",
                table: "PermitApproval",
                column: "PrimaryApproverRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApproval_Status_AlternateApproverRoleId",
                schema: "dbo",
                table: "PermitApproval",
                columns: new[] { "Status", "AlternateApproverRoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_PermitApproval_Status_PrimaryApproverRoleId",
                schema: "dbo",
                table: "PermitApproval",
                columns: new[] { "Status", "PrimaryApproverRoleId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalNotification",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowLevel",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PermitApproval",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflow",
                schema: "dbo");
        }
    }
}
