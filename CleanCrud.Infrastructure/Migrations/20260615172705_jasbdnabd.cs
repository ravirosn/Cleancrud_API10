using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCrud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class jasbdnabd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MobileNo",
                table: "Students",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MobileNo",
                table: "Students");
        }
    }
}
