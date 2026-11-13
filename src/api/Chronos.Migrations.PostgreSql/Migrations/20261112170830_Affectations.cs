using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Chronos.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class Affectations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Affectations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CollaborateurId = table.Column<int>(type: "integer", nullable: false),
                    ProjetId = table.Column<int>(type: "integer", nullable: false),
                    DateDebut = table.Column<DateOnly>(type: "date", nullable: false),
                    DateFin = table.Column<DateOnly>(type: "date", nullable: false),
                    TauxAllocation = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Affectations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Affectations_Collaborateurs_CollaborateurId",
                        column: x => x.CollaborateurId,
                        principalTable: "Collaborateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Affectations_Projets_ProjetId",
                        column: x => x.ProjetId,
                        principalTable: "Projets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JoursFeries",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoursFeries", x => x.Date);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Affectations_CollaborateurId_DateDebut",
                table: "Affectations",
                columns: new[] { "CollaborateurId", "DateDebut" });

            migrationBuilder.CreateIndex(
                name: "IX_Affectations_ProjetId",
                table: "Affectations",
                column: "ProjetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Affectations");

            migrationBuilder.DropTable(
                name: "JoursFeries");
        }
    }
}
