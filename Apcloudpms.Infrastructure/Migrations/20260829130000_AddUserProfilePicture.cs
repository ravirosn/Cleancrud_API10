using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260829130000_AddUserProfilePicture")]
public partial class AddUserProfilePicture : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ProfilePicturePath",
            table: "Users",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ProfilePictureUpdatedAtUtc",
            table: "Users",
            type: "datetime2(0)",
            precision: 0,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ProfilePicturePath", table: "Users");
        migrationBuilder.DropColumn(name: "ProfilePictureUpdatedAtUtc", table: "Users");
    }
}
