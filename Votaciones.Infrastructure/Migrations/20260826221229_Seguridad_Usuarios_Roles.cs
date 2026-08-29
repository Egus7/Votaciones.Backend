using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votaciones.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Seguridad_Usuarios_Roles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmUsuario_AdmRol_RolId",
                table: "AdmUsuario");

            migrationBuilder.DropForeignKey(
                name: "FK_MesaElectoral_Zona_ZonaId",
                table: "MesaElectoral");

            migrationBuilder.DropIndex(
                name: "IX_AdmUsuario_NombreUsuario",
                table: "AdmUsuario");

            migrationBuilder.AlterColumn<Guid>(
                name: "ZonaId",
                table: "MesaElectoral",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AdmUsuario_AdmRol_RolId",
                table: "AdmUsuario",
                column: "RolId",
                principalTable: "AdmRol",
                principalColumn: "IdRol",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MesaElectoral_Zona_ZonaId",
                table: "MesaElectoral",
                column: "ZonaId",
                principalTable: "Zona",
                principalColumn: "IdZona",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmUsuario_AdmRol_RolId",
                table: "AdmUsuario");

            migrationBuilder.DropForeignKey(
                name: "FK_MesaElectoral_Zona_ZonaId",
                table: "MesaElectoral");

            migrationBuilder.AlterColumn<Guid>(
                name: "ZonaId",
                table: "MesaElectoral",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_AdmUsuario_NombreUsuario",
                table: "AdmUsuario",
                column: "NombreUsuario",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AdmUsuario_AdmRol_RolId",
                table: "AdmUsuario",
                column: "RolId",
                principalTable: "AdmRol",
                principalColumn: "IdRol",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MesaElectoral_Zona_ZonaId",
                table: "MesaElectoral",
                column: "ZonaId",
                principalTable: "Zona",
                principalColumn: "IdZona");
        }
    }
}
