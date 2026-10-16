using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votaciones.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class tipoEleccionparroquiaRural : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ParroquiaRural",
                table: "Parroquia",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TipoEleccion",
                table: "Eleccion",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParroquiaRural",
                table: "Parroquia");

            migrationBuilder.DropColumn(
                name: "TipoEleccion",
                table: "Eleccion");
        }
    }
}
