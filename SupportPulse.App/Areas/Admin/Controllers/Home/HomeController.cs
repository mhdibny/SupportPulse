#region Usings

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPulse.Core.Security.ActionFilter;
using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.App.Areas.Admin.Controllers.Home
{
    /// <summary>
    /// Admin dashboard controller – provides the main admin page and its partial view.
    /// </summary>
    [Area("Admin")]
    [Authorize]
    [Route("/Admin")]
    [PermissionChecker(AdminPermission.Dashboard)]
    public class HomeController : Controller
    {
        #region Actions

        /// <summary>
        /// Returns the main admin dashboard view.
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Returns the dashboard partial view for AJAX navigation.
        /// </summary>
        [HttpGet("Dashboard")]
        public IActionResult Dashboard()
        {
            return PartialView("Partials/Dashboard/_DashboardPartial");
        }

        #endregion
    }
}