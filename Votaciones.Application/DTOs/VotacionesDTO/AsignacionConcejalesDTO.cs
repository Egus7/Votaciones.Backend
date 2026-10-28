
namespace Votaciones.Application.DTOs.VotacionesDTO
{
    public class AsignacionConcejalesDTO
    {
        public Guid EleccionId { get; set; }
        public string NombreEleccion { get; set; } = string.Empty;
        public string Dignidad { get; set; } = string.Empty;
        public string EstadoResultado { get; set; } = string.Empty;
        public int TotalEscanos { get; set; }
        public int TotalListas { get; set; }
        public List<AsignacionListaDTO> Listas { get; set; } = [];
    }

    public class AsignacionListaDTO
    {
        public Guid ListaElectoralId { get; set; }
        public int NumeroLista { get; set; }
        public string? NumerosLista { get; set; } = string.Empty;
        public string NombreLista { get; set; } = string.Empty;
        public int Votos { get; set; }
        public int EscanosAsignados { get; set; }
        public List<AsignacionCandidatoDTO> Candidatos { get; set; } = [];
    }

    public class AsignacionCandidatoDTO
    {
        public Guid CandidatoId { get; set; }
        public string NombreCandidato { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Electo { get; set; }
    }

}
