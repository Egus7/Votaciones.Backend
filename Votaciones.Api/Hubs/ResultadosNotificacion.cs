using Microsoft.AspNetCore.SignalR;
using Votaciones.Application.Interfaces.ISignal;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Api.Hubs
{
    public class ResultadosNotificacion : IResultadosNotificacion
    {
        private readonly IHubContext<ResultadosHub> _hubContext;
        private readonly ILogger<ResultadosNotificacion> _logger;
        public ResultadosNotificacion(IHubContext<ResultadosHub> hubContext, ILogger<ResultadosNotificacion> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }
        public async Task NotificarResultadosActualizadosAsync(Guid eleccionId, TipoCandidato tipoCandidato, Guid mesaId, Guid zonaId, 
                Guid parroquiaId, Guid cantonId, Guid provinciaId)
        {
            if (eleccionId == Guid.Empty || mesaId == Guid.Empty || zonaId == Guid.Empty || parroquiaId == Guid.Empty || 
                cantonId == Guid.Empty || provinciaId == Guid.Empty || !Enum.IsDefined(typeof(TipoCandidato), tipoCandidato)) {
                
                _logger.LogWarning("No se notificaron resultados: los parámetros no son válidos. " +
                    "EleccionId: {EleccionId}, TipoCandidato: {TipoCandidato}, ZonaId: {ZonaId}",
                    eleccionId, tipoCandidato, zonaId);

                return;
            }

            try 
            {
                var grupo = ResultadosHub.ObtenerNombreGrupo(eleccionId, tipoCandidato);

                await _hubContext.Clients.Group(grupo).SendAsync("ResultadosActualizados", eleccionId, tipoCandidato, mesaId, zonaId, 
                        parroquiaId, cantonId, provinciaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"No se pudo notificar la actualización de resultados. " + 
                    "EleccionId: {EleccionId}, TipoCandidato: {TipoCandidato}",
                    eleccionId, tipoCandidato);
            }
        }

    }
}
