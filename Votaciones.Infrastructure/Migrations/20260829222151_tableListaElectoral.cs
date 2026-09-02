using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votaciones.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class tableListaElectoral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActaDetalle_Candidato_CandidatoId",
                table: "ActaDetalle");

            migrationBuilder.DropColumn(
                name: "Lista",
                table: "Candidato");

            migrationBuilder.RenameColumn(
                name: "NumeroLista",
                table: "Candidato",
                newName: "TipoCandidato");

            migrationBuilder.AddColumn<Guid>(
                name: "ListaElectoralId",
                table: "Candidato",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Orden",
                table: "Candidato",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoCandidato",
                table: "ActaEleccion",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "CandidatoId",
                table: "ActaDetalle",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "ListaElectoralId",
                table: "ActaDetalle",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ListaElectoral",
                columns: table => new
                {
                    IdListaElectoral = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroLista = table.Column<int>(type: "int", nullable: false),
                    NombreLista = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Siglas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Jurisdiccion = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListaElectoral", x => x.IdListaElectoral);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candidato_ListaElectoralId",
                table: "Candidato",
                column: "ListaElectoralId");

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalle_ListaElectoralId",
                table: "ActaDetalle",
                column: "ListaElectoralId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActaDetalle_Candidato_CandidatoId",
                table: "ActaDetalle",
                column: "CandidatoId",
                principalTable: "Candidato",
                principalColumn: "IdCandidato",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActaDetalle_ListaElectoral_ListaElectoralId",
                table: "ActaDetalle",
                column: "ListaElectoralId",
                principalTable: "ListaElectoral",
                principalColumn: "IdListaElectoral",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Candidato_ListaElectoral_ListaElectoralId",
                table: "Candidato",
                column: "ListaElectoralId",
                principalTable: "ListaElectoral",
                principalColumn: "IdListaElectoral",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActaDetalle_Candidato_CandidatoId",
                table: "ActaDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_ActaDetalle_ListaElectoral_ListaElectoralId",
                table: "ActaDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_Candidato_ListaElectoral_ListaElectoralId",
                table: "Candidato");

            migrationBuilder.DropTable(
                name: "ListaElectoral");

            migrationBuilder.DropIndex(
                name: "IX_Candidato_ListaElectoralId",
                table: "Candidato");

            migrationBuilder.DropIndex(
                name: "IX_ActaDetalle_ListaElectoralId",
                table: "ActaDetalle");

            migrationBuilder.DropColumn(
                name: "ListaElectoralId",
                table: "Candidato");

            migrationBuilder.DropColumn(
                name: "Orden",
                table: "Candidato");

            migrationBuilder.DropColumn(
                name: "TipoCandidato",
                table: "ActaEleccion");

            migrationBuilder.DropColumn(
                name: "ListaElectoralId",
                table: "ActaDetalle");

            migrationBuilder.RenameColumn(
                name: "TipoCandidato",
                table: "Candidato",
                newName: "NumeroLista");

            migrationBuilder.AddColumn<string>(
                name: "Lista",
                table: "Candidato",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CandidatoId",
                table: "ActaDetalle",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ActaDetalle_Candidato_CandidatoId",
                table: "ActaDetalle",
                column: "CandidatoId",
                principalTable: "Candidato",
                principalColumn: "IdCandidato",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
