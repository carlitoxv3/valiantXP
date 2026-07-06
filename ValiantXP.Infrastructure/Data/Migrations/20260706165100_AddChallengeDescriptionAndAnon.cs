using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ValiantXP.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeDescriptionAndAnon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AnonParticipationAllowed",
                table: "DynamicChallenges",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "DynamicChallenges",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnonParticipationAllowed",
                table: "DynamicChallenges");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "DynamicChallenges");
        }
    }
}
