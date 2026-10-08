
namespace Votaciones.Application.DTOs.SeguridadDTO
{
    public class BitacoraDTO
    {
        public Guid IdBitacora { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string CorreoUsuario { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string Tabla { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string? ValoresAnteriores { get; set; }
        public string? ValoresNuevos { get; set; }
    }
}
