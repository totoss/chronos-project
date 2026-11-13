using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Chronos.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class TypesAbsenceReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "TypesAbsence",
                columns: new[] { "Id", "Code", "Libelle" },
                values: new object[,]
                {
                    { 1, "CP", "Congés payés" },
                    { 2, "RTT", "Réduction du temps de travail" },
                    { 3, "MAL", "Maladie" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TypesAbsence",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TypesAbsence",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TypesAbsence",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
