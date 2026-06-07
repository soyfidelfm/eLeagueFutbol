using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonRegistrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxPlayers",
                table: "Seasons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RegistrationOpen",
                table: "Seasons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SeasonRegistrations",
                columns: table => new
                {
                    SeasonRegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegisteredAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonRegistrations", x => x.SeasonRegistrationId);
                    table.ForeignKey(
                        name: "FK_SeasonRegistrations_GameClubs_GameClubId",
                        column: x => x.GameClubId,
                        principalTable: "GameClubs",
                        principalColumn: "GameClubId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeasonRegistrations_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "SeasonId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeasonRegistrations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeasonRegistrations_GameClubId",
                table: "SeasonRegistrations",
                column: "GameClubId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonRegistrations_SeasonId_UserId",
                table: "SeasonRegistrations",
                columns: new[] { "SeasonId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeasonRegistrations_UserId",
                table: "SeasonRegistrations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeasonRegistrations");

            migrationBuilder.DropColumn(
                name: "MaxPlayers",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "RegistrationOpen",
                table: "Seasons");
        }
    }
}
