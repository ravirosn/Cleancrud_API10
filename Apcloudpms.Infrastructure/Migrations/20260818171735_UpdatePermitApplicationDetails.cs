using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePermitApplicationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlannedEndDateTime",
                schema: "dbo",
                table: "PermitApplication");

            migrationBuilder.DropColumn(
                name: "PlannedStartDateTime",
                schema: "dbo",
                table: "PermitApplication");

            migrationBuilder.RenameColumn(
                name: "PermitIssuer",
                schema: "dbo",
                table: "PermitApplication",
                newName: "PermitIssuerName");

            migrationBuilder.RenameColumn(
                name: "PermitReceiver",
                schema: "dbo",
                table: "PermitApplication",
                newName: "PermitReceiverName");

            migrationBuilder.RenameColumn(
                name: "LocationOfWork",
                schema: "dbo",
                table: "PermitApplication",
                newName: "WorkLocation");

            migrationBuilder.RenameColumn(
                name: "DescriptionOfWork",
                schema: "dbo",
                table: "PermitApplication",
                newName: "WorkDescription");

            migrationBuilder.AddColumn<string>(
                name: "PermitIssuerContactNumber",
                schema: "dbo",
                table: "PermitApplication",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PermitReceiverName",
                schema: "dbo",
                table: "PermitApplication",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);

            migrationBuilder.AddColumn<string>(
                name: "PermitReceiverContactNumber",
                schema: "dbo",
                table: "PermitApplication",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreRiskAssessmentNumber",
                schema: "dbo",
                table: "PermitApplication",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkHeightBelowSurface",
                schema: "dbo",
                table: "PermitApplication",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PermitIssuerContactNumber",
                schema: "dbo",
                table: "PermitApplication");

            migrationBuilder.DropColumn(
                name: "PermitReceiverContactNumber",
                schema: "dbo",
                table: "PermitApplication");

            migrationBuilder.DropColumn(
                name: "PreRiskAssessmentNumber",
                schema: "dbo",
                table: "PermitApplication");

            migrationBuilder.DropColumn(
                name: "WorkHeightBelowSurface",
                schema: "dbo",
                table: "PermitApplication");

            migrationBuilder.RenameColumn(
                name: "WorkLocation",
                schema: "dbo",
                table: "PermitApplication",
                newName: "LocationOfWork");

            migrationBuilder.RenameColumn(
                name: "WorkDescription",
                schema: "dbo",
                table: "PermitApplication",
                newName: "DescriptionOfWork");

            migrationBuilder.AlterColumn<string>(
                name: "PermitReceiverName",
                schema: "dbo",
                table: "PermitApplication",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.RenameColumn(
                name: "PermitIssuerName",
                schema: "dbo",
                table: "PermitApplication",
                newName: "PermitIssuer");

            migrationBuilder.RenameColumn(
                name: "PermitReceiverName",
                schema: "dbo",
                table: "PermitApplication",
                newName: "PermitReceiver");

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedEndDateTime",
                schema: "dbo",
                table: "PermitApplication",
                type: "datetime2(0)",
                precision: 0,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedStartDateTime",
                schema: "dbo",
                table: "PermitApplication",
                type: "datetime2(0)",
                precision: 0,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
