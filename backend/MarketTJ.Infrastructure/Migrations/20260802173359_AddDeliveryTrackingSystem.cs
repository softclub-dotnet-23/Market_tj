using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketTJ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryTrackingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcceptedAt",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientNote",
                table: "Deliveries",
                type: "text",
                nullable: true);

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

            migrationBuilder.AddColumn<string>(
                name: "CourierNote",
                table: "Deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedDeliveryAt",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedPickupAt",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FarmerNote",
                table: "Deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProblemDescription",
                table: "Deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Rating",
                table: "CourierProfiles",
                type: "numeric(3,2)",
                precision: 3,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "ClientNote",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "ConfirmationAttempts",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "ConfirmationCode",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "ConfirmationCodeHash",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "CourierNote",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "EstimatedDeliveryAt",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "EstimatedPickupAt",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "FarmerNote",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "ProblemDescription",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "CourierProfiles");
        }
    }
}
