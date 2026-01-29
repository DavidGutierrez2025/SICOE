using AutoMapper;
using SICOE.Application.DTOs.Descarga;
using SICOE.Domain.Entities;

namespace SICOE.Application.Mappings;

/// <summary>
/// Profile de AutoMapper para mapeos relacionados con Descarga
/// </summary>
public class DescargaMappingProfile : Profile
{
    public DescargaMappingProfile()
    {
        // SolicitudDescarga
        CreateMap<SolicitudDescarga, SolicitudDescargaDto>()
            .ForMember(dest => dest.ClienteRfc, opt => opt.MapFrom(src => src.Cliente.Rfc.Valor))
            .ForMember(dest => dest.ClienteRazonSocial, opt => opt.MapFrom(src => src.Cliente.RazonSocial));

        CreateMap<SolicitudDescarga, EstadoDescargaDto>()
            .ForMember(dest => dest.Faltantes, opt => opt.MapFrom(src => src.ObtenerFaltantes()));

        // CFDI
        CreateMap<CFDI, CFDIDto>()
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Uuid.Valor))
            .ForMember(dest => dest.RfcEmisor, opt => opt.MapFrom(src => src.RfcEmisor.Valor))
            .ForMember(dest => dest.RfcReceptor, opt => opt.MapFrom(src => src.RfcReceptor.Valor))
            .ForMember(dest => dest.Total, opt => opt.MapFrom(src => src.Total.Valor))
            .ForMember(dest => dest.Moneda, opt => opt.MapFrom(src => src.Total.Moneda));
    }
}

