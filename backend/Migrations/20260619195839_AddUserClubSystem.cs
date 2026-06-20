using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserClubSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClubLineupPlayers_ClubLineupId",
                table: "ClubLineupPlayers");

            migrationBuilder.DropColumn(
                name: "GameClubId",
                table: "ClubLineups");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ClubLineups",
                newName: "UserClubId");

            migrationBuilder.RenameColumn(
                name: "GamePlayerId",
                table: "ClubLineupPlayers",
                newName: "UserClubPlayerId");

            migrationBuilder.AlterColumn<string>(
                name: "Formation",
                table: "ClubLineups",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ClubLineups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ClubLineups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "ClubLineups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Position",
                table: "ClubLineupPlayers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUTC",
                table: "ClubLineupPlayers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ClubLineupPlayers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ClubLineupPlayers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUTC",
                table: "ClubLineupPlayers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "ClubLineupPlayers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserClubs",
                columns: table => new
                {
                    UserClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseGameClubId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    InitialBudget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentBudget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClubs", x => x.UserClubId);
                    table.ForeignKey(
                        name: "FK_UserClubs_GameClubs_BaseGameClubId",
                        column: x => x.BaseGameClubId,
                        principalTable: "GameClubs",
                        principalColumn: "GameClubId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserClubs_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "SeasonId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserClubs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferRecords",
                columns: table => new
                {
                    TransferRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    GamePlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUserClubId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToUserClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerMarketValue = table.Column<int>(type: "integer", nullable: true),
                    TransferFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransferDateUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TransferType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferRecords", x => x.TransferRecordId);
                    table.ForeignKey(
                        name: "FK_TransferRecords_GamePlayers_GamePlayerId",
                        column: x => x.GamePlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "GamePlayerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferRecords_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "SeasonId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferRecords_UserClubs_FromUserClubId",
                        column: x => x.FromUserClubId,
                        principalTable: "UserClubs",
                        principalColumn: "UserClubId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferRecords_UserClubs_ToUserClubId",
                        column: x => x.ToUserClubId,
                        principalTable: "UserClubs",
                        principalColumn: "UserClubId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserClubBudgetTransactions",
                columns: table => new
                {
                    UserClubBudgetTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClubBudgetTransactions", x => x.UserClubBudgetTransactionId);
                    table.ForeignKey(
                        name: "FK_UserClubBudgetTransactions_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "SeasonId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserClubBudgetTransactions_UserClubs_UserClubId",
                        column: x => x.UserClubId,
                        principalTable: "UserClubs",
                        principalColumn: "UserClubId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClubPlayers",
                columns: table => new
                {
                    UserClubPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    GamePlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcquisitionPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AcquiredAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcquisitionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClubPlayers", x => x.UserClubPlayerId);
                    table.ForeignKey(
                        name: "FK_UserClubPlayers_GamePlayers_GamePlayerId",
                        column: x => x.GamePlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "GamePlayerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserClubPlayers_UserClubs_UserClubId",
                        column: x => x.UserClubId,
                        principalTable: "UserClubs",
                        principalColumn: "UserClubId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClubLineups_UserClubId",
                table: "ClubLineups",
                column: "UserClubId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubLineupPlayers_ClubLineupId_SlotIndex",
                table: "ClubLineupPlayers",
                columns: new[] { "ClubLineupId", "SlotIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubLineupPlayers_UserClubPlayerId",
                table: "ClubLineupPlayers",
                column: "UserClubPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRecords_FromUserClubId",
                table: "TransferRecords",
                column: "FromUserClubId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRecords_GamePlayerId",
                table: "TransferRecords",
                column: "GamePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRecords_SeasonId",
                table: "TransferRecords",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRecords_ToUserClubId",
                table: "TransferRecords",
                column: "ToUserClubId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClubBudgetTransactions_SeasonId",
                table: "UserClubBudgetTransactions",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClubBudgetTransactions_UserClubId",
                table: "UserClubBudgetTransactions",
                column: "UserClubId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClubPlayers_GamePlayerId",
                table: "UserClubPlayers",
                column: "GamePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClubPlayers_UserClubId_GamePlayerId",
                table: "UserClubPlayers",
                columns: new[] { "UserClubId", "GamePlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserClubs_BaseGameClubId",
                table: "UserClubs",
                column: "BaseGameClubId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClubs_SeasonId",
                table: "UserClubs",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClubs_UserId_SeasonId",
                table: "UserClubs",
                columns: new[] { "UserId", "SeasonId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubLineupPlayers_UserClubPlayers_UserClubPlayerId",
                table: "ClubLineupPlayers",
                column: "UserClubPlayerId",
                principalTable: "UserClubPlayers",
                principalColumn: "UserClubPlayerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubLineups_UserClubs_UserClubId",
                table: "ClubLineups",
                column: "UserClubId",
                principalTable: "UserClubs",
                principalColumn: "UserClubId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubLineupPlayers_UserClubPlayers_UserClubPlayerId",
                table: "ClubLineupPlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_ClubLineups_UserClubs_UserClubId",
                table: "ClubLineups");

            migrationBuilder.DropTable(
                name: "TransferRecords");

            migrationBuilder.DropTable(
                name: "UserClubBudgetTransactions");

            migrationBuilder.DropTable(
                name: "UserClubPlayers");

            migrationBuilder.DropTable(
                name: "UserClubs");

            migrationBuilder.DropIndex(
                name: "IX_ClubLineups_UserClubId",
                table: "ClubLineups");

            migrationBuilder.DropIndex(
                name: "IX_ClubLineupPlayers_ClubLineupId_SlotIndex",
                table: "ClubLineupPlayers");

            migrationBuilder.DropIndex(
                name: "IX_ClubLineupPlayers_UserClubPlayerId",
                table: "ClubLineupPlayers");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ClubLineups");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ClubLineups");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ClubLineups");

            migrationBuilder.DropColumn(
                name: "CreatedAtUTC",
                table: "ClubLineupPlayers");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ClubLineupPlayers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ClubLineupPlayers");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUTC",
                table: "ClubLineupPlayers");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ClubLineupPlayers");

            migrationBuilder.RenameColumn(
                name: "UserClubId",
                table: "ClubLineups",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "UserClubPlayerId",
                table: "ClubLineupPlayers",
                newName: "GamePlayerId");

            migrationBuilder.AlterColumn<string>(
                name: "Formation",
                table: "ClubLineups",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<Guid>(
                name: "GameClubId",
                table: "ClubLineups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Position",
                table: "ClubLineupPlayers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_ClubLineupPlayers_ClubLineupId",
                table: "ClubLineupPlayers",
                column: "ClubLineupId");
        }
    }
}
