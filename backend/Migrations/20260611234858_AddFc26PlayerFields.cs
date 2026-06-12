using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFc26PlayerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "FutbinId",
                table: "GamePlayers",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "GamePlayers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "GamePlayers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HeightCm",
                table: "GamePlayers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "GamePlayers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Potential",
                table: "GamePlayers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PreferredFoot",
                table: "GamePlayers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SofifaPlayerId",
                table: "GamePlayers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WeightKg",
                table: "GamePlayers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "Potential",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "PreferredFoot",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "SofifaPlayerId",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "GamePlayers");

            migrationBuilder.AlterColumn<int>(
                name: "FutbinId",
                table: "GamePlayers",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
