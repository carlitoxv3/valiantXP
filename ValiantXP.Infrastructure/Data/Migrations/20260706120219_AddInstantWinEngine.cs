using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ValiantXP.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInstantWinEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserPrizes_PrizeId",
                table: "UserPrizes");

            migrationBuilder.DropIndex(
                name: "IX_UserPrizes_UserId",
                table: "UserPrizes");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "UserPrizes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GiftCardCode",
                table: "UserPrizes",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRedeemed",
                table: "UserPrizes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PointsAwarded",
                table: "UserPrizes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PrizeType",
                table: "UserPrizes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "RedeemedAt",
                table: "UserPrizes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmissionId",
                table: "UserPrizes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Prizes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<bool>(
                name: "AllowNoWin",
                table: "Prizes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Prizes",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalReference",
                table: "Prizes",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Prizes",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxGlobalInWindow",
                table: "Prizes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxPerUserInWindow",
                table: "Prizes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointMultiplier",
                table: "Prizes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsExpirationDays",
                table: "Prizes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PrizeType",
                table: "Prizes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "WindowHours",
                table: "Prizes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RallySubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DynamicChallengeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RallyType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsWinner = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MediaUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TextContent = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TicketDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubChallengeTag = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RemoteIp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModeratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModeratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModerationNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RallySubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RallySubmissions_DynamicChallenges_DynamicChallengeId",
                        column: x => x.DynamicChallengeId,
                        principalTable: "DynamicChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RallySubmissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPointMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PrizeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPointMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPointMovements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RallySubmissionVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RallySubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RemoteIp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VotedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RallySubmissionVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RallySubmissionVotes_RallySubmissions_RallySubmissionId",
                        column: x => x.RallySubmissionId,
                        principalTable: "RallySubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RallySubmissionVotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPrizes_PrizeId_AwardedAt",
                table: "UserPrizes",
                columns: new[] { "PrizeId", "AwardedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPrizes_UserId_PrizeId",
                table: "UserPrizes",
                columns: new[] { "UserId", "PrizeId" });

            migrationBuilder.CreateIndex(
                name: "IX_RallySubmissions_DynamicChallengeId_Status",
                table: "RallySubmissions",
                columns: new[] { "DynamicChallengeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RallySubmissions_DynamicChallengeId_UserId",
                table: "RallySubmissions",
                columns: new[] { "DynamicChallengeId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_RallySubmissions_SubmissionCode",
                table: "RallySubmissions",
                column: "SubmissionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RallySubmissions_UserId",
                table: "RallySubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RallySubmissionVotes_RallySubmissionId",
                table: "RallySubmissionVotes",
                column: "RallySubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RallySubmissionVotes_UserId_RallySubmissionId",
                table: "RallySubmissionVotes",
                columns: new[] { "UserId", "RallySubmissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RallySubmissionVotes_UserId_VotedAt",
                table: "RallySubmissionVotes",
                columns: new[] { "UserId", "VotedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPointMovements_CreatedAt",
                table: "UserPointMovements",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserPointMovements_UserId_ExpiresAt",
                table: "UserPointMovements",
                columns: new[] { "UserId", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RallySubmissionVotes");

            migrationBuilder.DropTable(
                name: "UserPointMovements");

            migrationBuilder.DropTable(
                name: "RallySubmissions");

            migrationBuilder.DropIndex(
                name: "IX_UserPrizes_PrizeId_AwardedAt",
                table: "UserPrizes");

            migrationBuilder.DropIndex(
                name: "IX_UserPrizes_UserId_PrizeId",
                table: "UserPrizes");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "UserPrizes");

            migrationBuilder.DropColumn(
                name: "GiftCardCode",
                table: "UserPrizes");

            migrationBuilder.DropColumn(
                name: "IsRedeemed",
                table: "UserPrizes");

            migrationBuilder.DropColumn(
                name: "PointsAwarded",
                table: "UserPrizes");

            migrationBuilder.DropColumn(
                name: "PrizeType",
                table: "UserPrizes");

            migrationBuilder.DropColumn(
                name: "RedeemedAt",
                table: "UserPrizes");

            migrationBuilder.DropColumn(
                name: "SubmissionId",
                table: "UserPrizes");

            migrationBuilder.DropColumn(
                name: "AllowNoWin",
                table: "Prizes");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Prizes");

            migrationBuilder.DropColumn(
                name: "ExternalReference",
                table: "Prizes");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Prizes");

            migrationBuilder.DropColumn(
                name: "MaxGlobalInWindow",
                table: "Prizes");

            migrationBuilder.DropColumn(
                name: "MaxPerUserInWindow",
                table: "Prizes");

            migrationBuilder.DropColumn(
                name: "PointMultiplier",
                table: "Prizes");

            migrationBuilder.DropColumn(
                name: "PointsExpirationDays",
                table: "Prizes");

            migrationBuilder.DropColumn(
                name: "PrizeType",
                table: "Prizes");

            migrationBuilder.DropColumn(
                name: "WindowHours",
                table: "Prizes");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Prizes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPrizes_PrizeId",
                table: "UserPrizes",
                column: "PrizeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPrizes_UserId",
                table: "UserPrizes",
                column: "UserId");
        }
    }
}
