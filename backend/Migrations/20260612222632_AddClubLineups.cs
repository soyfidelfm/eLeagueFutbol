using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddClubLineups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClubLineups",
                columns: table => new
                {
                    ClubLineupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Formation = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubLineups", x => x.ClubLineupId);
                });

            migrationBuilder.CreateTable(
                name: "ClubLineupPlayers",
                columns: table => new
                {
                    ClubLineupPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubLineupId = table.Column<Guid>(type: "uuid", nullable: false),
                    GamePlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotIndex = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubLineupPlayers", x => x.ClubLineupPlayerId);
                    table.ForeignKey(
                        name: "FK_ClubLineupPlayers_ClubLineups_ClubLineupId",
                        column: x => x.ClubLineupId,
                        principalTable: "ClubLineups",
                        principalColumn: "ClubLineupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClubLineupPlayers_ClubLineupId",
                table: "ClubLineupPlayers",
                column: "ClubLineupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClubLineupPlayers");

            migrationBuilder.DropTable(
                name: "ClubLineups");
        }
    }
}
