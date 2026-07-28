using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketTJ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MergeConversationsByCustomerFarmerPair : Migration
    {
        // Чисто модельных изменений нет (Up/Down по умолчанию пустые) — эта
        // миграция только переносит данные: объединяет дубли Conversation по
        // паре CustomerId/FarmerId, оставшиеся с прошлой версии, где
        // уникальность ещё считалась по Order (см. ConversationService).
        // Down необратим по смыслу (объединение данных нельзя честно
        // "разъединить" обратно), поэтому оставлен пустым.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Для каждой пары (CustomerId, FarmerId) с несколькими Conversation
            // оставляем "выжившим" самый старый (минимальный Id), переносим все
            // ChatMessage дублей на него, затем удаляем сами дубли-строки.
            migrationBuilder.Sql(
                """
                WITH ranked AS (
                    SELECT "Id",
                           FIRST_VALUE("Id") OVER (PARTITION BY "CustomerId", "FarmerId" ORDER BY "Id") AS "SurvivorId"
                    FROM "Conversations"
                ),
                dups AS (
                    SELECT "Id", "SurvivorId" FROM ranked WHERE "Id" <> "SurvivorId"
                )
                UPDATE "ChatMessages" cm
                SET "ConversationId" = dups."SurvivorId"
                FROM dups
                WHERE cm."ConversationId" = dups."Id";
                """);

            migrationBuilder.Sql(
                """
                WITH ranked AS (
                    SELECT "Id",
                           FIRST_VALUE("Id") OVER (PARTITION BY "CustomerId", "FarmerId" ORDER BY "Id") AS "SurvivorId"
                    FROM "Conversations"
                )
                DELETE FROM "Conversations" c
                USING ranked
                WHERE c."Id" = ranked."Id" AND ranked."Id" <> ranked."SurvivorId";
                """);

            // UpdatedAt выжившего чата должен отражать самое позднее реальное
            // сообщение (после переноса выше) — иначе сортировка "последние
            // сверху" в списке переписок могла бы показать объединённый чат не
            // на своём месте.
            migrationBuilder.Sql(
                """
                UPDATE "Conversations" c
                SET "UpdatedAt" = latest."MaxCreatedAt"
                FROM (
                    SELECT "ConversationId", MAX("CreatedAt") AS "MaxCreatedAt"
                    FROM "ChatMessages"
                    GROUP BY "ConversationId"
                ) latest
                WHERE c."Id" = latest."ConversationId" AND latest."MaxCreatedAt" > c."UpdatedAt";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
