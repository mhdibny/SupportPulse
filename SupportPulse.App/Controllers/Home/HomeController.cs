#region Usings

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPulse.App.Factories.Home;
using SupportPulse.Core.Utilities.ClaimsPrincipals;

#endregion

namespace SupportPulse.App.Controllers.Home
{
    /// <summary>
    /// Controller for the main home page and error pages.
    /// </summary>
    public class HomeController : Controller
    {
        #region Constructor & Dependencies

        private readonly HomeViewModelFactory _viewModelFactory;

        public HomeController(HomeViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        #endregion

        #region Index

        /// <summary>
        /// Displays the home page with the user's chats and available support categories.
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var model = await _viewModelFactory.CreateIndexModelAsync(User.GetUserIdAsInt());
            return View(model);
        }

        #endregion

        #region Error

        /// <summary>
        /// Displays a friendly error page based on the HTTP status code.
        /// </summary>
        /// <param name="code">The HTTP status code.</param>
        public async Task<IActionResult> Error(int code)
        {
            return code switch
            {
                400 => View("Errors/400"),
                401 => View("Errors/401"),
                403 => View("Errors/403"),
                404 => View("Errors/404"),
                500 => View("Errors/500"),
                503 => View("Errors/503"),
                _ => View("Errors/404")
            };
        }

        #endregion
    }
}