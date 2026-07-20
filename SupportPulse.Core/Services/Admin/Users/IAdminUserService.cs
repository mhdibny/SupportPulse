#region Usings

using SupportPulse.Core.DTOs.Admin.Common;
using SupportPulse.Core.DTOs.Admin.User;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Data.Entities.User;
using SupportPulse.Data.Enums.Admin;
#endregion

namespace SupportPulse.Core.Services.Admin.Users
{
    /// <summary>
    /// Defines operations for managing admin users, their roles, support categories, and ban-related data.
    /// </summary>
    public interface IAdminUserService
    {
        #region Permission Checker

        /// <summary>
        /// Checks whether the specified user has the given permission.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="permissionId">The permission identifier.</param>
        Task<bool> UserHasPermissionAsync(int userId, int permissionId);

        #endregion

        #region Common

        /// <summary>
        /// Determines whether the given user is an admin (has at least one role).
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        Task<bool> IsThisUserAdminAsync(int userId);

        #endregion

        #region User List

        /// <summary>
        /// Retrieves a paginated list of users with optional search and ban filters.
        /// </summary>
        /// <param name="search">Optional search criteria.</param>
        /// <param name="paging">Pagination parameters (page number and page size).</param>
        Task<OperationResult<PagedResult<UserListDto>>> GetUserListAsync(
            UserSearchTermDto? search, UserPageRequestDto? paging = default);

        #endregion

        #region User Info for Ban

        /// <summary>
        /// Returns the full <see cref="User"/> entity by its identifier.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        Task<User?> GetUserByIdAsync(int userId);

        /// <summary>
        /// Returns basic user information needed for ban operations.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        Task<UserInformationForBanDto?> GetUserInformationForBanAsync(int userId);

        #endregion

        #region User Roles

        /// <summary>
        /// Returns the roles currently assigned to a user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        Task<OperationResult<UserRolesDto>> GetUserRolesAsync(int userId);

        /// <summary>
        /// Replaces the existing roles of a user with a new set of roles.
        /// Also updates the permission cache and dispatches a <see cref="AdminEventType.UserRolesChanged"/> event.
        /// </summary>
        /// <param name="roles">The new role assignment.</param>
        Task<OperationResult> AddOrEditUserRolesAsync(UserRolesDto roles);

        #endregion

        #region User Support Categories

        /// <summary>
        /// Returns the support categories currently assigned to a user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        Task<OperationResult<UserSupportCategoryDto>> GetUserSupportCategoriesAsync(int userId);

        /// <summary>
        /// Replaces the existing support categories of a user with a new set.
        /// Also updates the authorization cache and dispatches a <see cref="AdminEventType.UserSupportCategoriesChanged"/> event.
        /// </summary>
        /// <param name="supportCategory">The new support category assignment.</param>
        Task<OperationResult> AddOrEditUserSupportCategoriesAsync(UserSupportCategoryDto supportCategory);

        #endregion
    }
}