using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votaciones.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class tableAlianzaElectoral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AlianzaElectoralId",
                table: "Candidato",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AlianzaElectoral",
                columns: table => new
                {
                    IdAlianzaElectoral = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreAlianza = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EleccionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProvinciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CantonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParroquiaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlianzaElectoral", x => x.IdAlianzaElectoral);
                    table.ForeignKey(
                        name: "FK_AlianzaElectoral_Canton_CantonId",
                        column: x => x.CantonId,
                        principalTable: "Canton",
                        principalColumn: "IdCanton");
                    table.ForeignKey(
                        name: "FK_AlianzaElectoral_Eleccion_EleccionId",
                        column: x => x.EleccionId,
                        principalTable: "Eleccion",
                        principalColumn: "IdEleccion",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlianzaElectoral_Parroquia_ParroquiaId",
                        column: x => x.ParroquiaId,
                        principalTable: "Parroquia",
                        principalColumn: "IdParroquia");
                    table.ForeignKey(
                        name: "FK_AlianzaElectoral_Provincia_ProvinciaId",
                        column: x => x.ProvinciaId,
                        principalTable: "Provincia",
                        principalColumn: "IdProvincia");
                });

            migrationBuilder.CreateTable(
                name: "AlianzaDetalle",
                columns: table => new
                {
                    IdAlianzaDetalle = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlianzaElectoralId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListaElectoralId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListaAlianzaPrincipal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlianzaDetalle", x => x.IdAlianzaDetalle);
                    table.ForeignKey(
                        name: "FK_AlianzaDetalle_AlianzaElectoral_AlianzaElectoralId",
                        column: x => x.AlianzaElectoralId,
                        principalTable: "AlianzaElectoral",
                        principalColumn: "IdAlianzaElectoral",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlianzaDetalle_ListaElectoral_ListaElectoralId",
                        column: x => x.ListaElectoralId,
                        principalTable: "ListaElectoral",
                        principalColumn: "IdListaElectoral",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candidato_AlianzaElectoralId",
                table: "Candidato",
                column: "AlianzaElectoralId");

            migrationBuilder.CreateIndex(
                name: "IX_AlianzaDetalle_AlianzaElectoralId",
                table: "AlianzaDetalle",
                column: "AlianzaElectoralId");

            migrationBuilder.CreateIndex(
                name: "IX_AlianzaDetalle_ListaElectoralId",
                table: "AlianzaDetalle",
                column: "ListaElectoralId");

            migrationBuilder.CreateIndex(
                name: "IX_AlianzaElectoral_CantonId",
                table: "AlianzaElectoral",
                column: "CantonId");

            migrationBuilder.CreateIndex(
                name: "IX_AlianzaElectoral_EleccionId",
                table: "AlianzaElectoral",
                column: "EleccionId");

            migrationBuilder.CreateIndex(
                name: "IX_AlianzaElectoral_ParroquiaId",
                table: "AlianzaElectoral",
                column: "ParroquiaId");

            migrationBuilder.CreateIndex(
                name: "IX_AlianzaElectoral_ProvinciaId",
                table: "AlianzaElectoral",
                column: "ProvinciaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidato_AlianzaElectoral_AlianzaElectoralId",
                table: "Candidato",
                column: "AlianzaElectoralId",
                principalTable: "AlianzaElectoral",
                principalColumn: "IdAlianzaElectoral",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Candidato_AlianzaElectoral_AlianzaElectoralId",
                table: "Candidato");

            migrationBuilder.DropTable(
                name: "AlianzaDetalle");

            migrationBuilder.DropTable(
                name: "AlianzaElectoral");

            migrationBuilder.DropIndex(
                name: "IX_Candidato_AlianzaElectoralId",
                table: "Candidato");

            migrationBuilder.DropColumn(
                name: "AlianzaElectoralId",
                table: "Candidato");
        }
    }
}
