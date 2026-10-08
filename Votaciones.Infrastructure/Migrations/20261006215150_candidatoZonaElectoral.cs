using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votaciones.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class candidatoZonaElectoral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CantonId",
                table: "Candidato",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParroquiaId",
                table: "Candidato",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProvinciaId",
                table: "Candidato",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Candidato_CantonId",
                table: "Candidato",
                column: "CantonId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidato_ParroquiaId",
                table: "Candidato",
                column: "ParroquiaId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidato_ProvinciaId",
                table: "Candidato",
                column: "ProvinciaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidato_Canton_CantonId",
                table: "Candidato",
                column: "CantonId",
                principalTable: "Canton",
                principalColumn: "IdCanton");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidato_Parroquia_ParroquiaId",
                table: "Candidato",
                column: "ParroquiaId",
                principalTable: "Parroquia",
                principalColumn: "IdParroquia");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidato_Provincia_ProvinciaId",
                table: "Candidato",
                column: "ProvinciaId",
                principalTable: "Provincia",
                principalColumn: "IdProvincia");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Candidato_Canton_CantonId",
                table: "Candidato");

            migrationBuilder.DropForeignKey(
                name: "FK_Candidato_Parroquia_ParroquiaId",
                table: "Candidato");

            migrationBuilder.DropForeignKey(
                name: "FK_Candidato_Provincia_ProvinciaId",
                table: "Candidato");

            migrationBuilder.DropIndex(
                name: "IX_Candidato_CantonId",
                table: "Candidato");

            migrationBuilder.DropIndex(
                name: "IX_Candidato_ParroquiaId",
                table: "Candidato");

            migrationBuilder.DropIndex(
                name: "IX_Candidato_ProvinciaId",
                table: "Candidato");

            migrationBuilder.DropColumn(
                name: "CantonId",
                table: "Candidato");

            migrationBuilder.DropColumn(
                name: "ParroquiaId",
                table: "Candidato");

            migrationBuilder.DropColumn(
                name: "ProvinciaId",
                table: "Candidato");
        }
    }
}
