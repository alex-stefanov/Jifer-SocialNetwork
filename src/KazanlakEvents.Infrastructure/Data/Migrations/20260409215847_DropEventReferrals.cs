using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KazanlakEvents.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropEventReferrals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventReferrals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventReferrals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClickCount = table.Column<int>(type: "int", nullable: false),
                    ConversionCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferralCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferrerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventReferrals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventReferrals_EventId_ReferrerUserId",
                table: "EventReferrals",
                columns: new[] { "EventId", "ReferrerUserId" },
                unique: true,
                filter: "[ReferrerUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventReferrals_ReferralCode",
                table: "EventReferrals",
                column: "ReferralCode",
                unique: true);
        }
    }
}
