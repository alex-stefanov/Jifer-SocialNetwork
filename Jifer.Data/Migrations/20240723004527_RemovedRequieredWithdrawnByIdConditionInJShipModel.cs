using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jifer.Data.Migrations
{
    public partial class RemovedRequieredWithdrawnByIdConditionInJShipModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FriendShips_AspNetUsers_WithdrawnById",
                table: "FriendShips");

            migrationBuilder.AlterColumn<string>(
                name: "WithdrawnById",
                table: "FriendShips",
                type: "nvarchar(450)",
                nullable: true,
                comment: "Id of the user who withdrew the JShip",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldComment: "Id of the user who withdrew the JShip");

            migrationBuilder.AddForeignKey(
                name: "FK_FriendShips_AspNetUsers_WithdrawnById",
                table: "FriendShips",
                column: "WithdrawnById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FriendShips_AspNetUsers_WithdrawnById",
                table: "FriendShips");

            migrationBuilder.AlterColumn<string>(
                name: "WithdrawnById",
                table: "FriendShips",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                comment: "Id of the user who withdrew the JShip",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true,
                oldComment: "Id of the user who withdrew the JShip");

            migrationBuilder.AddForeignKey(
                name: "FK_FriendShips_AspNetUsers_WithdrawnById",
                table: "FriendShips",
                column: "WithdrawnById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
