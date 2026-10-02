using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedProfessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Professions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Professions_UserId",
                table: "Professions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Professions_Users_UserId",
                table: "Professions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Professions_Users_UserId",
                table: "Professions");

            migrationBuilder.DropIndex(
                name: "IX_Professions_UserId",
                table: "Professions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Professions");
        }
    }
}
