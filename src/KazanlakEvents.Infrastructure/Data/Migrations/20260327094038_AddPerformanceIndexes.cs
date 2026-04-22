using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KazanlakEvents.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_TicketTypeId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Comments_EventId",
                table: "Comments");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TicketTypeId_Status",
                table: "Tickets",
                columns: new[] { "TicketTypeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_CreatedAt",
                table: "Orders",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId",
                table: "Favorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CategoryId_Status_StartDate",
                table: "Events",
                columns: new[] { "CategoryId", "Status", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Status_StartDate",
                table: "Events",
                columns: new[] { "Status", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_EventId_IsHidden_ParentCommentId",
                table: "Comments",
                columns: new[] { "EventId", "IsHidden", "ParentCommentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_TicketTypeId_Status",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_UserId",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Events_CategoryId_Status_StartDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_Status_StartDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Comments_EventId_IsHidden_ParentCommentId",
                table: "Comments");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TicketTypeId",
                table: "Tickets",
                column: "TicketTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_EventId",
                table: "Comments",
                column: "EventId");
        }
    }
}
