using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchSummaryStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchSummaryStats",
                columns: table => new
                {
                    MatchSummaryStatsId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    HomePossession = table.Column<int>(type: "integer", nullable: false),
                    AwayPossession = table.Column<int>(type: "integer", nullable: false),
                    HomeShots = table.Column<int>(type: "integer", nullable: false),
                    AwayShots = table.Column<int>(type: "integer", nullable: false),
                    HomeExpectedGoals = table.Column<decimal>(type: "numeric", nullable: false),
                    AwayExpectedGoals = table.Column<decimal>(type: "numeric", nullable: false),
                    HomePasses = table.Column<int>(type: "integer", nullable: false),
                    AwayPasses = table.Column<int>(type: "integer", nullable: false),
                    HomeTackles = table.Column<int>(type: "integer", nullable: false),
                    AwayTackles = table.Column<int>(type: "integer", nullable: false),
                    HomeTacklesWon = table.Column<int>(type: "integer", nullable: false),
                    AwayTacklesWon = table.Column<int>(type: "integer", nullable: false),
                    HomeInterceptions = table.Column<int>(type: "integer", nullable: false),
                    AwayInterceptions = table.Column<int>(type: "integer", nullable: false),
                    HomeSaves = table.Column<int>(type: "integer", nullable: false),
                    AwaySaves = table.Column<int>(type: "integer", nullable: false),
                    HomeFoulsCommitted = table.Column<int>(type: "integer", nullable: false),
                    AwayFoulsCommitted = table.Column<int>(type: "integer", nullable: false),
                    HomeOffsides = table.Column<int>(type: "integer", nullable: false),
                    AwayOffsides = table.Column<int>(type: "integer", nullable: false),
                    HomeCorners = table.Column<int>(type: "integer", nullable: false),
                    AwayCorners = table.Column<int>(type: "integer", nullable: false),
                    HomeFreeKicks = table.Column<int>(type: "integer", nullable: false),
                    AwayFreeKicks = table.Column<int>(type: "integer", nullable: false),
                    HomePenaltyKicks = table.Column<int>(type: "integer", nullable: false),
                    AwayPenaltyKicks = table.Column<int>(type: "integer", nullable: false),
                    HomeYellowCards = table.Column<int>(type: "integer", nullable: false),
                    AwayYellowCards = table.Column<int>(type: "integer", nullable: false),
                    MatchId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchSummaryStats", x => x.MatchSummaryStatsId);
                    table.ForeignKey(
                        name: "FK_MatchSummaryStats_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "MatchId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchSummaryStats_Matches_MatchId1",
                        column: x => x.MatchId1,
                        principalTable: "Matches",
                        principalColumn: "MatchId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchSummaryStats_MatchId",
                table: "MatchSummaryStats",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchSummaryStats_MatchId1",
                table: "MatchSummaryStats",
                column: "MatchId1",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchSummaryStats");
        }
    }
}
