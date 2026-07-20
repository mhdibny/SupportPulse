#region Usings

using SupportPulse.Core.DTOs.Admin.Role;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Data.Enums.Admin;
#endregion

namespace SupportPulse.Core.Services.Admin.Roles
{
    /// <summary>
    /// Defines operations for managing roles and their permissions.
    /// </summary>
    public interface IRoleService
    {
        #region Permissions

        /// <summary>
        /// Returns all available permissions.
        /// </summary>
        Task<List<PermissionDto>> GetPermissionsAsync();

        #endregion

        #region Role Queries

        /// <summary>
        /// Returns a flat list of all roles with basic statistics.
        /// </summary>
        Task<OperationResult<List<RoleListDto>>> GetRoleListAsync();

        /// <summary>
        /// Returns the details of a single role for editing.
        /// </summary>
        /// <param name="roleId">The role identifier.</param>
        Task<OperationResult<RoleDto>> GetRoleForEditAsync(int roleId);

        /// <summary>
        /// Returns role details needed for the delete confirmation dialog.
        /// </summary>
        /// <param name="roleId">The role identifier.</param>
        Task<OperationResult<DeleteRoleDto>> GetRoleForDelete(int roleId);

        /// <summary>
        /// Searches roles by name and/or assigned permissions.
        /// </summary>
        /// <param name="search">Search criteria (name and/or permission IDs).</param>
        Task<OperationResult<List<RoleListDto>>> SearchInRolesAsync(SearchRoleDto? search);

        /// <summary>
        /// Returns all roles with their full permission details (used for assignment).
        /// </summary>
        Task<OperationResult<List<RoleDto>>> GetRolesListAsync();

        #endregion

        #region Role Mutation

        /// <summary>
        /// Creates a new role and dispatches a <see cref="AdminEventType.RoleCreated"/> event.
        /// </summary>
        /// <param name="role">The role creation data.</param>
        Task<OperationResult> AddRoleAsync(AddRoleDto role);

        /// <summary>
        /// Edits an existing role, updates the permission cache, and dispatches a <see cref="AdminEventType.RoleEdited"/> event.
        /// </summary>
        /// <param name="role">The role update data.</param>
        Task<OperationResult> EditRoleAsync(EditRoleDto role);

        /// <summary>
        /// Deletes a role, updates the permission cache, and dispatches a <see cref="AdminEventType.RoleDeleted"/> event.
        /// </summary>
        /// <param name="roleId">The role identifier.</param>
        Task<OperationResult> DeleteRoleAsync(int roleId);

        #endregion
    }
}