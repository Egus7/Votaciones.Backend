using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.SeguridadDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Application.Utils;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Models;

namespace Votaciones.Application.Services.Bitacora
{
    public class BitacoraService : IBitacoraService
    {
        private readonly IBitacoraRepository _bitacoraRepository;
        private readonly IMapper _mapper;

        public BitacoraService(IBitacoraRepository bitacoraRepository, IMapper mapper)
        {
            _bitacoraRepository = bitacoraRepository;
            _mapper = mapper;
        }

        public async Task<PaginacionDTO<BitacoraDTO>> ObtenerPaginacionAsync(int pagina, int pageSize,
            DateTime? fechaDesde = null, DateTime? fechaHasta = null, Guid? usuarioId = null, 
            string? accion = null, string? tabla = null)
        {
            var query = _bitacoraRepository.ObtenerQuery();

            if (fechaDesde.HasValue)
            {
                query = query.Where(x => x.FechaRegistro >= fechaDesde.Value.Date);
            }
            if (fechaHasta.HasValue)
            {
                var fechaFin = fechaHasta.Value.Date.AddDays(1);
                query = query.Where(x => x.FechaRegistro < fechaFin);
            }
            if (usuarioId.HasValue)
            {
                query = query.Where(x =>x.UsuarioId == usuarioId.Value);
            }
            if (!string.IsNullOrWhiteSpace(accion))
            {
                query = query.Where(x => x.Accion == accion);
            }
            if (!string.IsNullOrWhiteSpace(tabla))
            {
                query = query.Where(x =>x.Tabla == tabla);
            }

            var totalRegistros = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.FechaRegistro)
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<BitacoraDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return new PaginacionDTO<BitacoraDTO>
            {
                Items = items,
                PageActual = pagina,
                PageSize = pageSize,
                TotalRegistros = totalRegistros,
                TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize)
            };
        }

        public async Task<BitacoraDTO?> ObtenerPorIdAsync(Guid bitacoraId)
        {
            var query = _bitacoraRepository.ObtenerQuery();

            return await query.Where(x => x.IdBitacora == bitacoraId)
                .ProjectTo<BitacoraDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

        }

        public async Task<List<BitacoraDTO>> ObtenerPorRegistroAsync(string tabla, string idRegistro)
        {
            var bitacoras = await _bitacoraRepository.ObtenerPorRegistroAsync(tabla, idRegistro);
            
            return bitacoras.Select(b => _mapper.Map<BitacoraDTO>(b)).ToList();
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
