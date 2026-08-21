using Microsoft.Extensions.DependencyInjection;
using Votaciones.Domain.Interfaces;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Infrastructure.Persistence;
using Votaciones.Infrastructure.Persistence.Repositories;

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
            services.AddScoped<ICandidatoRepository, CandidatoRepository>();
            services.AddScoped<IMesaElectoralRepository, MesaElectoralRepository>();

            //Service (unidad de trabajo)
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
