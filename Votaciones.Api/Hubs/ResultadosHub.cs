using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Votaciones.Application.Security;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Api.Hubs
{
    [Authorize(Policy = RolPermisos.ResultadosView)]
    public class ResultadosHub : Hub
    {
        public async Task UnirseAResultados(Guid eleccionId, TipoCandidato tipoCandidato)
        {
            if (eleccionId == Guid.Empty) 
                throw new HubException("La elección no es válida.");      
            if (!Enum.IsDefined(typeof(TipoCandidato), tipoCandidato)) 
                throw new HubException("El tipo de candidato no es válido."); 
            
            var grupo = ObtenerNombreGrupo(eleccionId, tipoCandidato); 
            await Groups.AddToGroupAsync(Context.ConnectionId, grupo);
        }

        public async Task SalirDeResultados(Guid eleccionId, TipoCandidato tipoCandidato) { 
            if (eleccionId == Guid.Empty || !Enum.IsDefined(typeof(TipoCandidato), tipoCandidato)) { 
                throw new HubException("Los parámetros no son válidos."); 
            } 
            
            var grupo = ObtenerNombreGrupo(eleccionId, tipoCandidato); 
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, grupo); 
        }

        public static string ObtenerNombreGrupo(Guid eleccionId, TipoCandidato tipoCandidato) { 
            return $"resultados:{eleccionId:N}:{tipoCandidato}"; 
        }

    }
}
