using Votaciones.Domain.Models;

namespace Votaciones.Application.Helpers.Audit
{
    public class CamposAuditablesBitacora
    {
        public static class CamposAuditablesEleccion
        {
            public static readonly string[] Campos =
            {
                nameof(Eleccion.NombreEleccion),
                nameof(Eleccion.Descripcion),
                nameof(Eleccion.FechaEleccion),
                nameof(Eleccion.Estado),
                nameof(Eleccion.FechaCreacion),
            };
        }

        public static class CamposAuditablesCandidato
        {
            public static readonly string[] Campos =
            {
                nameof(Candidato.NombreCandidato),
                nameof(Candidato.NumeroLista),
                nameof(Candidato.Lista),
                nameof(Candidato.Activo),
            };
        }

        public static class CamposAuditablesMesaElectoral
        {
            public static readonly string[] Campos =
            {
                nameof(MesaElectoral.CodigoMesa),
                nameof(MesaElectoral.TipoMesa),
                nameof(MesaElectoral.Descripcion),
                nameof(MesaElectoral.Activa),
            };
        }

        public static class CamposAuditablesActa
        {
            public static readonly string[] Campos =
            {
                nameof(ActaEleccion.FechaRegistro),
                nameof(ActaEleccion.VotosBlancos),
                nameof(ActaEleccion.VotosNulos),
                nameof(ActaEleccion.TotalVotos),
                nameof(ActaEleccion.Estado)
            };
        }

        //Seguridad
        public static class CamposAuditablesUsuario
        {
            public static readonly string[] Campos =
            {
                nameof(AdmUsuario.NombreUsuario),
                nameof(AdmUsuario.EmailUsuario),
                nameof(AdmUsuario.Estado),
                nameof(AdmUsuario.FechaCreacion)
            };
        }

        public static class CamposAuditablesRol
        {
            public static readonly string[] Campos =
            {
                nameof(AdmRol.NombreRol),
                nameof(AdmRol.DescripcionRol),
                nameof(AdmRol.PermisosRol),
                nameof(AdmRol.Activo)
            };
        }
    }
}
