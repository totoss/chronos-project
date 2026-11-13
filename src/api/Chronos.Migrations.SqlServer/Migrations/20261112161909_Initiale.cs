using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronos.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class Initiale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Collaborateurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Matricule = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Prenom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Categorie = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Poste = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateEntree = table.Column<DateOnly>(type: "date", nullable: false),
                    DateSortie = table.Column<DateOnly>(type: "date", nullable: true),
                    ManagerId = table.Column<int>(type: "int", nullable: true),
                    EstArchive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collaborateurs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Collaborateurs_Collaborateurs_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Collaborateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contrats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollaborateurId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quotite = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    DateDebut = table.Column<DateOnly>(type: "date", nullable: false),
                    DateFin = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contrats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contrats_Collaborateurs_CollaborateurId",
                        column: x => x.CollaborateurId,
                        principalTable: "Collaborateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Nature = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    DateDebut = table.Column<DateOnly>(type: "date", nullable: false),
                    DateFin = table.Column<DateOnly>(type: "date", nullable: true),
                    BudgetJours = table.Column<int>(type: "int", nullable: false),
                    ResponsableId = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projets_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projets_Collaborateurs_ResponsableId",
                        column: x => x.ResponsableId,
                        principalTable: "Collaborateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Contrats_CollaborateurId",
                table: "Contrats",
                column: "CollaborateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Projets_ClientId",
                table: "Projets",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Projets_Code",
                table: "Projets",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projets_ResponsableId",
                table: "Projets",
                column: "ResponsableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contrats");

            migrationBuilder.DropTable(
                name: "Projets");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Collaborateurs");
        }
    }
}
