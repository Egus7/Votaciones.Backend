using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Votaciones.Domain.Models
{
    [Table("ListaCandidato")]
    public class ListaCandidato
    {
        [Key]
        public Guid IdListaCandidato { get; set; }

        public Guid CandidatoId { get; set; }

        [ForeignKey(nameof(CandidatoId))]
        [JsonIgnore]
        public Candidato? Candidato { get; set; }

        public Guid ListaElectoralId { get; set; }

        [ForeignKey(nameof(ListaElectoralId))]
        [JsonIgnore]
        public ListaElectoral? ListaElectoral { get; set; }

        // Identifica la lista principal de la candidatura
        public bool ListaPrincipal { get; set; }
    }
}
