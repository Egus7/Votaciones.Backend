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

        public enum Jurisdiccion
        {
            Nacional = 1,
            Provincial = 2,
            Cantonal = 3,
            Circunscripcion = 4,
        }

        public enum TipoCandidato
        {
            Presidente = 1,
            Prefecto = 2,
            Alcalde = 3,
            ConcejalUrbano = 4,
            ConcejalRural = 5,
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
        public enum TipoResultado
        {
            Oficial = 1,
            Preliminar = 2
        }

    }
}
