using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniquePlayerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PlayerModels_Name",
                table: "PlayerModels",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerModels_Name",
                table: "PlayerModels");
        }
    }
}
