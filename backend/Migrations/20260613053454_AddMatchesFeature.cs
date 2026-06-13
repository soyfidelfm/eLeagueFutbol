using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchesFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsFeatured",
                table: "Matches",
                newName: "IsMatchOfTheWeek");

            migrationBuilder.AddColumn<Guid>(
                name: "TeamId",
                table: "MatchPlayerStats",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Arena",
                table: "Matches",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "Matches",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WeekNumber",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayerStats_TeamId",
                table: "MatchPlayerStats",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchPlayerStats_Teams_TeamId",
                table: "MatchPlayerStats",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchPlayerStats_Teams_TeamId",
                table: "MatchPlayerStats");

            migrationBuilder.DropIndex(
                name: "IX_MatchPlayerStats_TeamId",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "MatchPlayerStats");

            migrationBuilder.DropColumn(
                name: "Arena",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "WeekNumber",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "IsMatchOfTheWeek",
                table: "Matches",
                newName: "IsFeatured");
        }
    }
}
