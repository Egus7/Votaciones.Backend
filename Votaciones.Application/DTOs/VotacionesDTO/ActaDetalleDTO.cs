namespace Votaciones.Application.DTOs.VotacionesDTO
{
    public class ActaDetalleDTO
    {
        public Guid? CandidatoId { get; set; }
        public string? NombreCandidato { get; set; }
        public Guid ListaElectoralId { get; set; }
        public string NombreLista {  get; set; } = string.Empty;
        public int NumeroLista { get; set; } 
        public int Votos { get; set; }
    }
}
