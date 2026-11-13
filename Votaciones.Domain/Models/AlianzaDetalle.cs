using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Votaciones.Domain.Models
{
    [Table("AlianzaDetalle")]
    public class AlianzaDetalle
    {
        [Key]
        public Guid IdAlianzaDetalle { get; set; }
        public Guid AlianzaElectoralId { get; set; }
        [ForeignKey(nameof(AlianzaElectoralId))]
        [JsonIgnore]
        public AlianzaElectoral? AlianzaElectoral { get; set; }
        public Guid ListaElectoralId { get; set; }
        [ForeignKey(nameof(ListaElectoralId))]
        [JsonIgnore]
        public ListaElectoral? ListaElectoral { get; set; }
        public bool ListaAlianzaPrincipal { get; set; }
    }
}
