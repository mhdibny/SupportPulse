#region Usings

using AutoMapper;
using SupportPulse.Core.DTOs.Admin.Role;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.DTOs.User;
using SupportPulse.Data.Entities.User.Role;

#endregion

namespace SupportPulse.Core.Mapper.Admin
{
    /// <summary>
    /// AutoMapper profile for admin‑related mappings (roles, permissions, etc.).
    /// </summary>
    public class AdminMappingProfile : Profile
    {
        /// <summary>
        /// Initializes the mapping configuration.
        /// </summary>
        public AdminMappingProfile()
        {
            // AddRoleDto <-> Role (PermissionIdList is ignored in reverse mapping)
            CreateMap<AddRoleDto, Role>()
                .ReverseMap()
                .ForMember(dest => dest.PermissionIdList, opt => opt.Ignore());

            // Role -> RoleDto (flattening permissions)
            CreateMap<Role, RoleDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src =>
                    src.Permissions != null
                        ? src.Permissions.Select(rp => new PermissionDto
                        {
                            Id = rp.PermissionId,
                            Name = rp.Permission!.Name,
                            Category = rp.Permission.Category
                        }).ToList()
                        : new List<PermissionDto>()));

            // AlertViewModel <-> OperationResult<List<RoleListDto>>
            CreateMap<AlertViewModel, OperationResult<List<RoleListDto>>>().ReverseMap();
        }
    }
}