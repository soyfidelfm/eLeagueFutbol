using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftAdminControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentPickDeadlineUTC",
                table: "DraftSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "DraftSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaused",
                table: "DraftSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PausedRemainingSeconds",
                table: "DraftSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PickSeconds",
                table: "DraftSessions",
                type: "integer",
                nullable: false,
                defaultValue: 300);

            migrationBuilder.AddColumn<bool>(
                name: "TimerEnabled",
                table: "DraftSessions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Drafts already running get a fresh server-side deadline so the new timer picks them up.
            migrationBuilder.Sql(
                "UPDATE \"DraftSessions\" SET \"CurrentPickDeadlineUTC\" = now() + interval '300 seconds' " +
                "WHERE \"IsActive\" = TRUE;");

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    UpdatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropColumn(
                name: "CurrentPickDeadlineUTC",
                table: "DraftSessions");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "DraftSessions");

            migrationBuilder.DropColumn(
                name: "IsPaused",
                table: "DraftSessions");

            migrationBuilder.DropColumn(
                name: "PausedRemainingSeconds",
                table: "DraftSessions");

            migrationBuilder.DropColumn(
                name: "PickSeconds",
                table: "DraftSessions");

            migrationBuilder.DropColumn(
                name: "TimerEnabled",
                table: "DraftSessions");
        }
    }
}
