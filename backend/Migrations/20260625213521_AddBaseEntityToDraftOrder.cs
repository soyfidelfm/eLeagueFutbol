using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseEntityToDraftOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUTC",
                table: "DraftOrders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "DraftOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DraftOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUTC",
                table: "DraftOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "DraftOrders",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAtUTC",
                table: "DraftOrders");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "DraftOrders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DraftOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUTC",
                table: "DraftOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "DraftOrders");
        }
    }
}
