using AutoMapper;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;

namespace HubManagement.Application.Socities.MappingProfile
{
    public class SocietyProfile : Profile
    {
        public SocietyProfile()
        {
            CreateMap<Society, SocietyDto>()
                .ForMember(dest => dest.Name,
                            e => e.MapFrom(src => src.Name))
                .ForMember(dest => dest.ContactEmail,
                            e => e.MapFrom(src => src.ContactEmail))
                 .ForMember(dest => dest.Phone,
                            e => e.MapFrom(src => src.Phone))
                 .ForMember(dest => dest.Street,
                            e => e.MapFrom(src => src.Address.Street))
                 .ForMember(dest => dest.City,
                            e => e.MapFrom(src => src.Address.City))
                 .ForMember(dest => dest.State,
                            e => e.MapFrom(src => src.Address.State))
                 .ForMember(dest => dest.ZipCode,
                            e => e.MapFrom(src => src.Address.ZipCode))
                 .ForMember(dest => dest.Country,
                            e => e.MapFrom(src => src.Address.Country))
                ;
        }
    }
}
