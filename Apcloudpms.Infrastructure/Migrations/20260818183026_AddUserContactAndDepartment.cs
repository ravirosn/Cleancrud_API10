using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserContactAndDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactNumber",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                ;WITH NumberedUsers AS (
                    SELECT [Id], ROW_NUMBER() OVER (ORDER BY [Id]) AS [RowNumber]
                    FROM [dbo].[Users]
                    WHERE [ContactNumber] IS NULL
                )
                UPDATE users
                SET [ContactNumber] = CONVERT(nvarchar(20),
                    CAST(9800000000 AS bigint) + numbered.[RowNumber])
                FROM [dbo].[Users] users
                INNER JOIN NumberedUsers numbered ON numbered.[Id] = users.[Id];

                WITH Assignments AS (
                    SELECT * FROM (VALUES
                        (N'DEMO.ADMIN', N'KTM-HO', N'ADMIN'),
                        (N'DEMO.ASHA', N'KTM-HO', N'FIN'),
                        (N'DEMO.BIBEK', N'KTM-HO', N'IT'),
                        (N'DEMO.DEEPA', N'PKR', N'OPS'),
                        (N'DEMO.GAURAV', N'PKR', N'CS'),
                        (N'DEMO.KABITA', N'BRT', N'OPS'),
                        (N'DEMO.NABIN', N'BRT', N'CS'),
                        (N'DEMO.PRIYA', N'KTM-HO', N'FIN'),
                        (N'DEMO.ROSHAN', N'PKR', N'OPS'),
                        (N'DEMO.SUSHMA', N'BRT', N'CS')
                    ) valuesTable ([NormalizedUserName], [BranchCode], [DepartmentCode])
                )
                UPDATE users
                SET [DepartmentId] = department.[Id]
                FROM [dbo].[Users] users
                INNER JOIN Assignments assignment
                    ON assignment.[NormalizedUserName] = users.[NormalizedUserName]
                INNER JOIN [dbo].[OfficeBranch] branch
                    ON branch.[Code] = assignment.[BranchCode]
                INNER JOIN [dbo].[Department] department
                    ON department.[OfficeBranchId] = branch.[Id]
                    AND department.[Code] = assignment.[DepartmentCode]
                WHERE users.[DepartmentId] IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Department_DepartmentId",
                table: "Users",
                column: "DepartmentId",
                principalSchema: "dbo",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Department_DepartmentId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DepartmentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ContactNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Users");
        }
    }
}
