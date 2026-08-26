using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.DTOs.VotacionesDTO
{
    public class MesaElectoralDTO
    {
        public Guid IdMesaElectoral { get; set; }
        public string Eleccion { get; set; } = string.Empty;
        public string DescripcionEleccion {  get; set; } = string.Empty;
        public string CodigoMesa { get; set; } = string.Empty;
        public TipoMesa TipoMesa { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Zona { get; set; } = string.Empty;
        public string Parroquia { get; set; } = string.Empty;
        public string Canton { get; set; } = string.Empty;
        public string Provincia { get; set; } = string.Empty;
        public bool Activa { get; set; }
    }
}
