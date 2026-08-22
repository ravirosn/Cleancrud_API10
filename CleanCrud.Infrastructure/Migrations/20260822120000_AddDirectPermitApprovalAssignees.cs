using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCrud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectPermitApprovalAssignees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "dbo",
                table: "PermitApproval",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "PermitApprovalAssignee",
                schema: "dbo",
                columns: table => new
                {
                    PermitApprovalId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedByUserId = table.Column<int>(type: "int", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    RevokedByUserId = table.Column<int>(type: "int", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermitApprovalAssignee", x => new { x.PermitApprovalId, x.UserId });
                    table.ForeignKey(
                        name: "FK_PermitApprovalAssignee_PermitApproval_PermitApprovalId",
                        column: x => x.PermitApprovalId,
                        principalSchema: "dbo",
                        principalTable: "PermitApproval",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PermitApprovalAssignee_Users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermitApprovalAssignee_Users_RevokedByUserId",
                        column: x => x.RevokedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermitApprovalAssignee_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PermitApprovalAssignee_AssignedByUserId",
                schema: "dbo",
                table: "PermitApprovalAssignee",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApprovalAssignee_RevokedByUserId",
                schema: "dbo",
                table: "PermitApprovalAssignee",
                column: "RevokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApprovalAssignee_UserId_IsActive_PermitApprovalId",
                schema: "dbo",
                table: "PermitApprovalAssignee",
                columns: new[] { "UserId", "IsActive", "PermitApprovalId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermitApprovalAssignee",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "dbo",
                table: "PermitApproval");
        }
    }
}
