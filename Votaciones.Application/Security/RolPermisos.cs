
namespace Votaciones.Application.Security
{
    public static class RolPermisos
    {
        // Elecciones
        public const string EleccionesView = "elecciones.view";
        public const string EleccionesCreate = "elecciones.create";
        public const string EleccionesEdit = "elecciones.edit";
        // Listas
        public const string ListasView = "listas.view";
        public const string ListasCreate = "listas.create";
        public const string ListasEdit = "listas.edit";
        // Candidatos
        public const string CandidatosView = "candidatos.view";
        public const string CandidatosCreate = "candidatos.create";
        public const string CandidatosEdit = "candidatos.edit";
        // Mesas
        public const string MesasView = "mesas.view";
        public const string MesasCreate = "mesas.create";
        public const string MesasEdit = "mesas.edit";
        // Actas
        public const string ActasView = "actas.view";
        public const string ActasRegistrar = "actas.registrar";
        public const string ActasEditar = "actas.editar";
        public const string ActasValidar = "actas.validar";
        // Resultados
        public const string ResultadosView = "resultados.view";
        // Usuarios
        public const string UsuariosView = "usuarios.view";
        public const string UsuariosCreate = "usuarios.create";
        public const string UsuariosEdit = "usuarios.edit";

        // Roles
        public const string RolesView = "roles.view";
        public const string RolesCreate = "roles.create";
        public const string RolesEdit = "roles.edit";
        // Auditoría
        public const string BitacoraView = "bitacora.view";


        public static IReadOnlyCollection<string> ObtenerTodos()
        {
            return new[]
            {
                EleccionesView,
                EleccionesCreate,
                EleccionesEdit,

                ListasView,
                ListasCreate,
                ListasEdit,

                CandidatosView,
                CandidatosCreate,
                CandidatosEdit,

                MesasView,
                MesasCreate,
                MesasEdit,

                ActasView,
                ActasRegistrar,
                ActasEditar,
                ActasValidar,

                ResultadosView,

                UsuariosView,
                UsuariosCreate,
                UsuariosEdit,

                RolesView,
                RolesCreate,
                RolesEdit,

                BitacoraView
            };
        }

    }
}
