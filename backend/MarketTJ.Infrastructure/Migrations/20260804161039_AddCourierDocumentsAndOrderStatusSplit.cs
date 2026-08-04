using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarketTJ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourierDocumentsAndOrderStatusSplit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ВАЖНО: dotnet ef migrations add изначально сгенерировал сюда ЕЩЁ
            // 14 AddColumn на Deliveries/CourierProfiles (AcceptedAt, AdminNote,
            // CancellationReason, CancelledAt, ClientNote, ConfirmationAttempts,
            // ConfirmationCode, ConfirmationCodeHash, CourierNote,
            // EstimatedDeliveryAt, EstimatedPickupAt, FarmerNote,
            // ProblemDescription, CourierProfiles.Rating) — это давний разъезд
            // между локальным снапшотом модели и реальной БД (эти колонки physически
            // уже существуют в production ещё с AddDeliveryTrackingSystem, снапшот
            // просто не отражал их после ручного мержа при cherry-pick 2026-08-02).
            // Проверено напрямую в БД перед применением (information_schema.columns) —
            // все 14 колонок уже есть. Удалены отсюда вручную, иначе миграция упала
            // бы на проде с "column already exists".
            migrationBuilder.AddColumn<int>(
                name: "CourierStatus",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FarmerStatus",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CourierDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourierProfileId = table.Column<int>(type: "integer", nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByAdminId = table.Column<int>(type: "integer", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierDocuments_CourierProfiles_CourierProfileId",
                        column: x => x.CourierProfileId,
                        principalTable: "CourierProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourierDocuments_Users_ReviewedByAdminId",
                        column: x => x.ReviewedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourierDocuments_CourierProfileId",
                table: "CourierDocuments",
                column: "CourierProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierDocuments_ReviewedByAdminId",
                table: "CourierDocuments",
                column: "ReviewedByAdminId");

            // Backfill существующих заказов в новые FarmerStatus/CourierStatus —
            // по прямому запросу пользователя (2026-08-04), маппинг из старого
            // единого Order.Status (OrderStatus: Pending=1, Accepted=2,
            // Rejected=3, Preparing=4, ReadyForPickup=5, CourierAssigned=6,
            // PickedUp=7, InDelivery=8, Delivered=9, Completed=10, Cancelled=11)
            // в FarmerOrderStatus (Accepted=1, HandedToCourier=2) и
            // CourierOrderStatus (Accepted=1, Delivered=2):
            //
            //   Status IN (Pending, Rejected, Preparing)              → FarmerStatus=NULL, CourierStatus=NULL
            //   Status IN (Accepted)                                  → FarmerStatus=Accepted, CourierStatus=NULL
            //   Status IN (ReadyForPickup, CourierAssigned)           → FarmerStatus=HandedToCourier, CourierStatus=NULL
            //   Status IN (PickedUp, InDelivery)                      → FarmerStatus=HandedToCourier, CourierStatus=Accepted
            //   Status = Delivered                                   → FarmerStatus=HandedToCourier, CourierStatus=Delivered
            //   Status = Completed, есть Delivery                     → тот же маппинг, что Delivered (был передан курьеру и доставлен)
            //   Status = Completed, Delivery нет                      → FarmerStatus=NULL, CourierStatus=NULL (заказ завершён без курьера)
            //   Status = Cancelled, есть Delivery со статусом <= ArrivedAtFarmer (2-5) → FarmerStatus=HandedToCourier, CourierStatus=NULL
            //   Status = Cancelled, есть Delivery со статусом >= PickedUp (6-9)        → FarmerStatus=HandedToCourier, CourierStatus=Accepted
            //   Status = Cancelled, Delivery нет                      → FarmerStatus=NULL, CourierStatus=NULL
            migrationBuilder.Sql(
                """
                UPDATE "Orders" SET "FarmerStatus" = 1 WHERE "Status" = 2;

                UPDATE "Orders" SET "FarmerStatus" = 2 WHERE "Status" IN (5, 6);

                UPDATE "Orders" SET "FarmerStatus" = 2, "CourierStatus" = 1 WHERE "Status" IN (7, 8);

                UPDATE "Orders" SET "FarmerStatus" = 2, "CourierStatus" = 2 WHERE "Status" = 9;

                UPDATE "Orders" o SET "FarmerStatus" = 2, "CourierStatus" = 2
                    WHERE o."Status" = 10 AND EXISTS (SELECT 1 FROM "Deliveries" d WHERE d."OrderId" = o."Id");

                UPDATE "Orders" o SET "FarmerStatus" = 2
                    WHERE o."Status" = 11 AND EXISTS (
                        SELECT 1 FROM "Deliveries" d WHERE d."OrderId" = o."Id" AND d."Status" BETWEEN 2 AND 5);

                UPDATE "Orders" o SET "FarmerStatus" = 2, "CourierStatus" = 1
                    WHERE o."Status" = 11 AND EXISTS (
                        SELECT 1 FROM "Deliveries" d WHERE d."OrderId" = o."Id" AND d."Status" BETWEEN 6 AND 9);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourierDocuments");

            migrationBuilder.DropColumn(
                name: "CourierStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FarmerStatus",
                table: "Orders");
        }
    }
}
