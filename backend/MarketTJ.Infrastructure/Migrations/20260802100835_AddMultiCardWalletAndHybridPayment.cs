using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketTJ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiCardWalletAndHybridPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets");

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Wallets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "—");

            migrationBuilder.AddColumn<string>(
                name: "CardNumber",
                table: "Wallets",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "0000000000000000");

            migrationBuilder.AddColumn<string>(
                name: "Cvv",
                table: "Wallets",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "000");

            migrationBuilder.AddColumn<int>(
                name: "ExpiryMonth",
                table: "Wallets",
                type: "integer",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.AddColumn<int>(
                name: "ExpiryYear",
                table: "Wallets",
                type: "integer",
                nullable: false,
                defaultValue: 2099);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "WalletId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_WalletId",
                table: "Orders",
                column: "WalletId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Wallets_WalletId",
                table: "Orders",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Wallets_WalletId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_Orders_WalletId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "CardNumber",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "Cvv",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "ExpiryMonth",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "ExpiryYear",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "WalletId",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets",
                column: "UserId",
                unique: true);
        }
    }
}
