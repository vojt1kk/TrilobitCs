using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrilobitCS.Migrations
{
    /// <inheritdoc />
    public partial class AddModeratorNoteAndCascadeUefPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_UserEagleFeathers_UserEagleFeatherId",
                table: "Posts");

            migrationBuilder.AddColumn<string>(
                name: "ModeratorNote",
                table: "UserEagleFeathers",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_UserEagleFeathers_UserEagleFeatherId",
                table: "Posts",
                column: "UserEagleFeatherId",
                principalTable: "UserEagleFeathers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_UserEagleFeathers_UserEagleFeatherId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ModeratorNote",
                table: "UserEagleFeathers");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_UserEagleFeathers_UserEagleFeatherId",
                table: "Posts",
                column: "UserEagleFeatherId",
                principalTable: "UserEagleFeathers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
