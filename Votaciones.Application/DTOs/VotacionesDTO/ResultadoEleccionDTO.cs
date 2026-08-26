
namespace Votaciones.Application.DTOs.VotacionesDTO
{
    public class ResultadoEleccionDTO
    {
        public int MesasRegistradas { get; set; }
        public int MesasEnRevision { get; set; }
        public int MesasConInconsistencia { get; set; }
        public int MesasValidadas { get; set; }
        public int TotalMesas { get; set; }

        public int TotalVotosBlancos { get; set; }
        public int TotalVotosNulos { get; set; }
        public int TotalVotos { get; set; }
        public int TotalVotosValidos { get; set; }

        public List<ResultadoCandidatoDTO> Resultados { get; set; } = new();
    }
}
