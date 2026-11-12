using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfigurationCollaborateurs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Contrats",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quotite",
                table: "Contrats",
                type: "numeric(3,2)",
                precision: 3,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Prenom",
                table: "Collaborateurs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Poste",
                table: "Collaborateurs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Nom",
                table: "Collaborateurs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Matricule",
                table: "Collaborateurs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Collaborateurs",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Categorie",
                table: "Collaborateurs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Collaborateurs_Email",
                table: "Collaborateurs",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Collaborateurs_ManagerId",
                table: "Collaborateurs",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Collaborateurs_Matricule",
                table: "Collaborateurs",
                column: "Matricule",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborateurs_Collaborateurs_ManagerId",
                table: "Collaborateurs",
                column: "ManagerId",
                principalTable: "Collaborateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collaborateurs_Collaborateurs_ManagerId",
                table: "Collaborateurs");

            migrationBuilder.DropIndex(
                name: "IX_Collaborateurs_Email",
                table: "Collaborateurs");

            migrationBuilder.DropIndex(
                name: "IX_Collaborateurs_ManagerId",
                table: "Collaborateurs");

            migrationBuilder.DropIndex(
                name: "IX_Collaborateurs_Matricule",
                table: "Collaborateurs");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Contrats",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quotite",
                table: "Contrats",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(3,2)",
                oldPrecision: 3,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Prenom",
                table: "Collaborateurs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Poste",
                table: "Collaborateurs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Nom",
                table: "Collaborateurs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Matricule",
                table: "Collaborateurs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Collaborateurs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254);

            migrationBuilder.AlterColumn<int>(
                name: "Categorie",
                table: "Collaborateurs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
