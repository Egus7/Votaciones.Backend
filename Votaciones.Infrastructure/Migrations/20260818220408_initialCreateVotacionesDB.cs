using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votaciones.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initialCreateVotacionesDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmRol",
                columns: table => new
                {
                    IdRol = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreRol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DescripcionRol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PermisosRol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmRol", x => x.IdRol);
                });

            migrationBuilder.CreateTable(
                name: "Eleccion",
                columns: table => new
                {
                    IdEleccion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreEleccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaEleccion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eleccion", x => x.IdEleccion);
                });

            migrationBuilder.CreateTable(
                name: "AdmUsuario",
                columns: table => new
                {
                    IdUsuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreUsuario = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    EmailUsuario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmUsuario", x => x.IdUsuario);
                    table.ForeignKey(
                        name: "FK_AdmUsuario_AdmRol_RolId",
                        column: x => x.RolId,
                        principalTable: "AdmRol",
                        principalColumn: "IdRol",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Candidato",
                columns: table => new
                {
                    IdCandidato = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EleccionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreCandidato = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NumeroLista = table.Column<int>(type: "int", maxLength: 100, nullable: false),
                    Lista = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidato", x => x.IdCandidato);
                    table.ForeignKey(
                        name: "FK_Candidato_Eleccion_EleccionId",
                        column: x => x.EleccionId,
                        principalTable: "Eleccion",
                        principalColumn: "IdEleccion",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MesaElectoral",
                columns: table => new
                {
                    IdMesaElectoral = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EleccionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoMesa = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TipoMesa = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MesaElectoral", x => x.IdMesaElectoral);
                    table.ForeignKey(
                        name: "FK_MesaElectoral_Eleccion_EleccionId",
                        column: x => x.EleccionId,
                        principalTable: "Eleccion",
                        principalColumn: "IdEleccion",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmBitacora",
                columns: table => new
                {
                    IdBitacora = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EleccionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tabla = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdRegistro = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValoresAnteriores = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValoresNuevos = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmBitacora", x => x.IdBitacora);
                    table.CheckConstraint("CK_AdmBitacora_ValoresAnteriores_Json", "[ValoresAnteriores] IS NULL OR ISJSON([ValoresAnteriores]) = 1");
                    table.CheckConstraint("CK_AdmBitacora_ValoresNuevos_Json", "[ValoresNuevos] IS NULL OR ISJSON([ValoresNuevos]) = 1");
                    table.ForeignKey(
                        name: "FK_AdmBitacora_AdmUsuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AdmUsuario",
                        principalColumn: "IdUsuario");
                    table.ForeignKey(
                        name: "FK_AdmBitacora_Eleccion_EleccionId",
                        column: x => x.EleccionId,
                        principalTable: "Eleccion",
                        principalColumn: "IdEleccion");
                });

            migrationBuilder.CreateTable(
                name: "ActaEleccion",
                columns: table => new
                {
                    IdActa = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EleccionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MesaElectoralId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioRegistroId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VotosBlancos = table.Column<int>(type: "int", nullable: false),
                    VotosNulos = table.Column<int>(type: "int", nullable: false),
                    TotalVotos = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioModificacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActaEleccion", x => x.IdActa);
                    table.ForeignKey(
                        name: "FK_ActaEleccion_Eleccion_EleccionId",
                        column: x => x.EleccionId,
                        principalTable: "Eleccion",
                        principalColumn: "IdEleccion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActaEleccion_MesaElectoral_MesaElectoralId",
                        column: x => x.MesaElectoralId,
                        principalTable: "MesaElectoral",
                        principalColumn: "IdMesaElectoral",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActaDetalle",
                columns: table => new
                {
                    IdActaDetalle = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidatoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Votos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActaDetalle", x => x.IdActaDetalle);
                    table.ForeignKey(
                        name: "FK_ActaDetalle_ActaEleccion_ActaId",
                        column: x => x.ActaId,
                        principalTable: "ActaEleccion",
                        principalColumn: "IdActa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActaDetalle_Candidato_CandidatoId",
                        column: x => x.CandidatoId,
                        principalTable: "Candidato",
                        principalColumn: "IdCandidato",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalle_ActaId",
                table: "ActaDetalle",
                column: "ActaId");

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalle_CandidatoId",
                table: "ActaDetalle",
                column: "CandidatoId");

            migrationBuilder.CreateIndex(
                name: "IX_ActaEleccion_EleccionId",
                table: "ActaEleccion",
                column: "EleccionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActaEleccion_MesaElectoralId",
                table: "ActaEleccion",
                column: "MesaElectoralId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmBitacora_EleccionId",
                table: "AdmBitacora",
                column: "EleccionId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmBitacora_UsuarioId",
                table: "AdmBitacora",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmUsuario_NombreUsuario",
                table: "AdmUsuario",
                column: "NombreUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmUsuario_RolId",
                table: "AdmUsuario",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidato_EleccionId",
                table: "Candidato",
                column: "EleccionId");

            migrationBuilder.CreateIndex(
                name: "IX_MesaElectoral_EleccionId",
                table: "MesaElectoral",
                column: "EleccionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActaDetalle");

            migrationBuilder.DropTable(
                name: "AdmBitacora");

            migrationBuilder.DropTable(
                name: "ActaEleccion");

            migrationBuilder.DropTable(
                name: "Candidato");

            migrationBuilder.DropTable(
                name: "AdmUsuario");

            migrationBuilder.DropTable(
                name: "MesaElectoral");

            migrationBuilder.DropTable(
                name: "AdmRol");

            migrationBuilder.DropTable(
                name: "Eleccion");
        }
    }
}
