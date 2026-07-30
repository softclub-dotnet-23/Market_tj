using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketTJ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryAndUnitToProductListing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "ProductListings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "ProductListings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "ProductListings",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Бэкфилл для уже существующих объявлений (засеянных до этой
            // миграции) — берём CategoryId/Unit у связанного Product, пока
            // ProductId ещё на него ссылается. Новые объявления после этой
            // миграции будут получать оба поля напрямую от фермера, без Product.
            migrationBuilder.Sql(
                """
                UPDATE "ProductListings" pl
                SET "CategoryId" = p."CategoryId", "Unit" = p."Unit"
                FROM "Products" p
                WHERE pl."ProductId" = p."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ProductListings_CategoryId",
                table: "ProductListings",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductListings_Categories_CategoryId",
                table: "ProductListings",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductListings_Categories_CategoryId",
                table: "ProductListings");

            migrationBuilder.DropIndex(
                name: "IX_ProductListings_CategoryId",
                table: "ProductListings");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "ProductListings");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "ProductListings");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "ProductListings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
