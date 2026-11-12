using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Chronos.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class FeuillesDeTemps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeuillesDeTemps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CollaborateurId = table.Column<int>(type: "integer", nullable: false),
                    Semaine = table.Column<DateOnly>(type: "date", nullable: false),
                    Statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MotifRejet = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeuillesDeTemps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeuillesDeTemps_Collaborateurs_CollaborateurId",
                        column: x => x.CollaborateurId,
                        principalTable: "Collaborateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaisiesDeTemps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FeuilleDeTempsId = table.Column<int>(type: "integer", nullable: false),
                    ProjetId = table.Column<int>(type: "integer", nullable: false),
                    Jour = table.Column<DateOnly>(type: "date", nullable: false),
                    Duree = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaisiesDeTemps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaisiesDeTemps_FeuillesDeTemps_FeuilleDeTempsId",
                        column: x => x.FeuilleDeTempsId,
                        principalTable: "FeuillesDeTemps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SaisiesDeTemps_Projets_ProjetId",
                        column: x => x.ProjetId,
                        principalTable: "Projets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeuillesDeTemps_CollaborateurId_Semaine",
                table: "FeuillesDeTemps",
                columns: new[] { "CollaborateurId", "Semaine" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaisiesDeTemps_FeuilleDeTempsId_ProjetId_Jour",
                table: "SaisiesDeTemps",
                columns: new[] { "FeuilleDeTempsId", "ProjetId", "Jour" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaisiesDeTemps_ProjetId",
                table: "SaisiesDeTemps",
                column: "ProjetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaisiesDeTemps");

            migrationBuilder.DropTable(
                name: "FeuillesDeTemps");
        }
    }
}
