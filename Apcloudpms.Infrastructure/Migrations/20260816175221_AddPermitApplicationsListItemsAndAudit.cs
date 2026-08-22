using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace Apcloudpms.Infrastructure.Migrations
{
    public partial class AddPermitApplicationsListItemsAndAudit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLog",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntityKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedColumns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: true),
                    ChangedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_AuditLog", x => x.Id));

            migrationBuilder.AlterColumn<string>(
                name: "CategoryName",
                schema: "dbo",
                table: "ListItemCategory",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(name: "Code", schema: "dbo", table: "ListItemCategory", type: "nvarchar(50)", maxLength: 50, nullable: false);
            migrationBuilder.AddColumn<string>(name: "Description", schema: "dbo", table: "ListItemCategory", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<bool>(name: "IsActive", schema: "dbo", table: "ListItemCategory", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<DateTime>(name: "CreatedAtUtc", schema: "dbo", table: "ListItemCategory", type: "datetime2(0)", precision: 0, nullable: false, defaultValue: new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc));
            migrationBuilder.AddColumn<DateTime>(name: "UpdatedAtUtc", schema: "dbo", table: "ListItemCategory", type: "datetime2(0)", precision: 0, nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ItemName",
                schema: "dbo",
                table: "ListItem",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(name: "Description", schema: "dbo", table: "ListItem", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<int>(name: "DisplayOrder", schema: "dbo", table: "ListItem", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<DateTime>(name: "CreatedAtUtc", schema: "dbo", table: "ListItem", type: "datetime2(0)", precision: 0, nullable: false, defaultValue: new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc));
            migrationBuilder.AddColumn<DateTime>(name: "UpdatedAtUtc", schema: "dbo", table: "ListItem", type: "datetime2(0)", precision: 0, nullable: true);

            migrationBuilder.Sql("ALTER TABLE [dbo].[ListItem] DROP CONSTRAINT [UK_ListItem_SystemName];");

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "ListItemCategory",
                columns: new[] { "ListItemCategoryId", "CategoryName", "Code", "CreatedAtUtc", "Description", "IsActive" },
                values: new object[,]
                {
                    { 1, "Permit Status", "PERMIT_STATUS", new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc), "Workflow statuses for permit applications.", true },
                    { 2, "Permit Type", "PERMIT_TYPE", new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc), "Available permit application types.", true }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "ListItem",
                columns: new[] { "ListItemId", "ListItemCategoryId", "SystemName", "ItemName", "Description", "DisplayOrder", "IsVisible", "CreatedAtUtc" },
                values: new object[,]
                {
                    { 1, 1, "DRAFT", "Draft", null, 1, true, new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 1, "SUBMITTED_FOR_APPROVAL", "Submitted For Approval", null, 2, true, new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 1, "APPROVED", "Approved", null, 3, true, new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, 1, "REJECTED", "Rejected", null, 4, true, new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, 1, "DELETED", "Deleted", null, 5, true, new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateTable(
                name: "PermitApplication",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    PermitNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PermitIssuer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PermitReceiver = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LocationOfWork = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DescriptionOfWork = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpecialInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlannedStartDateTime = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    PlannedEndDateTime = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    PermitTypeListItemId = table.Column<int>(type: "int", nullable: false),
                    PermitStatusListItemId = table.Column<int>(type: "int", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermitApplication", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermitApplication_ListItem_PermitStatusListItemId",
                        column: x => x.PermitStatusListItemId,
                        principalSchema: "dbo",
                        principalTable: "ListItem",
                        principalColumn: "ListItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermitApplication_ListItem_PermitTypeListItemId",
                        column: x => x.PermitTypeListItemId,
                        principalSchema: "dbo",
                        principalTable: "ListItem",
                        principalColumn: "ListItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_AuditLog_ChangedByUserId_ChangedAtUtc", schema: "dbo", table: "AuditLog", columns: new[] { "ChangedByUserId", "ChangedAtUtc" });
            migrationBuilder.CreateIndex(name: "IX_AuditLog_EntityName_ChangedAtUtc", schema: "dbo", table: "AuditLog", columns: new[] { "EntityName", "ChangedAtUtc" });
            migrationBuilder.CreateIndex(name: "IX_ListItem_ListItemCategoryId_Code", schema: "dbo", table: "ListItem", columns: new[] { "ListItemCategoryId", "SystemName" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_ListItem_ListItemCategoryId_IsActive_DisplayOrder", schema: "dbo", table: "ListItem", columns: new[] { "ListItemCategoryId", "IsVisible", "DisplayOrder" });
            migrationBuilder.CreateIndex(name: "IX_ListItemCategory_Code", schema: "dbo", table: "ListItemCategory", column: "Code", unique: true);
            migrationBuilder.CreateIndex(name: "IX_PermitApplication_PermitNumber", schema: "dbo", table: "PermitApplication", column: "PermitNumber", unique: true);
            migrationBuilder.CreateIndex(name: "IX_PermitApplication_PermitStatusListItemId_CreatedAtUtc", schema: "dbo", table: "PermitApplication", columns: new[] { "PermitStatusListItemId", "CreatedAtUtc" });
            migrationBuilder.CreateIndex(name: "IX_PermitApplication_PermitTypeListItemId", schema: "dbo", table: "PermitApplication", column: "PermitTypeListItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditLog", schema: "dbo");
            migrationBuilder.DropTable(name: "PermitApplication", schema: "dbo");

            migrationBuilder.DeleteData(schema: "dbo", table: "ListItem", keyColumn: "ListItemId", keyValues: new object[] { 1, 2, 3, 4, 5 });
            migrationBuilder.DeleteData(schema: "dbo", table: "ListItemCategory", keyColumn: "ListItemCategoryId", keyValues: new object[] { 1, 2 });

            migrationBuilder.DropIndex(name: "IX_ListItem_ListItemCategoryId_Code", schema: "dbo", table: "ListItem");
            migrationBuilder.DropIndex(name: "IX_ListItem_ListItemCategoryId_IsActive_DisplayOrder", schema: "dbo", table: "ListItem");
            migrationBuilder.DropIndex(name: "IX_ListItemCategory_Code", schema: "dbo", table: "ListItemCategory");

            migrationBuilder.DropColumn(name: "Description", schema: "dbo", table: "ListItem");
            migrationBuilder.DropColumn(name: "DisplayOrder", schema: "dbo", table: "ListItem");
            migrationBuilder.DropColumn(name: "CreatedAtUtc", schema: "dbo", table: "ListItem");
            migrationBuilder.DropColumn(name: "UpdatedAtUtc", schema: "dbo", table: "ListItem");
            migrationBuilder.DropColumn(name: "Code", schema: "dbo", table: "ListItemCategory");
            migrationBuilder.DropColumn(name: "Description", schema: "dbo", table: "ListItemCategory");
            migrationBuilder.DropColumn(name: "IsActive", schema: "dbo", table: "ListItemCategory");
            migrationBuilder.DropColumn(name: "CreatedAtUtc", schema: "dbo", table: "ListItemCategory");
            migrationBuilder.DropColumn(name: "UpdatedAtUtc", schema: "dbo", table: "ListItemCategory");

            migrationBuilder.AlterColumn<string>(name: "ItemName", schema: "dbo", table: "ListItem", type: "nvarchar(50)", maxLength: 50, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100);
            migrationBuilder.AlterColumn<string>(name: "CategoryName", schema: "dbo", table: "ListItemCategory", type: "nvarchar(50)", maxLength: 50, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100);
            migrationBuilder.Sql("ALTER TABLE [dbo].[ListItem] ADD CONSTRAINT [UK_ListItem_SystemName] UNIQUE ([SystemName]);");
        }
    }
}
