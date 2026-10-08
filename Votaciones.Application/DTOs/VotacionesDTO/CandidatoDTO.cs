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
        public int? Orden { get; set; }
        public string? Provincia { get; set; }
        public string? Canton { get; set; }
        public string? Parroquia { get; set; }
        public bool Activo { get; set; }
        public List<ListaCandidatoDTO> ListasCandidato { get; set; } = new List<ListaCandidatoDTO>();
    }
}
