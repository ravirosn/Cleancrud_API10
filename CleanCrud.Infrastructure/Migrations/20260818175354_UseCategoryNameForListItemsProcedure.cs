using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCrud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseCategoryNameForListItemsProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE [dbo].[SpGetListItemsByCategory]
                    @CategoryName nvarchar(100)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT
                        li.[ListItemId],
                        li.[ListItemCategoryId],
                        li.[SystemName] AS [Code],
                        li.[ItemName] AS [Name],
                        li.[Description],
                        li.[DisplayOrder]
                    FROM [dbo].[ListItem] AS li
                    INNER JOIN [dbo].[ListItemCategory] AS category
                        ON category.[ListItemCategoryId] = li.[ListItemCategoryId]
                    WHERE category.[CategoryName] = @CategoryName
                        AND category.[IsActive] = 1
                        AND li.[IsVisible] = 1
                    ORDER BY li.[DisplayOrder], li.[ItemName], li.[ListItemId];
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE [dbo].[SpGetListItemsByCategory]
                    @ListItemCategoryId int
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT
                        li.[ListItemId],
                        li.[ListItemCategoryId],
                        li.[SystemName] AS [Code],
                        li.[ItemName] AS [Name],
                        li.[Description],
                        li.[DisplayOrder]
                    FROM [dbo].[ListItem] AS li
                    INNER JOIN [dbo].[ListItemCategory] AS category
                        ON category.[ListItemCategoryId] = li.[ListItemCategoryId]
                    WHERE li.[ListItemCategoryId] = @ListItemCategoryId
                        AND category.[IsActive] = 1
                        AND li.[IsVisible] = 1
                    ORDER BY li.[DisplayOrder], li.[ItemName], li.[ListItemId];
                END;
                """);
        }
    }
}
