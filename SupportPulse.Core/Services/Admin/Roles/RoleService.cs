#region Usings

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SupportPulse.Core.DTOs.Admin.EventDispatcher;
using SupportPulse.Core.DTOs.Admin.Role;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.Services.Admin.EventDispatcher;
using SupportPulse.Core.Services.Admin.PermissionCache;
using SupportPulse.Core.Services.Admin.Session;
using SupportPulse.Core.Services.Common;
using SupportPulse.Data.Context;
using SupportPulse.Data.Entities.User.Role;
using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.Core.Services.Admin.Roles
{
    /// <summary>
    /// Manages role CRUD operations, permission cache synchronization, and event dispatching.
    /// </summary>
    public class RoleService : IRoleService
    {
        #region Constructor & Dependencies

        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly IOperationResultAction _operation;
        private readonly ICurrentAdminSession _adminSession;
        private readonly IAdminEventDispatcher _eventDispatcher;
        private readonly IAdminPermissionCacheService _adminPermissionCache;

        public RoleService(
            ApplicationDbContext db,
            IMapper mapper,
            IOperationResultAction operation,
            ICurrentAdminSession adminSession,
            IAdminEventDispatcher eventDispatcher,
            IAdminPermissionCacheService adminPermissionCache)
        {
            _db = db;
            _mapper = mapper;
            _operation = operation;
            _adminSession = adminSession;
            _eventDispatcher = eventDispatcher;
            _adminPermissionCache = adminPermissionCache;
        }

        #endregion

        #region Permissions

        /// <inheritdoc />
        public async Task<List<PermissionDto>> GetPermissionsAsync()
        {
            return await _db.Permissions
                .AsNoTracking()
                .Select(s => new PermissionDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Category = s.Category
                })
                .ToListAsync();
        }

        #endregion

        #region Role Queries

        /// <inheritdoc />
        public async Task<OperationResult<List<RoleListDto>>> GetRoleListAsync()
        {
            try
            {
                var roles = await _db.Roles
                    .AsNoTracking()
                    .Select(s => new RoleListDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        PermissionsCount = s.Permissions!.Count,
                        UserHaveThisRoleCount = s.Users!.Count
                    })
                    .ToListAsync();

                return _operation.SendSuccess(entity: roles);
            }
            catch
            {
                return _operation.SendError<List<RoleListDto>>("هنگام دریافت نقش ها مشکلی پیش امد.");
            }
        }

        /// <inheritdoc />
        public async Task<OperationResult<RoleDto>> GetRoleForEditAsync(int roleId)
        {
            Role? roleInDb = await _db.Roles
                .AsNoTracking()
                .AsSplitQuery()
                .Include(i => i.Permissions)!
                .ThenInclude(rp => rp.Permission)
                .SingleOrDefaultAsync(r => r.Id == roleId);

            if (roleInDb is null)
            {
                return _operation.SendError<RoleDto>("نقش مورد نظر یافت نشد.");
            }

            RoleDto roleDto = _mapper.Map<RoleDto>(roleInDb);
            return _operation.SendSuccess(entity: roleDto);
        }

        /// <inheritdoc />
        public async Task<OperationResult<DeleteRoleDto>> GetRoleForDelete(int roleId)
        {
            var roleInDb = await _db.Roles
                .AsNoTracking()
                .AsSplitQuery()
                .Select(s => new DeleteRoleDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Permissions = s.Permissions!.Select(p => new PermissionDto
                    {
                        Id = p.PermissionId,
                        Name = p.Permission!.Name,
                        Category = p.Permission.Category
                    }).ToList(),
                    UserHaveThisRoleCount = s.Users!.Count
                })
                .SingleOrDefaultAsync(r => r.Id == roleId);

            if (roleInDb is null)
            {
                return _operation.SendError<DeleteRoleDto>("نقش مورد نظر یافت نشد.");
            }

            return _operation.SendSuccess(entity: roleInDb);
        }

        /// <inheritdoc />
        public async Task<OperationResult<List<RoleListDto>>> SearchInRolesAsync(SearchRoleDto? search)
        {
            bool hasName = !string.IsNullOrWhiteSpace(search?.RoleName);
            bool hasPermissions = search?.PermissionsIdList is not null && search.PermissionsIdList.Count > 0;

            if (!hasName && !hasPermissions)
            {
                return _operation.SendError<List<RoleListDto>>(
                    "لطفاً برای جستجو، نام نقش یا حداقل یک مجوز را وارد کنید.",
                    OperationStatus.Info);
            }

            IQueryable<Role> query = _db.Roles.AsNoTracking();

            if (hasName)
                query = query.Where(r => r.Name.Contains(search!.RoleName!));

            if (hasPermissions)
            {
                query = query.Where(r =>
                    r.Permissions!.Any(rp => search!.PermissionsIdList!.Contains(rp.PermissionId)));
            }

            List<RoleListDto> roleList = await query
                .Select(s => new RoleListDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    PermissionsCount = s.Permissions!.Count,
                    UserHaveThisRoleCount = s.Users!.Count
                })
                .ToListAsync();

            return _operation.SendSuccess(entity: roleList);
        }

        /// <inheritdoc />
        public async Task<OperationResult<List<RoleDto>>> GetRolesListAsync()
        {
            var rolesList = await _db.Roles
                .AsNoTracking()
                .Select(s => new RoleDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Permissions = s.Permissions!.Select(p => new PermissionDto
                    {
                        Id = p.PermissionId,
                        Name = p.Permission!.Name,
                        Category = p.Permission.Category
                    }).ToList()
                })
                .ToListAsync();

            if (rolesList.Any())
            {
                return _operation.SendSuccess(entity: rolesList);
            }

            return _operation.SendError<List<RoleDto>>(
                "ما هیچ نقشی نداریم، لطفا ابتدا نقش ایجاد کنید سپس به کاربر تخصیص دهید.");
        }

        #endregion

        #region Add Role

        /// <inheritdoc />
        public async Task<OperationResult> AddRoleAsync(AddRoleDto role)
        {
            if (!await IsRoleNameUniqueAsync(role.Name))
                return _operation.SendError("نام نقش تکراری است.");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                Role newRole = _mapper.Map<Role>(role);
                await _db.Roles.AddAsync(newRole);
                await _db.SaveChangesAsync();

                foreach (var permissionId in role.PermissionIdList)
                {
                    await _db.RolePermissions.AddAsync(new RolePermission
                    {
                        RoleId = newRole.Id,
                        PermissionId = permissionId
                    });
                }
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // Dispatch role creation event
                var context = new AdminEventContext
                {
                    EventType = AdminEventType.RoleCreated,
                    ActorAdminId = _adminSession.GetAdminId(),
                    ActorFullName = _adminSession.GetAdminFullName(),
                    ActorUserName = _adminSession.GetAdminUserName(),
                    RoleName = role.Name,
                    RoleId = newRole.Id,
                    DataSyncPayload = new RoleListDto
                    {
                        Id = newRole.Id,
                        Name = newRole.Name,
                        PermissionsCount = role.PermissionIdList.Count,
                        UserHaveThisRoleCount = 0
                    }
                };
                await _eventDispatcher.DispatchAsync(context);
            }
            catch
            {
                await transaction.RollbackAsync();
                return _operation.SendError("هنگام افزودن نقش خطایی رخ داد، لطفا مجدد تلاش کنید");
            }

            return _operation.SendSuccess();
        }

        #endregion

        #region Edit Role

        /// <inheritdoc />
        public async Task<OperationResult> EditRoleAsync(EditRoleDto role)
        {
            if (!await IsRoleNameUniqueAsync(role.Name, role.Id))
                return _operation.SendError("نام نقش تکراری است.");

            Role? roleInDb = await GetRoleByIdAsync(role.Id);
            if (roleInDb is null)
                return _operation.SendError<RoleDto>("نقش مورد نظر یافت نشد.");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Retrieve existing permissions before removal
                var permissionsInDb = await _db.RolePermissions
                    .Where(rp => rp.RoleId == roleInDb.Id)
                    .ToListAsync();

                if (permissionsInDb.Count > 0)
                    _db.RolePermissions.RemoveRange(permissionsInDb);

                // Assign new permissions
                if (role.PermissionIdList?.Count > 0)
                {
                    if (!await IsPermissionsValidAsync(role.PermissionIdList))
                        return _operation.SendError("لطفا از مجوز معتبر استفاده کنید.");

                    foreach (var permissionId in role.PermissionIdList)
                    {
                        await _db.RolePermissions.AddAsync(new RolePermission
                        {
                            PermissionId = permissionId,
                            RoleId = roleInDb.Id
                        });
                    }
                }

                roleInDb.Name = role.Name;
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // Update permission cache and dispatch event
                await SyncRolePermissionsCacheAndDispatch(permissionsInDb, roleInDb.Id, role);
            }
            catch
            {
                await transaction.RollbackAsync();
                return _operation.SendError("هنگام ویرایش نقش، خطایی رخ داد، لطفا بعدا مجدد تلاش کنید");
            }

            return _operation.SendSuccess("ویرایش نقش با موفقیت انجام شد.");
        }

        #endregion

        #region Delete Role

        /// <inheritdoc />
        public async Task<OperationResult> DeleteRoleAsync(int roleId)
        {
            var role = await _db.Roles.FindAsync(roleId);
            if (role == null)
                return _operation.SendError("نقش مورد نظر یافت نشد.");

            try
            {
                var userRoles = await _db.UserRoles
                    .Where(ur => ur.RoleId == roleId)
                    .ToListAsync();

                var rolePermissions = await _db.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync();

                // Cache update and dispatch before removing role-permission links
                var thisRolePermissions = rolePermissions.Select(rp => rp.PermissionId).ToList();
                var userIds = userRoles.Select(ur => ur.UserId).ToList();

                foreach (var userId in userIds)
                {
                    _adminPermissionCache.RemovePermissionsFromUser(userId, thisRolePermissions);
                }

                if (userRoles.Any())
                    _db.UserRoles.RemoveRange(userRoles);

                if (rolePermissions.Any())
                    _db.RolePermissions.RemoveRange(rolePermissions);

                _db.Roles.Remove(role);
                await _db.SaveChangesAsync();

                // Dispatch role deletion event
                var context = new AdminEventContext
                {
                    EventType = AdminEventType.RoleDeleted,
                    ActorAdminId = _adminSession.GetAdminId(),
                    ActorFullName = _adminSession.GetAdminFullName(),
                    ActorUserName = _adminSession.GetAdminUserName(),
                    RoleName = role.Name,
                    RoleId = roleId,
                    DataSyncPayload = roleId
                };
                await _eventDispatcher.DispatchAsync(context);

                return _operation.SendSuccess(
                    $"نقش {role.Name} با موفقیت حذف شد، و هیچ ادمینی دیگر این نقش را ندارد.");
            }
            catch
            {
                return _operation.SendError("هنگام حذف نقش خطایی رخ داد. لطفاً دوباره تلاش کنید.");
            }
        }

        #endregion

        #region Private Helpers

        private async Task<Role?> GetRoleByIdAsync(int roleId)
        {
            return await _db.Roles.FindAsync(roleId);
        }

        private async Task<bool> IsRoleNameUniqueAsync(string name, int? excludeRoleId = null)
        {
            return !await _db.Roles
                .AsNoTracking()
                .AnyAsync(r => r.Name == name && (!excludeRoleId.HasValue || r.Id != excludeRoleId.Value));
        }

        private async Task<bool> IsPermissionsValidAsync(List<int> permissionsIdList)
        {
            if (permissionsIdList == null || permissionsIdList.Count == 0)
                return false;

            List<int> permissionsInDb = await _db.Permissions
                .AsNoTracking()
                .Select(s => s.Id)
                .ToListAsync();

            var set = new HashSet<int>(permissionsInDb);
            return permissionsIdList.All(id => set.Contains(id));
        }

        /// <summary>
        /// Synchronises the permission cache based on added/removed permissions after role update,
        /// and dispatches a <see cref="AdminEventType.RoleEdited"/> event.
        /// </summary>
        private async Task SyncRolePermissionsCacheAndDispatch(
            List<RolePermission> oldRolePermissions,
            int roleId,
            EditRoleDto updatedRole)
        {
            var oldPermIds = oldRolePermissions.Select(rp => rp.PermissionId).ToList();
            var newPermIds = updatedRole.PermissionIdList ?? new List<int>();

            var addedPermIds = newPermIds.Except(oldPermIds).ToList();
            var removedPermIds = oldPermIds.Except(newPermIds).ToList();

            var userIds = await _db.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            foreach (var userId in userIds)
            {
                _adminPermissionCache.AddPermissionsToUser(userId, addedPermIds);
                _adminPermissionCache.RemovePermissionsFromUser(userId, removedPermIds);
            }

            var roleListDto = new RoleListDto
            {
                Id = roleId,
                Name = updatedRole.Name,
                PermissionsCount = newPermIds.Count,
                UserHaveThisRoleCount = userIds.Count
            };

            var context = new AdminEventContext
            {
                EventType = AdminEventType.RoleEdited,
                ActorAdminId = _adminSession.GetAdminId(),
                ActorFullName = _adminSession.GetAdminFullName(),
                ActorUserName = _adminSession.GetAdminUserName(),
                RoleName = updatedRole.Name,
                RoleId = roleId,
                DataSyncPayload = roleListDto
            };

            await _eventDispatcher.DispatchAsync(context);
        }

        #endregion
    }
}