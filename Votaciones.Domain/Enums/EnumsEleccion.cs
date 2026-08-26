namespace Votaciones.Domain.Enums
{
    public class EnumsEleccion
    {
        public enum EstadoEleccion
        {
            Planificada = 1,
            Activa = 2,
            Cerrada = 3
        }

        public enum TipoMesa
        {
            Femenina = 1,
            Masculina = 2
        }
        public enum EstadoActa
        {
            Registrada = 1,
            EnRevision = 2,
            ConInconsistencia = 3,
            Validada = 4,
            Anulada = 5      
        }

    }
}
