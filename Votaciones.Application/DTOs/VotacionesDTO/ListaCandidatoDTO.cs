using System;
using System.Collections.Generic;
using System.Text;

namespace Votaciones.Application.DTOs.VotacionesDTO
{
    public class ListaCandidatoDTO
    {
        public Guid ListaElectoralId { get; set; }
        public int NumeroLista { get; set; }
        public string NombreLista { get; set; } = string.Empty;
        public bool EsPrincipal { get; set; }
    }
}
