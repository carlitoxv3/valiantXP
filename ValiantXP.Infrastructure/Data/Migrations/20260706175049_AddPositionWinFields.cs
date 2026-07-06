using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ValiantXP.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionWinFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBaseReward",
                table: "Prizes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPositionalReward",
                table: "Prizes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PositionWinConfigJson",
                table: "DynamicChallenges",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBaseReward",
                table: "Prizes");

            migrationBuilder.DropColumn(
                name: "IsPositionalReward",
                table: "Prizes");

            migrationBuilder.DropColumn(
                name: "PositionWinConfigJson",
                table: "DynamicChallenges");
        }
    }
}
