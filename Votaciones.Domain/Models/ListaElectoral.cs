using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Domain.Models
{
    [Table("ListaElectoral")]
    public class ListaElectoral
    {
        [Key]
        public Guid IdListaElectoral { get; set; }
        [Required]
        public int NumeroLista { get; set; }
        [Required]
        public string NombreLista { get; set; } = string.Empty;
        public string? Siglas { get; set; }
        public Jurisdiccion Jurisdiccion { get; set; }
        public bool Activo { get; set; }
    }
}
