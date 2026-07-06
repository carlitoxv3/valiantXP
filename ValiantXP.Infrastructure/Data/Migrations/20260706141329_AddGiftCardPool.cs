using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ValiantXP.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGiftCardPool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GiftCardProviderId",
                table: "Prizes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GiftCardProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InstructiveUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftCardProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftCardProviders_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GiftCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RedeemUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Pin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserPrizeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftCards_GiftCardProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "GiftCardProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiftCards_UserPrizes_UserPrizeId",
                        column: x => x.UserPrizeId,
                        principalTable: "UserPrizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GiftCards_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prizes_GiftCardProviderId",
                table: "Prizes",
                column: "GiftCardProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardProviders_CampaignId",
                table: "GiftCardProviders",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardProviders_Name",
                table: "GiftCardProviders",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCards_AssignedToUserId",
                table: "GiftCards",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCards_Provider_Available",
                table: "GiftCards",
                columns: new[] { "ProviderId", "AssignedToUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_GiftCards_UserPrizeId",
                table: "GiftCards",
                column: "UserPrizeId");

            migrationBuilder.CreateIndex(
                name: "UX_GiftCards_Provider_Code",
                table: "GiftCards",
                columns: new[] { "ProviderId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Prizes_GiftCardProviders_GiftCardProviderId",
                table: "Prizes",
                column: "GiftCardProviderId",
                principalTable: "GiftCardProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Data migration: existing PrizeType=3 (GiftCard deprecated) -> PrizeType=2 (Product)
            // GiftCard is now a DELIVERY MECHANISM on Product prizes via GiftCardProviderId.
            migrationBuilder.Sql("UPDATE Prizes SET PrizeType = 2 WHERE PrizeType = 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prizes_GiftCardProviders_GiftCardProviderId",
                table: "Prizes");

            // Reverse data migration: restore PrizeType=3 for prizes that had no pool provider assigned.
            // Prizes with GiftCardProviderId set stay as PrizeType=2 (they were real pool-based products).
            migrationBuilder.Sql("UPDATE Prizes SET PrizeType = 3 WHERE PrizeType = 2 AND GiftCardProviderId IS NULL");

            migrationBuilder.DropTable(
                name: "GiftCards");

            migrationBuilder.DropTable(
                name: "GiftCardProviders");

            migrationBuilder.DropIndex(
                name: "IX_Prizes_GiftCardProviderId",
                table: "Prizes");

            migrationBuilder.DropColumn(
                name: "GiftCardProviderId",
                table: "Prizes");
        }
    }
}
