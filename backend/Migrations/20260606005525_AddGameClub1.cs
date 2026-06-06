using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddGameClub1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GameClubId",
                table: "Teams",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "GameClubs",
                columns: table => new
                {
                    GameClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    League = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameClubs", x => x.GameClubId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_GameClubId",
                table: "Teams",
                column: "GameClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_GameClubs_GameClubId",
                table: "Teams",
                column: "GameClubId",
                principalTable: "GameClubs",
                principalColumn: "GameClubId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_GameClubs_GameClubId",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "GameClubs");

            migrationBuilder.DropIndex(
                name: "IX_Teams_GameClubId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "GameClubId",
                table: "Teams");
        }
    }
}
