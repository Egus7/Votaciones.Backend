using AutoMapper;
using Votaciones.Application.DTOs.SeguridadDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Domain.Models;

namespace Votaciones.Application.DTOs.Mapping
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles() 
        {
            //MesaElectoral
            CreateMap<MesaElectoral, MesaElectoralDTO>()
                .ForMember(dest => dest.Eleccion, opt => opt.MapFrom(src => src.Eleccion!.NombreEleccion))
                .ForMember(dest => dest.DescripcionEleccion, opt => opt.MapFrom(src => src.Eleccion!.Descripcion))
                .ForMember(dest => dest.Zona, opt => opt.MapFrom(src => src.Zona!.NombreZona))
                .ForMember(dest => dest.Parroquia, opt => opt.MapFrom(src => src.Zona!.Parroquia!.NombreParroquia))
                .ForMember(dest => dest.Canton, opt => opt.MapFrom(src => src.Zona!.Parroquia!.Canton!.NombreCanton))
                .ForMember(dest => dest.Provincia, opt => opt.MapFrom(src => src.Zona!.Parroquia!.Canton!.Provincia!.NombreProvincia));

            //Candidato
            CreateMap<Candidato, CandidatoDTO>()
                .ForMember(dest => dest.Eleccion, opt => opt.MapFrom(src => src.Eleccion!.NombreEleccion))
                .ForMember(dest => dest.DescripcionEleccion, opt => opt.MapFrom(src => src.Eleccion!.Descripcion))
                .ForMember(dest => dest.Parroquia, opt => opt.MapFrom(src => src.Parroquia!.NombreParroquia))
                .ForMember(dest => dest.Canton, opt => opt.MapFrom(src => src.Canton!.NombreCanton))
                .ForMember(dest => dest.Provincia, opt => opt.MapFrom(src => src.Provincia!.NombreProvincia))
                // Detalle
                .ForMember(dest => dest.ListasCandidato, opt => opt.MapFrom(src => src.ListaCandidatos
                    .OrderByDescending(x => x.ListaPrincipal).ThenBy(x => x.ListaElectoral!.NumeroLista)));

            // ListaCandidato
            CreateMap<ListaCandidato, ListaCandidatoDTO>()
                .ForMember(dest => dest.NumeroLista, opt => opt.MapFrom(src => src.ListaElectoral!.NumeroLista))
                .ForMember(dest => dest.NombreLista, opt => opt.MapFrom(src => src.ListaElectoral!.NombreLista))
                .ForMember(dest => dest.EsPrincipal, opt => opt.MapFrom(src => src.ListaPrincipal));

            //ActaEleccion
            CreateMap<ActaEleccion, ActaDTO>()
                .ForMember(dest => dest.Eleccion, opt => opt.MapFrom(src => src.Eleccion!.NombreEleccion))
                .ForMember(dest => dest.DescripcionEleccion, opt => opt.MapFrom(src => src.Eleccion!.Descripcion))
                .ForMember(dest => dest.CodigoMesa, opt => opt.MapFrom(src => src.MesaElectoral!.CodigoMesa))
                .ForMember(dest => dest.DescripcionMesa, opt => opt.MapFrom(src => src.MesaElectoral!.Descripcion))
                .ForMember(dest => dest.Zona, opt => opt.MapFrom(src => src.MesaElectoral!.Zona!.NombreZona))
                .ForMember(dest => dest.Parroquia, opt => opt.MapFrom(src => src.MesaElectoral!.Zona!.Parroquia!.NombreParroquia))
                .ForMember(dest => dest.Canton, opt => opt.MapFrom(src => src.MesaElectoral!.Zona!.Parroquia!.Canton!.NombreCanton))
                .ForMember(dest => dest.Provincia, opt => opt.MapFrom(src => src.MesaElectoral!.Zona!.Parroquia!.Canton!.Provincia!.NombreProvincia))
                // Detalle
                .ForMember(dest => dest.ActasDetalle, opt => opt.MapFrom(src => src.ActaDetalles)); // Detalles

            //Acta Detalle
            CreateMap<ActaDetalle, ActaDetalleDTO>()
                .ForMember(dest => dest.NombreCandidato, opt => opt.MapFrom(src => src.Candidato!.NombreCandidato))
                .ForMember(dest => dest.NombreLista, opt => opt.MapFrom(src => src.ListaElectoral!.NombreLista))
                .ForMember(dest => dest.NumeroLista, opt => opt.MapFrom(src => src.ListaElectoral!.NumeroLista));

            //Usuario
            CreateMap<AdmUsuario, UsuarioDTO>()
                .ForMember(dest => dest.NombreRol, opt => opt.MapFrom(src => src.Rol!.NombreRol));

            //Rol
            CreateMap<AdmRol, RolDTO>()
                .ForMember(dest => dest.Permisos, opt => opt.Ignore());

            //Bitacora
            CreateMap<AdmBitacora, BitacoraDTO>()
                .ForMember(dest => dest.Usuario, opt => opt.MapFrom(src => src.Usuario!.NombreUsuario))
                .ForMember(dest => dest.CorreoUsuario, opt => opt.MapFrom(src => src.Usuario!.EmailUsuario))
                .ForMember(dest => dest.ValoresAnteriores, opt => opt.MapFrom(src => src.ValoresAnteriores))
                .ForMember(dest => dest.ValoresNuevos, opt => opt.MapFrom(src => src.ValoresNuevos));

        }
    }
}
