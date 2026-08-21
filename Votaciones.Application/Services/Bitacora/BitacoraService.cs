using System.Text.Json;
using Votaciones.Application.Utils;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Interfaces.IServices;
using Votaciones.Domain.Models;

namespace Votaciones.Application.Services.Bitacora
{
    public class BitacoraService : IBitacoraService
    {
        private readonly IBitacoraRepository _bitacoraRepository;

        public BitacoraService(IBitacoraRepository bitacoraRepository)
        {
            _bitacoraRepository = bitacoraRepository;
        }

        public async Task<List<AdmBitacora>> ObtenerPorEleccionAsync(Guid eleccionId)
        {
            return await _bitacoraRepository.ObtenerPorEleccionAsync(eleccionId);
        }

        public async Task<List<AdmBitacora>> ObtenerPorRegistroAsync(string tabla, string idRegistro)
        {
            return await _bitacoraRepository.ObtenerPorRegistroAsync(tabla, idRegistro);
        }

        public async Task RegistrarBitacoraAsync(string accion, string tabla, string idRegistro, string descripcion, 
            Guid? eleccionId = null, Guid? usuarioId = null, object? valoresAnteriores = null, object? valoresNuevos = null)
        {
            var bitacora = new AdmBitacora
            {
                IdBitacora = Guid.NewGuid(),
                EleccionId = eleccionId,
                UsuarioId = usuarioId,
                FechaRegistro = Fecha.DevolverDatetime(DateTime.Now.ToString("o")),
                Accion = accion,
                Tabla = tabla,
                IdRegistro = idRegistro,
                Descripcion = descripcion,
                ValoresAnteriores = valoresAnteriores == null ? null : JsonSerializer.Serialize(valoresAnteriores),
                ValoresNuevos = valoresNuevos == null ? null : JsonSerializer.Serialize(valoresNuevos)
            };

            await _bitacoraRepository.AgregarAsync(bitacora);
        }

    }
}
