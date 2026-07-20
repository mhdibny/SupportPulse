#region Usings

using Microsoft.EntityFrameworkCore;
using SupportPulse.Core.DTOs.Admin.Common;
using SupportPulse.Core.DTOs.Admin.EventDispatcher;
using SupportPulse.Core.DTOs.Admin.User;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.Services.Admin.EventDispatcher;
using SupportPulse.Core.Services.Admin.PermissionCache;
using SupportPulse.Core.Services.Admin.Session;
using SupportPulse.Core.Services.Common;
using SupportPulse.Data.Context;
using SupportPulse.Data.Entities.User;
using SupportPulse.Data.Entities.User.SupportCategory;
using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.Core.Services.Admin.Users
{
    /// <summary>
    /// Handles admin‑related user operations, including permission checks,
    /// user listing, role/support‑category assignment, and event dispatching.
    /// </summary>
    public class AdminUserService : IAdminUserService
    {
        #region Constructor & Dependencies

        private readonly ApplicationDbContext _db;
        private readonly IOperationResultAction _operation;
        private readonly IAdminPermissionCacheService _adminPermissionCache;
        private readonly ICurrentAdminSession _adminSession;
        private readonly IAdminEventDispatcher _eventDispatcher;

        public AdminUserService(
            ApplicationDbContext db,
            IOperationResultAction operation,
            IAdminPermissionCacheService adminPermissionCache,
            ICurrentAdminSession adminSession,
            IAdminEventDispatcher eventDispatcher)
        {
            _db = db;
            _operation = operation;
            _adminPermissionCache = adminPermissionCache;
            _adminSession = adminSession;
            _eventDispatcher = eventDispatcher;
        }

        #endregion

        #region Permission Checker

        /// <inheritdoc />
        public async Task<bool> UserHasPermissionAsync(int userId, int permissionId)
        {
            return await _db.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_db.RolePermissions,
                    ur => ur.RoleId,
                    rp => rp.RoleId,
                    (ur, rp) => rp)
                .AnyAsync(rp => rp.PermissionId == permissionId);
        }

        #endregion

        #region Common

        /// <inheritdoc />
        public async Task<bool> IsThisUserAdminAsync(int userId)
        {
            return await _db.UserRoles.AnyAsync(a => a.UserId == userId);
        }

        #endregion

        #region User List

        /// <inheritdoc />
        public async Task<OperationResult<PagedResult<UserListDto>>> GetUserListAsync(
            UserSearchTermDto? search, UserPageRequestDto? paging = default)
        {
            paging ??= new UserPageRequestDto();

            int skip = (paging.PageNumber - 1) * paging.PageSize;
            int take = paging.PageSize;

            IQueryable<User> query = _db.Users.AsNoTracking();

            // Apply search filters if provided
            if (search is not null)
            {
                if (!string.IsNullOrWhiteSpace(search.FirstName))
                    query = query.Where(r => r.FirstName.Contains(search.FirstName));

                if (!string.IsNullOrWhiteSpace(search.LastName))
                    query = query.Where(r => r.LastName.Contains(search.LastName));

                if (!string.IsNullOrWhiteSpace(search.UserName))
                    query = query.Where(r => r.UserName.Contains(search.UserName));

                if (search.IsBanned is not null)
                    query = query.Where(r => r.IsBan == search.IsBanned.Value);
            }

            try
            {
                int totalCount = await query.CountAsync();

                List<UserListDto> users = await query
                    .Select(s => new UserListDto
                    {
                        Id = s.Id,
                        FirstName = s.FirstName,
                        LastName = s.LastName,
                        UserName = s.UserName,
                        IsBanned = s.IsBan,
                        IsAdmin = s.Roles!.Any(),
                        RoleCount = s.Roles.Count
                    })
                    .OrderBy(o => o.Id)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                var pagesResult = new PagedResult<UserListDto>
                {
                    Items = users,
                    PageNumber = paging.PageNumber,
                    PageSize = paging.PageSize,
                    TotalCount = totalCount
                };

                return _operation.SendSuccess(entity: pagesResult);
            }
            catch
            {
                return _operation.SendError<PagedResult<UserListDto>>(
                    "هنگام دریافت اطلاعات کاربران خطایی رخ داد");
            }
        }

        #endregion

        #region User Info for Ban

        /// <inheritdoc />
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _db.Users.FindAsync(userId);
        }

        /// <inheritdoc />
        public async Task<UserInformationForBanDto?> GetUserInformationForBanAsync(int userId)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(s => new UserInformationForBanDto
                {
                    UserId = s.Id,
                    FullName = s.FullName,
                    UserName = s.UserName
                })
                .SingleOrDefaultAsync();
        }

        #endregion

        #region User Roles

        /// <inheritdoc />
        public async Task<OperationResult<UserRolesDto>> GetUserRolesAsync(int userId)
        {
            var userRoles = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(s => new UserRolesDto
                {
                    UserId = s.Id,
                    FullName = s.FullName,
                    UserName = s.UserName,
                    UserRolesIdList = s.Roles!.Select(ur => ur.RoleId).ToList()
                })
                .SingleOrDefaultAsync();

            if (userRoles is null)
            {
                return _operation.SendError<UserRolesDto>(
                    "کاربر مورد نظر یافت نشد.", OperationStatus.ValidationError);
            }

            return _operation.SendSuccess(entity: userRoles);
        }

        /// <inheritdoc />
        public async Task<OperationResult> AddOrEditUserRolesAsync(UserRolesDto roles)
        {
            bool userExists = await _db.Users.AnyAsync(u => u.Id == roles.UserId);
            if (!userExists)
            {
                return _operation.SendError("کاربر مورد نظر یافت نشد", OperationStatus.ValidationError);
            }

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                // Retrieve and remove existing roles
                List<UserRole> userRolesInDb = await _db.UserRoles
                    .Where(ur => ur.UserId == roles.UserId)
                    .ToListAsync();

                if (userRolesInDb.Any())
                {
                    _db.UserRoles.RemoveRange(userRolesInDb);
                }

                string message = "تمام نقش های کاربر حذف شدند.";

                // Assign new roles (if any)
                if (roles.UserRolesIdList.Count > 0)
                {
                    if (!await IsRolesValidAsync(roles.UserRolesIdList))
                    {
                        return _operation.SendError("لطفا از نقش معتبر استفاده کنید.");
                    }

                    var newRoles = roles.UserRolesIdList
                        .Select(roleId => new UserRole { UserId = roles.UserId, RoleId = roleId })
                        .ToList();

                    await _db.UserRoles.AddRangeAsync(newRoles);

                    message = userRolesInDb.Count > 0
                        ? "نقش های کاربر ویرایش شد"
                        : "نقش جدید به کاربر اضافه شد.";
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // Update permission cache and dispatch event
                await SyncRoleCacheAndDispatch(userRolesInDb, roles);

                return _operation.SendSuccess(message);
            }
            catch
            {
                await transaction.RollbackAsync();
                return _operation.SendError();
            }
        }

        #endregion

        #region User Support Categories

        /// <inheritdoc />
        public async Task<OperationResult<UserSupportCategoryDto>> GetUserSupportCategoriesAsync(int userId)
        {
            var userSupportCategories = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(s => new UserSupportCategoryDto
                {
                    UserId = s.Id,
                    FullName = s.FullName,
                    UserName = s.UserName,
                    SupportCategoryIdList = s.SupportCategories!
                        .Select(us => us.SupportCategoryId)
                        .ToList()
                })
                .SingleOrDefaultAsync();

            if (userSupportCategories is null)
            {
                return _operation.SendError<UserSupportCategoryDto>(
                    "کاربر مورد نظر یافت نشد.", OperationStatus.ValidationError);
            }

            return _operation.SendSuccess(entity: userSupportCategories);
        }

        /// <inheritdoc />
        public async Task<OperationResult> AddOrEditUserSupportCategoriesAsync(
            UserSupportCategoryDto supportCategory)
        {
            bool userExists = await _db.Users.AnyAsync(u => u.Id == supportCategory.UserId);
            if (!userExists)
            {
                return _operation.SendError("کاربر مورد نظر یافت نشد", OperationStatus.ValidationError);
            }

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                // Retrieve and remove existing support categories
                List<UserSupportCategory> userSupportCategoriesInDb = await _db.UserSupportCategories
                    .Where(us => us.UserId == supportCategory.UserId)
                    .ToListAsync();

                if (userSupportCategoriesInDb.Any())
                {
                    _db.UserSupportCategories.RemoveRange(userSupportCategoriesInDb);
                }

                string message = "تمام واحد های پشتیبانی از کاربر گرفته شد.";

                // Assign new support categories (if any)
                if (supportCategory.SupportCategoryIdList.Count > 0)
                {
                    if (!await IsSupportCategoriesValidAsync(supportCategory.SupportCategoryIdList))
                    {
                        return _operation.SendError("لطفا از واحد پشتیبانی معتبر استفاده کنید.");
                    }

                    var newSupportCategories = supportCategory.SupportCategoryIdList
                        .Select(id => new UserSupportCategory
                        {
                            UserId = supportCategory.UserId,
                            SupportCategoryId = id
                        })
                        .ToList();

                    await _db.UserSupportCategories.AddRangeAsync(newSupportCategories);

                    message = userSupportCategoriesInDb.Count > 0
                        ? "واحد های پشتیبانی کاربر ویرایش شد"
                        : "واحد پشتیبانی جدید به کاربر اضافه شد.";
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // Update cache and dispatch event
                await SyncSupportCategoryCacheAndDispatch(userSupportCategoriesInDb, supportCategory);

                return _operation.SendSuccess(message);
            }
            catch
            {
                await transaction.RollbackAsync();
                return _operation.SendError();
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Updates the permission cache and dispatches a <see cref="AdminEventType.UserRolesChanged"/> event.
        /// </summary>
        private async Task SyncRoleCacheAndDispatch(
            List<UserRole> oldUserRoles,
            UserRolesDto newRoles)
        {
            var oldRoleIds = oldUserRoles.Select(ur => ur.RoleId).ToList();
            var newRoleIds = newRoles.UserRolesIdList;

            // Calculate added / removed permissions
            var oldPermIds = await _db.RolePermissions
                .Where(rp => oldRoleIds.Contains(rp.RoleId))
                .Select(rp => rp.PermissionId)
                .Distinct()
                .ToListAsync();

            var newPermIds = await _db.RolePermissions
                .Where(rp => newRoleIds.Contains(rp.RoleId))
                .Select(rp => rp.PermissionId)
                .Distinct()
                .ToListAsync();

            var addedPermIds = newPermIds.Except(oldPermIds).ToList();
            var removedPermIds = oldPermIds.Except(newPermIds).ToList();

            // Apply to cache
            _adminPermissionCache.AddPermissionsToUser(newRoles.UserId, addedPermIds);
            _adminPermissionCache.RemovePermissionsFromUser(newRoles.UserId, removedPermIds);

            // Fetch user info for the event context
            var userInfo = await _db.Users
                .Where(u => u.Id == newRoles.UserId)
                .Select(u => new { u.FullName, u.UserName })
                .FirstOrDefaultAsync();

            var context = new AdminEventContext
            {
                EventType = AdminEventType.UserRolesChanged,
                ActorAdminId = _adminSession.GetAdminId(),
                ActorFullName = _adminSession.GetAdminFullName(),
                ActorUserName = _adminSession.GetAdminUserName(),
                TargetUserId = newRoles.UserId,
                TargetFullName = userInfo?.FullName ?? "",
                TargetUserName = userInfo?.UserName ?? "",
                DataSyncPayload = new UserListDto
                {
                    Id = newRoles.UserId,
                    UserName = userInfo?.UserName ?? "",
                    FirstName = userInfo?.FullName?.Split(' ').FirstOrDefault() ?? "",
                    LastName = userInfo?.FullName?.Split(' ').LastOrDefault() ?? "",
                    IsBanned = false,
                    IsAdmin = true,
                    RoleCount = newRoleIds.Count
                }
            };

            await _eventDispatcher.DispatchAsync(context);
        }

        /// <summary>
        /// Updates the authorization cache for support categories and dispatches a
        /// <see cref="AdminEventType.UserSupportCategoriesChanged"/> event.
        /// </summary>
        private async Task SyncSupportCategoryCacheAndDispatch(
            List<UserSupportCategory> oldCategories,
            UserSupportCategoryDto newCategories)
        {
            var oldCatIds = oldCategories.Select(us => us.SupportCategoryId).ToList();
            var newCatIds = newCategories.SupportCategoryIdList;

            var addedCatIds = newCatIds.Except(oldCatIds).ToList();
            var removedCatIds = oldCatIds.Except(newCatIds).ToList();

            foreach (var catId in addedCatIds)
                _adminPermissionCache.AddSupportCategoryToUser(newCategories.UserId, catId);

            foreach (var catId in removedCatIds)
                _adminPermissionCache.RemoveSupportCategoryFromUser(newCategories.UserId, catId);

            var userInfo = await _db.Users
                .Where(u => u.Id == newCategories.UserId)
                .Select(u => new { u.FullName, u.UserName })
                .FirstOrDefaultAsync();

            var context = new AdminEventContext
            {
                EventType = AdminEventType.UserSupportCategoriesChanged,
                ActorAdminId = _adminSession.GetAdminId(),
                ActorFullName = _adminSession.GetAdminFullName(),
                ActorUserName = _adminSession.GetAdminUserName(),
                TargetUserId = newCategories.UserId,
                TargetFullName = userInfo?.FullName ?? "",
                TargetUserName = userInfo?.UserName ?? "",
                DataSyncPayload = new UserListDto
                {
                    Id = newCategories.UserId,
                    UserName = userInfo?.UserName ?? "",
                    FirstName = userInfo?.FullName?.Split(' ').FirstOrDefault() ?? "",
                    LastName = userInfo?.FullName?.Split(' ').LastOrDefault() ?? "",
                    IsBanned = false,
                    IsAdmin = true,
                    RoleCount = 0
                }
            };

            await _eventDispatcher.DispatchAsync(context);
        }

        private async Task<bool> IsRolesValidAsync(List<int> rolesIdList)
        {
            if (rolesIdList is null || rolesIdList.Count == 0)
                return false;

            List<int> rolesInDb = await _db.Roles
                .AsNoTracking()
                .Select(s => s.Id)
                .ToListAsync();

            var rolesInDbSet = new HashSet<int>(rolesInDb);
            return rolesIdList.All(id => rolesInDbSet.Contains(id));
        }

        private async Task<bool> IsSupportCategoriesValidAsync(List<int> supportCategoryIdList)
        {
            if (supportCategoryIdList is null || supportCategoryIdList.Count == 0)
                return false;

            List<int> categoriesInDb = await _db.SupportCategories
                .AsNoTracking()
                .Select(s => s.Id)
                .ToListAsync();

            var categoriesInDbSet = new HashSet<int>(categoriesInDb);
            return supportCategoryIdList.All(id => categoriesInDbSet.Contains(id));
        }

        #endregion
    }
}