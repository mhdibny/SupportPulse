#region Usings

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPulse.Core.Security.ActionFilter;
using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.App.Areas.Admin.Controllers.User
{
    /// <summary>
    /// Handles user management pages and partial views for the admin SPA.
    /// </summary>
    [Area("Admin")]
    [Authorize]
    [Route("/Admin/Users")]
    public class UsersController : Controller
    {
        #region List

        /// <summary>
        /// Returns the user list page.
        /// </summary>
        [HttpGet]
        [PermissionChecker(AdminPermission.UserList)]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Returns the user list partial view for AJAX navigation.
        /// </summary>
        [HttpGet("Partial")]
        [PermissionChecker(AdminPermission.UserList)]
        public IActionResult Partial()
        {
            return PartialView("Partials/Users/_UserListPartial");
        }

        #endregion

        #region Roles

        /// <summary>
        /// Returns the role assignment view for a specific user (full page or partial for AJAX).
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        [HttpGet("Roles/{userId}")]
        [PermissionChecker(AdminPermission.AssignRoleToUser)]
        public IActionResult Roles(int userId)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Partials/Users/_UserRolesPartial");
            }

            ViewBag.UserId = userId;
            return View();
        }

        #endregion

        #region Support Categories

        /// <summary>
        /// Returns the support category assignment view for a specific user (full page or partial for AJAX).
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        [HttpGet("SupportCategory/{userId}")]
        [PermissionChecker(AdminPermission.AssignSupportCategoryToUser)]
        public IActionResult SupportCategory(int userId)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Partials/Users/_UserSupportCategoryPartial");
            }

            ViewBag.UserId = userId;
            return View();
        }

        #endregion
    }
}