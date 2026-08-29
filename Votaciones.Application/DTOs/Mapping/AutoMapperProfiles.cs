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
                .ForMember(dest => dest.DescripcionEleccion, opt => opt.MapFrom(src => src.Eleccion!.Descripcion));

            //ActaEleccion
            CreateMap<ActaEleccion, ActaDTO>()
                .ForMember(dest => dest.Eleccion, opt => opt.MapFrom(src => src.Eleccion!.NombreEleccion))
                .ForMember(dest => dest.DescripcionEleccion, opt => opt.MapFrom(src => src.Eleccion!.Descripcion))
                .ForMember(dest => dest.CodigoMesa, opt => opt.MapFrom(src => src.MesaElectoral!.CodigoMesa))
                .ForMember(dest => dest.DescripcionMesa, opt => opt.MapFrom(src => src.MesaElectoral!.Descripcion))
                // Detalle
                .ForMember(dest => dest.ActasDetalle, opt => opt.MapFrom(src => src.ActaDetalles)); // Detalles
            
            //Acta Detalle
            CreateMap<ActaDetalle, ActaDetalleDTO>()
                .ForMember(dest => dest.Candidato, opt => opt.MapFrom(src => src.Candidato!.NombreCandidato))
                .ForMember(dest => dest.Lista, opt => opt.MapFrom(src => src.Candidato!.Lista))
                .ForMember(dest => dest.NumeroLista, opt => opt.MapFrom(src => src.Candidato!.NumeroLista));

            //Usuario
            CreateMap<AdmUsuario, UsuarioDTO>()
                .ForMember(dest => dest.NombreRol, opt => opt.MapFrom(src => src.Rol!.NombreRol));

            //Rol
            CreateMap<AdmRol, RolDTO>()
                .ForMember(dest => dest.Permisos, opt => opt.Ignore());

        }
    }
}
