using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AwayUserId",
                table: "Matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HomeUserId",
                table: "Matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Matches" m
                SET "HomeUserId" = u."UserId"
                FROM "Users" u
                WHERE u."GameClubId" = m."HomeGameClubId"
                  AND u."IsDeleted" = FALSE;
            """);

            migrationBuilder.Sql("""
                UPDATE "Matches" m
                SET "AwayUserId" = u."UserId"
                FROM "Users" u
                WHERE u."GameClubId" = m."AwayGameClubId"
                  AND u."IsDeleted" = FALSE;
            """);

            migrationBuilder.Sql("""
                DELETE FROM "Matches"
                WHERE "HomeUserId" IS NULL
                   OR "AwayUserId" IS NULL;
            """);

            migrationBuilder.AlterColumn<Guid>(
                name: "AwayUserId",
                table: "Matches",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "HomeUserId",
                table: "Matches",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AwayUserId",
                table: "Matches",
                column: "AwayUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_HomeUserId",
                table: "Matches",
                column: "HomeUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_AwayUserId",
                table: "Matches",
                column: "AwayUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_HomeUserId",
                table: "Matches",
                column: "HomeUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_AwayUserId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_HomeUserId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_AwayUserId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_HomeUserId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayUserId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeUserId",
                table: "Matches");
        }
    }
}