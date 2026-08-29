using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260829100000_AddUserThemeSettings")]
public partial class AddUserThemeSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UserThemeSetting",
            schema: "dbo",
            columns: table => new
            {
                UserId = table.Column<int>(type: "int", nullable: false),
                Mode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Radius = table.Column<int>(type: "int", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserThemeSetting", x => x.UserId);
                table.CheckConstraint("CK_UserThemeSetting_Color", "[Color] IN (N'blue', N'azure', N'indigo', N'purple', N'pink', N'red', N'orange', N'green')");
                table.CheckConstraint("CK_UserThemeSetting_Mode", "[Mode] IN (N'light', N'dark', N'system')");
                table.CheckConstraint("CK_UserThemeSetting_Radius", "[Radius] IN (0, 6, 12)");
                table.ForeignKey(
                    name: "FK_UserThemeSetting_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "UserThemeSetting",
            schema: "dbo");
    }
}
