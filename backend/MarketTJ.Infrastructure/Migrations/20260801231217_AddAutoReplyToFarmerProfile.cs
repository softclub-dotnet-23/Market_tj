using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketTJ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoReplyToFarmerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoReplyToReviewsEnabled",
                table: "FarmerProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoReplyToReviewsEnabled",
                table: "FarmerProfiles");
        }
    }
}
