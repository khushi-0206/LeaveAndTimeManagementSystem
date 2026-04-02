using AuthService.Application.DTOs;
using AuthService.Domain.Entities;
using AutoMapper;


namespace AuthService.Infrastructure.Mappings
{
    public class AuthMappingProfile : Profile
    {
        public AuthMappingProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<RegisterUserDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
        }
    }
}