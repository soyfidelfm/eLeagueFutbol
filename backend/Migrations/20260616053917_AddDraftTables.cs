using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DraftOrders",
                columns: table => new
                {
                    DraftOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PickOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DraftOrders", x => x.DraftOrderId);
                });

            migrationBuilder.CreateTable(
                name: "DraftPicks",
                columns: table => new
                {
                    DraftPickId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GamePlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PickNumber = table.Column<int>(type: "integer", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    PickedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DraftPicks", x => x.DraftPickId);
                });

            migrationBuilder.CreateTable(
                name: "DraftSessions",
                columns: table => new
                {
                    DraftSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentPickNumber = table.Column<int>(type: "integer", nullable: false),
                    PlayersPerTeam = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DraftSessions", x => x.DraftSessionId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DraftOrders");

            migrationBuilder.DropTable(
                name: "DraftPicks");

            migrationBuilder.DropTable(
                name: "DraftSessions");
        }
    }
}
