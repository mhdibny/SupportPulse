#region Usings

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPulse.Core.Security.ActionFilter;
using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.App.Areas.Admin.Controllers.Role
{
    /// <summary>
    /// Handles role management pages and partial views for the admin SPA.
    /// </summary>
    [Area("Admin")]
    [Authorize]
    [Route("/Admin/Roles")]
    public class RolesController : Controller
    {
        #region List

        /// <summary>
        /// Returns the role list page.
        /// </summary>
        [HttpGet]
        [PermissionChecker(AdminPermission.RoleList)]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Returns the role list partial view for AJAX navigation.
        /// </summary>
        [HttpGet("Partial")]
        [PermissionChecker(AdminPermission.RoleList)]
        public IActionResult Partial()
        {
            return PartialView("Partials/Roles/_RoleListPartial");
        }

        #endregion

        #region Add

        /// <summary>
        /// Returns the add‑role view (full page or partial for AJAX).
        /// </summary>
        [HttpGet("Add/")]
        [PermissionChecker(AdminPermission.AddRole)]
        public IActionResult Add()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Partials/Roles/_AddRolePartial");
            }
            return View();
        }

        /// <summary>
        /// Returns the add‑role partial view (used by SPA navigation).
        /// </summary>
        [HttpGet("Add/Partial")]
        [PermissionChecker(AdminPermission.AddRole)]
        public IActionResult AddPartial()
        {
            return PartialView("Partials/Roles/_AddRolePartial");
        }

        #endregion

        #region Edit

        /// <summary>
        /// Returns the edit‑role view (full page or partial for AJAX).
        /// </summary>
        /// <param name="roleId">The role identifier.</param>
        [HttpGet("Edit/{roleId}")]
        [PermissionChecker(AdminPermission.EditRole)]
        public IActionResult Edit(int roleId)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Partials/Roles/_EditRolePartial");
            }
            ViewBag.RoleId = roleId;
            return View();
        }

        #endregion

        #region Delete

        /// <summary>
        /// Returns the delete‑role confirmation view (full page or partial for AJAX).
        /// </summary>
        /// <param name="roleId">The role identifier.</param>
        [HttpGet("Delete/{roleId}")]
        [PermissionChecker(AdminPermission.DeleteRole)]
        public IActionResult Delete(int roleId)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Partials/Roles/_DeleteRolePartial");
            }
            ViewBag.RoleId = roleId;
            return View();
        }

        #endregion
    }
}