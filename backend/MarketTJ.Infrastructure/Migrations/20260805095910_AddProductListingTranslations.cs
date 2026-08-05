using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketTJ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductListingTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "ProductListings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionTj",
                table: "ProductListings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleEn",
                table: "ProductListings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleTj",
                table: "ProductListings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "ProductListings");

            migrationBuilder.DropColumn(
                name: "DescriptionTj",
                table: "ProductListings");

            migrationBuilder.DropColumn(
                name: "TitleEn",
                table: "ProductListings");

            migrationBuilder.DropColumn(
                name: "TitleTj",
                table: "ProductListings");
        }
    }
}
