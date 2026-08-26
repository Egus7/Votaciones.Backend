using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votaciones.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class tablesZonas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ZonaId",
                table: "MesaElectoral",
                type: "uniqueidentifier",
                nullable: true,
                defaultValue: null);

            migrationBuilder.AlterColumn<string>(
                name: "Lista",
                table: "Candidato",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaModificacion",
                table: "ActaEleccion",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.CreateTable(
                name: "Provincia",
                columns: table => new
                {
                    IdProvincia = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoProvincia = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    NombreProvincia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provincia", x => x.IdProvincia);
                });

            migrationBuilder.CreateTable(
                name: "Canton",
                columns: table => new
                {
                    IdCanton = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProvinciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreCanton = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Canton", x => x.IdCanton);
                    table.ForeignKey(
                        name: "FK_Canton_Provincia_ProvinciaId",
                        column: x => x.ProvinciaId,
                        principalTable: "Provincia",
                        principalColumn: "IdProvincia",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Parroquia",
                columns: table => new
                {
                    IdParroquia = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CantonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreParroquia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parroquia", x => x.IdParroquia);
                    table.ForeignKey(
                        name: "FK_Parroquia_Canton_CantonId",
                        column: x => x.CantonId,
                        principalTable: "Canton",
                        principalColumn: "IdCanton",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Zona",
                columns: table => new
                {
                    IdZona = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParroquiaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreZona = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zona", x => x.IdZona);
                    table.ForeignKey(
                        name: "FK_Zona_Parroquia_ParroquiaId",
                        column: x => x.ParroquiaId,
                        principalTable: "Parroquia",
                        principalColumn: "IdParroquia",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MesaElectoral_ZonaId",
                table: "MesaElectoral",
                column: "ZonaId");

            migrationBuilder.CreateIndex(
                name: "IX_Canton_ProvinciaId",
                table: "Canton",
                column: "ProvinciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Parroquia_CantonId",
                table: "Parroquia",
                column: "CantonId");

            migrationBuilder.CreateIndex(
                name: "IX_Zona_ParroquiaId",
                table: "Zona",
                column: "ParroquiaId");

            migrationBuilder.AddForeignKey(
                name: "FK_MesaElectoral_Zona_ZonaId",
                table: "MesaElectoral",
                column: "ZonaId",
                principalTable: "Zona",
                principalColumn: "IdZona");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MesaElectoral_Zona_ZonaId",
                table: "MesaElectoral");

            migrationBuilder.DropTable(
                name: "Zona");

            migrationBuilder.DropTable(
                name: "Parroquia");

            migrationBuilder.DropTable(
                name: "Canton");

            migrationBuilder.DropTable(
                name: "Provincia");

            migrationBuilder.DropIndex(
                name: "IX_MesaElectoral_ZonaId",
                table: "MesaElectoral");

            migrationBuilder.DropColumn(
                name: "ZonaId",
                table: "MesaElectoral");

            migrationBuilder.AlterColumn<string>(
                name: "Lista",
                table: "Candidato",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaModificacion",
                table: "ActaEleccion",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
