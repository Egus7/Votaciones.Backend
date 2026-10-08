using Microsoft.Extensions.DependencyInjection;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Application.Services.Bitacora;
using Votaciones.Application.Services.Seguridad;
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
            services.AddScoped<IZonaService, ZonaService>();
            services.AddScoped<IListaElectoralService, ListaElectoralService>();
            services.AddScoped<ICandidatoService, CandidatoService>();
            services.AddScoped<IMesaElectoralService, MesaElectoralService>();
            services.AddScoped<IActaService, ActaService>();
            services.AddScoped<IResultadoService, ResultadoService>();
            //Seguridad
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IRolService, RolService>();
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
