
namespace Votaciones.Application.DTOs.VotacionesDTO
{
    public class ResultadoCandidatoDTO
    {
        public Guid CandidatoId { get; set; }
        public string Candidato { get; set; } = string.Empty;
        public string Lista { get; set; } = string.Empty;
        public int NumeroLista { get; set; }
        public int Votos { get; set; }
        public decimal Porcentaje { get; set; }
    }
}
