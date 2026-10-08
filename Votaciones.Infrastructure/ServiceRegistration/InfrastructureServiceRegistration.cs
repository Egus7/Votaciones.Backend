using Microsoft.Extensions.DependencyInjection;
using Votaciones.Application.Interfaces.ISecurity;
using Votaciones.Domain.Interfaces;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Infrastructure.Persistence;
using Votaciones.Infrastructure.Persistence.Repositories;
using Votaciones.Infrastructure.Security;

namespace Votaciones.Infrastructure.ServiceRegistration
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // Se registra los servicios de infraestructura, como repositorios, servicios, etc.
            // Repositories (Persistencia)
            services.AddScoped<IBitacoraRepository, BitacoraRepository>();
            services.AddScoped<IEleccionRepository, EleccionRepository>();
            services.AddScoped<IZonaRepository, ZonaRepository>();
            services.AddScoped<IListaElectoralRepository, ListaElectoralRepository>();
            services.AddScoped<ICandidatoRepository, CandidatoRepository>();
            services.AddScoped<IMesaElectoralRepository, MesaElectoralRepository>();
            services.AddScoped<IActaRepository, ActaRepository>();
            services.AddScoped<IResultadoRepository, ResultadoRepository>();
            //Seguridad
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IRolRepository, RolRepository>();          
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IJwtService, JwtService>();
            //Service (unidad de trabajo)
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
