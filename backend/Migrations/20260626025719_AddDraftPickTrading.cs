using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftPickTrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAtUTC",
                table: "ClubLineups",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "DraftPickAssets",
                columns: table => new
                {
                    DraftPickAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    PickNumber = table.Column<int>(type: "integer", nullable: false),
                    OriginalUserClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentOwnerUserClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DraftPickAssets", x => x.DraftPickAssetId);
                    table.ForeignKey(
                        name: "FK_DraftPickAssets_UserClubs_CurrentOwnerUserClubId",
                        column: x => x.CurrentOwnerUserClubId,
                        principalTable: "UserClubs",
                        principalColumn: "UserClubId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DraftPickAssets_UserClubs_OriginalUserClubId",
                        column: x => x.OriginalUserClubId,
                        principalTable: "UserClubs",
                        principalColumn: "UserClubId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DraftPickTrades",
                columns: table => new
                {
                    DraftPickTradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUserClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToUserClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DraftPickTrades", x => x.DraftPickTradeId);
                    table.ForeignKey(
                        name: "FK_DraftPickTrades_UserClubs_FromUserClubId",
                        column: x => x.FromUserClubId,
                        principalTable: "UserClubs",
                        principalColumn: "UserClubId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DraftPickTrades_UserClubs_ToUserClubId",
                        column: x => x.ToUserClubId,
                        principalTable: "UserClubs",
                        principalColumn: "UserClubId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DraftPickTradeAssets",
                columns: table => new
                {
                    DraftPickTradeAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftPickTradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftPickAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DraftPickTradeAssets", x => x.DraftPickTradeAssetId);
                    table.ForeignKey(
                        name: "FK_DraftPickTradeAssets_DraftPickAssets_DraftPickAssetId",
                        column: x => x.DraftPickAssetId,
                        principalTable: "DraftPickAssets",
                        principalColumn: "DraftPickAssetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DraftPickTradeAssets_DraftPickTrades_DraftPickTradeId",
                        column: x => x.DraftPickTradeId,
                        principalTable: "DraftPickTrades",
                        principalColumn: "DraftPickTradeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DraftPickAssets_CurrentOwnerUserClubId",
                table: "DraftPickAssets",
                column: "CurrentOwnerUserClubId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftPickAssets_OriginalUserClubId",
                table: "DraftPickAssets",
                column: "OriginalUserClubId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftPickTradeAssets_DraftPickAssetId",
                table: "DraftPickTradeAssets",
                column: "DraftPickAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftPickTradeAssets_DraftPickTradeId",
                table: "DraftPickTradeAssets",
                column: "DraftPickTradeId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftPickTrades_FromUserClubId",
                table: "DraftPickTrades",
                column: "FromUserClubId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftPickTrades_ToUserClubId",
                table: "DraftPickTrades",
                column: "ToUserClubId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DraftPickTradeAssets");

            migrationBuilder.DropTable(
                name: "DraftPickAssets");

            migrationBuilder.DropTable(
                name: "DraftPickTrades");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAtUTC",
                table: "ClubLineups",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
