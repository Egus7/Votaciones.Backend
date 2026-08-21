using Microsoft.Extensions.DependencyInjection;
using Votaciones.Application.Services.Bitacora;
using Votaciones.Application.Services.Votaciones;
using Votaciones.Domain.Interfaces.IServices;

namespace Votaciones.Application.ServiceRegistration
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register application services here
            // Logica de negocio
            services.AddScoped<IBitacoraService, BitacoraService>();
            services.AddScoped<IEleccionService, EleccionService>();
            services.AddScoped<ICandidatoService, CandidatoService>();
            services.AddScoped<IMesaElectoralService, MesaElectoralService>();

            return services;
        }
    }
}
