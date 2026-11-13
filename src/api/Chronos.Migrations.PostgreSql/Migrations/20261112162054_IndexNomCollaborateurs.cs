using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronos.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class IndexNomCollaborateurs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Collaborateurs_Nom",
                table: "Collaborateurs",
                column: "Nom");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Collaborateurs_Nom",
                table: "Collaborateurs");
        }
    }
}
