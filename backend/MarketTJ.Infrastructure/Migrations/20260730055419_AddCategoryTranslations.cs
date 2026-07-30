using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketTJ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameTj",
                table: "Categories",
                type: "text",
                nullable: true);

            // Бэкфилл переводов для 6 категорий, засеянных ещё до появления
            // NameTj/NameEn (см. Frontend/src/locales/{tj,en}/data.json) —
            // новые категории, добавленные после этой миграции через админку,
            // обязаны иметь оба перевода (см. CategoryValidator), поэтому
            // бэкфилл нужен только для уже существующих строк.
            migrationBuilder.Sql(
                """
                UPDATE "Categories" SET "NameTj" = 'Сабзавот', "NameEn" = 'Vegetables' WHERE "Name" = 'Овощи';
                UPDATE "Categories" SET "NameTj" = 'Меваҳо', "NameEn" = 'Fruits' WHERE "Name" = 'Фрукты';
                UPDATE "Categories" SET "NameTj" = 'Кабудӣ', "NameEn" = 'Greens' WHERE "Name" = 'Зелень';
                UPDATE "Categories" SET "NameTj" = 'Мевахушк', "NameEn" = 'Dried fruits' WHERE "Name" = 'Сухофрукты';
                UPDATE "Categories" SET "NameTj" = 'Мағзиҷот', "NameEn" = 'Nuts' WHERE "Name" = 'Орехи';
                UPDATE "Categories" SET "NameTj" = 'Маҳсулоти ширӣ', "NameEn" = 'Dairy' WHERE "Name" = 'Молочная продукция';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "NameTj",
                table: "Categories");
        }
    }
}
