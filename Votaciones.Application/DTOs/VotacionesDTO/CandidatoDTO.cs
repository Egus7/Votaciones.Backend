using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.DTOs.VotacionesDTO
{
    public class CandidatoDTO
    {
        public Guid IdCandidato { get; set; }
        public string Eleccion { get; set; } = string.Empty;
        public string DescripcionEleccion { get; set; } = string.Empty;
        public string NombreCandidato { get; set; } = string.Empty;
        public TipoCandidato TipoCandidato { get; set; }
        public string Lista { get; set; } = string.Empty;
        public int NumeroLista { get; set; } 
        public int? Orden { get; set; }
        public bool Activo { get; set; }
    }
}
