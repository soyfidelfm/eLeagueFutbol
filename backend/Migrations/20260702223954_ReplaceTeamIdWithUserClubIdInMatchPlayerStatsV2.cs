using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTeamIdWithUserClubIdInMatchPlayerStatsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchPlayerStats_Teams_TeamId",
                table: "MatchPlayerStats");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                table: "MatchPlayerStats",
                newName: "UserClubId");

            migrationBuilder.RenameIndex(
                name: "IX_MatchPlayerStats_TeamId",
                table: "MatchPlayerStats",
                newName: "IX_MatchPlayerStats_UserClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchPlayerStats_UserClubs_UserClubId",
                table: "MatchPlayerStats",
                column: "UserClubId",
                principalTable: "UserClubs",
                principalColumn: "UserClubId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchPlayerStats_UserClubs_UserClubId",
                table: "MatchPlayerStats");

            migrationBuilder.RenameColumn(
                name: "UserClubId",
                table: "MatchPlayerStats",
                newName: "TeamId");

            migrationBuilder.RenameIndex(
                name: "IX_MatchPlayerStats_UserClubId",
                table: "MatchPlayerStats",
                newName: "IX_MatchPlayerStats_TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchPlayerStats_Teams_TeamId",
                table: "MatchPlayerStats",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
