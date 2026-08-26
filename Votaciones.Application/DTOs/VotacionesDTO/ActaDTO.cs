using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.DTOs.VotacionesDTO
{
    public class ActaDTO
    {
        public Guid IdActa { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Eleccion { get; set; } = string.Empty;
        public string DescripcionEleccion { get; set; } = string.Empty;
        public string CodigoMesa {  get; set; } = string.Empty;
        public string DescripcionMesa {  get; set; } = string.Empty;
        public EstadoActa Estado { get; set; }
        public int VotosBlancos { get; set; }
        public int VotosNulos { get; set; }
        public int TotalVotos { get; set; }
        public List<ActaDetalleDTO> ActasDetalle { get; set; } = new List<ActaDetalleDTO>();

    }
}
