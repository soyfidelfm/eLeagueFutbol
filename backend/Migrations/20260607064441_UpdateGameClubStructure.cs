using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGameClubStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttackRating",
                table: "GameClubs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefenseRating",
                table: "GameClubs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExternalTeamId",
                table: "GameClubs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MidfieldRating",
                table: "GameClubs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OverallRating",
                table: "GameClubs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "GameClubs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StadiumName",
                table: "GameClubs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttackRating",
                table: "GameClubs");

            migrationBuilder.DropColumn(
                name: "DefenseRating",
                table: "GameClubs");

            migrationBuilder.DropColumn(
                name: "ExternalTeamId",
                table: "GameClubs");

            migrationBuilder.DropColumn(
                name: "MidfieldRating",
                table: "GameClubs");

            migrationBuilder.DropColumn(
                name: "OverallRating",
                table: "GameClubs");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "GameClubs");

            migrationBuilder.DropColumn(
                name: "StadiumName",
                table: "GameClubs");
        }
    }
}
