using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.Interfaces.ISignal
{
    public interface IResultadosNotificacion
    {
        Task NotificarResultadosActualizadosAsync(Guid eleccionId, TipoCandidato tipoCandidato, Guid mesaId, Guid zonaId, Guid parroquiaId,
            Guid cantonId, Guid provinciaId);
    }
}
