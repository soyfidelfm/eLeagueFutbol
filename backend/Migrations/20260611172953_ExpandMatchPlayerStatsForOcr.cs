using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class ExpandMatchPlayerStatsForOcr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DistanceCoveredKm",
                table: "MatchPlayerStats",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DistanceSprintedKm",
                table: "MatchPlayerStats",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DribbleSuccessRate",
                table: "MatchPlayerStats",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Dribbles",
                table: "MatchPlayerStats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FoulsCommitted",
                table: "MatchPlayerStats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPlayerOfTheMatch",
                table: "MatchPlayerStats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Offsides",
                table: "MatchPlayerStats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PassAccuracy",
                table: "MatchPlayerStats",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Passes",
                table: "MatchPlayerStats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PlayerName",
                table: "MatchPlayerStats",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "MatchPlayerStats",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PossessionLost",
                table: "MatchPlayerStats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PossessionWon",
                table: "MatchPlayerStats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ShotAccuracy",
                table: "MatchPlayerStats",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Shots",
                table: "MatchPlayerStats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TackleSuccessRate",
                table: "MatchPlayerStats",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Tackles",
                table: "MatchPlayerStats",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistanceCoveredKm",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "DistanceSprintedKm",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "DribbleSuccessRate",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "Dribbles",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "FoulsCommitted",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "IsPlayerOfTheMatch",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "Offsides",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "PassAccuracy",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "Passes",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "PlayerName",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "PossessionLost",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "PossessionWon",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "ShotAccuracy",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "Shots",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "TackleSuccessRate",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "Tackles",
                table: "MatchPlayerStats");
        }
    }
}
