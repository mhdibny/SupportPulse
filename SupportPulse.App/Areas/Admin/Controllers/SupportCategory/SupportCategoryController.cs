#region Usings

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPulse.Core.Security.ActionFilter;
using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.App.Areas.Admin.Controllers.SupportCategory
{
    /// <summary>
    /// Handles support category management pages and partial views for the admin SPA.
    /// </summary>
    [Area("Admin")]
    [Authorize]
    [Route("/Admin/SupportCategories")]
    public class SupportCategoryController : Controller
    {
        #region List

        /// <summary>
        /// Returns the support category list page.
        /// </summary>
        [HttpGet]
        [PermissionChecker(AdminPermission.SupportCategoryList)]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Returns the support category list partial view for AJAX navigation.
        /// </summary>
        [HttpGet("Partial")]
        [PermissionChecker(AdminPermission.SupportCategoryList)]
        public IActionResult Partial()
        {
            return PartialView("Partials/SupportCategories/_SupportCategoryListPartial");
        }

        #endregion

        #region Add

        /// <summary>
        /// Returns the add‑support‑category view (full page or partial for AJAX).
        /// </summary>
        [HttpGet("Add/")]
        [PermissionChecker(AdminPermission.AddSupportCategory)]
        public IActionResult Add()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Partials/SupportCategories/_AddSupportCategoryPartial");
            }
            return View();
        }

        /// <summary>
        /// Returns the add‑support‑category partial view (used by SPA navigation).
        /// </summary>
        [HttpGet("Add/Partial")]
        [PermissionChecker(AdminPermission.AddSupportCategory)]
        public IActionResult AddPartial()
        {
            return PartialView("Partials/SupportCategories/_AddSupportCategoryPartial");
        }

        #endregion

        #region Edit

        /// <summary>
        /// Returns the edit‑support‑category view (full page or partial for AJAX).
        /// </summary>
        /// <param name="supportCategoryId">The support category identifier.</param>
        [HttpGet("Edit/{supportCategoryId}")]
        [PermissionChecker(AdminPermission.EditSupportCategory)]
        public IActionResult Edit(int supportCategoryId)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Partials/SupportCategories/_EditSupportCategoryPartial");
            }

            ViewBag.SupportCategoryId = supportCategoryId;
            return View();
        }

        #endregion
    }
}