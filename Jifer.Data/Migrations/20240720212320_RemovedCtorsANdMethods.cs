using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jifer.Data.Migrations
{
    public partial class RemovedCtorsANdMethods : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FriendShips_AspNetUsers_WithdrawnById",
                table: "FriendShips");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Posts",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                comment: "Content of the JGo",
                oldClrType: typeof(string),
                oldType: "nvarchar(1500)",
                oldMaxLength: 1500,
                oldComment: "Content of the JGo");

            migrationBuilder.AlterColumn<string>(
                name: "WithdrawnById",
                table: "FriendShips",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                comment: "Id of the user who withdrew the JShip",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FriendShips_AspNetUsers_WithdrawnById",
                table: "FriendShips",
                column: "WithdrawnById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FriendShips_AspNetUsers_WithdrawnById",
                table: "FriendShips");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Posts",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: false,
                comment: "Content of the JGo",
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldComment: "Content of the JGo");

            migrationBuilder.AlterColumn<string>(
                name: "WithdrawnById",
                table: "FriendShips",
                type: "nvarchar(450)",
                nullable: true,
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
    }
}
