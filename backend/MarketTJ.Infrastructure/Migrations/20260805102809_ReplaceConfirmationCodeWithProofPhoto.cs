using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketTJ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceConfirmationCodeWithProofPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ВАЖНО: не переименование (EF по умолчанию предложил
            // RenameColumn ConfirmationCodeHash → DeliveryProofPhotoUrl) — это
            // перенесло бы старые bcrypt-хэши кодов подтверждения в новую
            // колонку с URL фото для всех доставок, ещё не завершённых до
            // этой миграции (и для уже Delivered — тоже, задним числом). Явный
            // Drop + Add, чтобы новая колонка гарантированно начиналась пустой.
            migrationBuilder.DropColumn(
                name: "ConfirmationAttempts",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "ConfirmationCode",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "ConfirmationCodeHash",
                table: "Deliveries");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryProofPhotoUrl",
                table: "Deliveries",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryProofPhotoUrl",
                table: "Deliveries");

            migrationBuilder.AddColumn<int>(
                name: "ConfirmationAttempts",
                table: "Deliveries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmationCode",
                table: "Deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmationCodeHash",
                table: "Deliveries",
                type: "text",
                nullable: true);
        }
    }
}
