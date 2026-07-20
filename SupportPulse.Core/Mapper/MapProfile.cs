#region Usings

using AutoMapper;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.DTOs.User;
using SupportPulse.Core.ViewModels.User;
using SupportPulse.Data.Entities.User;

#endregion

namespace SupportPulse.Core.Mapper
{
    /// <summary>
    /// AutoMapper profile for general user‑related mappings.
    /// </summary>
    public class MapProfile : Profile
    {
        /// <summary>
        /// Initializes the mapping configuration.
        /// </summary>
        public MapProfile()
        {
            // User <-> SignUpUserVM
            CreateMap<User, SignUpUserVM>().ReverseMap();

            // AlertViewModel <-> OperationResult<UserForLoginDto>
            CreateMap<AlertViewModel, OperationResult<UserForLoginDto>>().ReverseMap();

            // OperationResult -> SystemAlertDto
            CreateMap<OperationResult, SystemAlertDto>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.MessageTitle))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.MessageType));
        }
    }
}