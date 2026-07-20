#region Usings

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPulse.Core.Security.ActionFilter;
using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.App.Areas.Admin.Controllers.Chat
{
    /// <summary>
    /// Provides the admin chat page and its partial view for the SPA framework.
    /// </summary>
    [Area("Admin")]
    [Authorize]
    [Route("/Admin/Chats")]
    [PermissionChecker(AdminPermission.ViewChatList)]
    public class ChatController : Controller
    {
        #region Actions

        /// <summary>
        /// Returns the main chat management page.
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Returns the partial view containing the chat list for AJAX navigation.
        /// </summary>
        [HttpGet("Partial")]
        public IActionResult Partial()
        {
            return PartialView("Partials/Chat/_AdminChatListPartial");
        }

        #endregion
    }
}