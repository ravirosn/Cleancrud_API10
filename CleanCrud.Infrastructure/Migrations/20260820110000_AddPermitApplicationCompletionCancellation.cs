using CleanCrud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCrud.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260820110000_AddPermitApplicationCompletionCancellation")]
public partial class AddPermitApplicationCompletionCancellation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CompletionOfWorks",
            schema: "dbo",
            table: "PermitApplication",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CompletionApprovedBy",
            schema: "dbo",
            table: "PermitApplication",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CompletionDate",
            schema: "dbo",
            table: "PermitApplication",
            type: "datetime2(0)",
            precision: 0,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CompletionRemarks",
            schema: "dbo",
            table: "PermitApplication",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CancelledBy",
            schema: "dbo",
            table: "PermitApplication",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CancelledDate",
            schema: "dbo",
            table: "PermitApplication",
            type: "datetime2(0)",
            precision: 0,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CancelledRemarks",
            schema: "dbo",
            table: "PermitApplication",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CompletionOfWorks", schema: "dbo", table: "PermitApplication");
        migrationBuilder.DropColumn(name: "CompletionApprovedBy", schema: "dbo", table: "PermitApplication");
        migrationBuilder.DropColumn(name: "CompletionDate", schema: "dbo", table: "PermitApplication");
        migrationBuilder.DropColumn(name: "CompletionRemarks", schema: "dbo", table: "PermitApplication");
        migrationBuilder.DropColumn(name: "CancelledBy", schema: "dbo", table: "PermitApplication");
        migrationBuilder.DropColumn(name: "CancelledDate", schema: "dbo", table: "PermitApplication");
        migrationBuilder.DropColumn(name: "CancelledRemarks", schema: "dbo", table: "PermitApplication");
    }
}
