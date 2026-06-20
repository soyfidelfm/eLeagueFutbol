using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreFc26PlayerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClubContractValidUntilYear",
                table: "GamePlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClubJerseyNumber",
                table: "GamePlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClubName",
                table: "GamePlayers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClubTeamId",
                table: "GamePlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "GamePlayers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Defending",
                table: "GamePlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Dribbling",
                table: "GamePlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NationTeamId",
                table: "GamePlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NationalityId",
                table: "GamePlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Pace",
                table: "GamePlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Passing",
                table: "GamePlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Physic",
                table: "GamePlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReleaseClauseEur",
                table: "GamePlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Shooting",
                table: "GamePlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SkillMoves",
                table: "GamePlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValueEur",
                table: "GamePlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WageEur",
                table: "GamePlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeakFoot",
                table: "GamePlayers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClubContractValidUntilYear",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "ClubJerseyNumber",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "ClubName",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "ClubTeamId",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "Defending",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "Dribbling",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "NationTeamId",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "NationalityId",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "Pace",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "Passing",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "Physic",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "ReleaseClauseEur",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "Shooting",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "SkillMoves",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "ValueEur",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "WageEur",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "WeakFoot",
                table: "GamePlayers");
        }
    }
}
