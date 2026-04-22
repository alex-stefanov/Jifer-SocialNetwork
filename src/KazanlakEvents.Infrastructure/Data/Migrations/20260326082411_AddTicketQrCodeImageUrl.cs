using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KazanlakEvents.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketQrCodeImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrCodeImageUrl",
                table: "Tickets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QrCodeImageUrl",
                table: "Tickets");
        }
    }
}
