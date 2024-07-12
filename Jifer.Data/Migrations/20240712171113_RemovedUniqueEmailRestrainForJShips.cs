using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jifer.Data.Migrations
{
    public partial class RemovedUniqueEmailRestrainForJShips : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invitations_InviteeEmail",
                table: "Invitations");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Invitations_InviteeEmail",
                table: "Invitations",
                column: "InviteeEmail",
                unique: true);
        }
    }
}
