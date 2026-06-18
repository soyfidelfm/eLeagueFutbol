using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationTokens",
                columns: table => new
                {
                    NotificationTokenId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FcmToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeviceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastUsedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTokens", x => x.NotificationTokenId);
                    table.ForeignKey(
                        name: "FK_NotificationTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DraftSessions_SeasonId",
                table: "DraftSessions",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftPicks_GamePlayerId",
                table: "DraftPicks",
                column: "GamePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftPicks_SeasonId",
                table: "DraftPicks",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftPicks_UserId",
                table: "DraftPicks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftOrders_SeasonId",
                table: "DraftOrders",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftOrders_UserId",
                table: "DraftOrders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTokens_FcmToken",
                table: "NotificationTokens",
                column: "FcmToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTokens_UserId",
                table: "NotificationTokens",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DraftOrders_Seasons_SeasonId",
                table: "DraftOrders",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "SeasonId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DraftOrders_Users_UserId",
                table: "DraftOrders",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DraftPicks_GamePlayers_GamePlayerId",
                table: "DraftPicks",
                column: "GamePlayerId",
                principalTable: "GamePlayers",
                principalColumn: "GamePlayerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DraftPicks_Seasons_SeasonId",
                table: "DraftPicks",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "SeasonId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DraftPicks_Users_UserId",
                table: "DraftPicks",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DraftSessions_Seasons_SeasonId",
                table: "DraftSessions",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "SeasonId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DraftOrders_Seasons_SeasonId",
                table: "DraftOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_DraftOrders_Users_UserId",
                table: "DraftOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_DraftPicks_GamePlayers_GamePlayerId",
                table: "DraftPicks");

            migrationBuilder.DropForeignKey(
                name: "FK_DraftPicks_Seasons_SeasonId",
                table: "DraftPicks");

            migrationBuilder.DropForeignKey(
                name: "FK_DraftPicks_Users_UserId",
                table: "DraftPicks");

            migrationBuilder.DropForeignKey(
                name: "FK_DraftSessions_Seasons_SeasonId",
                table: "DraftSessions");

            migrationBuilder.DropTable(
                name: "NotificationTokens");

            migrationBuilder.DropIndex(
                name: "IX_DraftSessions_SeasonId",
                table: "DraftSessions");

            migrationBuilder.DropIndex(
                name: "IX_DraftPicks_GamePlayerId",
                table: "DraftPicks");

            migrationBuilder.DropIndex(
                name: "IX_DraftPicks_SeasonId",
                table: "DraftPicks");

            migrationBuilder.DropIndex(
                name: "IX_DraftPicks_UserId",
                table: "DraftPicks");

            migrationBuilder.DropIndex(
                name: "IX_DraftOrders_SeasonId",
                table: "DraftOrders");

            migrationBuilder.DropIndex(
                name: "IX_DraftOrders_UserId",
                table: "DraftOrders");
        }
    }
}
