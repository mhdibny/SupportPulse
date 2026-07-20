#region Usings

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SupportPulse.Core.Services.Admin.Users;
using SupportPulse.Core.Utilities.ClaimsPrincipals;

#endregion

namespace SupportPulse.Core.Security.ActionFilter
{
    /// <summary>
    /// An async authorization filter that verifies the current user possesses the required admin permission.
    /// </summary>
    public class PermissionCheckerFilter : IAsyncAuthorizationFilter
    {
        #region Constructor & Dependencies

        private readonly int _permissionId;
        private readonly IAdminUserService _adminUserService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionCheckerFilter"/> class.
        /// </summary>
        /// <param name="permissionId">The admin permission identifier to check.</param>
        /// <param name="adminUserService">The admin user service.</param>
        public PermissionCheckerFilter(int permissionId, IAdminUserService adminUserService)
        {
            _permissionId = permissionId;
            _adminUserService = adminUserService;
        }

        #endregion

        #region IAsyncAuthorizationFilter

        /// <inheritdoc />
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user is null)
            {
                context.Result = new NotFoundResult();
                return;
            }

            if (!await _adminUserService.UserHasPermissionAsync(user.GetUserIdAsInt(), _permissionId))
            {
                context.Result = new NotFoundResult();
            }
        }

        #endregion
    }
}