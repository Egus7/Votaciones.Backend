using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votaciones.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class metodoEscanosEleccion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MetodoEscanos",
                table: "Eleccion",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MetodoEscanos",
                table: "Eleccion");
        }
    }
}
