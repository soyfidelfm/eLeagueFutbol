using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFullMatchPlayerStatsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AwayUserClubUserClubId",
                table: "Matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HomeUserClubUserClubId",
                table: "Matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AwayUserClubUserClubId",
                table: "Matches",
                column: "AwayUserClubUserClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_HomeUserClubUserClubId",
                table: "Matches",
                column: "HomeUserClubUserClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_UserClubs_AwayUserClubUserClubId",
                table: "Matches",
                column: "AwayUserClubUserClubId",
                principalTable: "UserClubs",
                principalColumn: "UserClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_UserClubs_HomeUserClubUserClubId",
                table: "Matches",
                column: "HomeUserClubUserClubId",
                principalTable: "UserClubs",
                principalColumn: "UserClubId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_UserClubs_AwayUserClubUserClubId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_UserClubs_HomeUserClubUserClubId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_AwayUserClubUserClubId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_HomeUserClubUserClubId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayUserClubUserClubId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeUserClubUserClubId",
                table: "Matches");
        }
    }
}
