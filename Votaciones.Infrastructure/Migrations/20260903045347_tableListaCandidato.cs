using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votaciones.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class tableListaCandidato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Candidato_ListaElectoral_ListaElectoralId",
                table: "Candidato");

            migrationBuilder.DropIndex(
                name: "IX_Candidato_ListaElectoralId",
                table: "Candidato");

            migrationBuilder.DropColumn(
                name: "ListaElectoralId",
                table: "Candidato");

            migrationBuilder.CreateTable(
                name: "ListaCandidato",
                columns: table => new
                {
                    IdListaCandidato = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidatoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListaElectoralId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListaPrincipal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListaCandidato", x => x.IdListaCandidato);
                    table.ForeignKey(
                        name: "FK_ListaCandidato_Candidato_CandidatoId",
                        column: x => x.CandidatoId,
                        principalTable: "Candidato",
                        principalColumn: "IdCandidato",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListaCandidato_ListaElectoral_ListaElectoralId",
                        column: x => x.ListaElectoralId,
                        principalTable: "ListaElectoral",
                        principalColumn: "IdListaElectoral",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListaCandidato_CandidatoId",
                table: "ListaCandidato",
                column: "CandidatoId");

            migrationBuilder.CreateIndex(
                name: "IX_ListaCandidato_ListaElectoralId",
                table: "ListaCandidato",
                column: "ListaElectoralId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListaCandidato");

            migrationBuilder.AddColumn<Guid>(
                name: "ListaElectoralId",
                table: "Candidato",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Candidato_ListaElectoralId",
                table: "Candidato",
                column: "ListaElectoralId");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidato_ListaElectoral_ListaElectoralId",
                table: "Candidato",
                column: "ListaElectoralId",
                principalTable: "ListaElectoral",
                principalColumn: "IdListaElectoral",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
