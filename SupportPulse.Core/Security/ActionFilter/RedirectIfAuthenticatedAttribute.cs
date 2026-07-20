#region Usings

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

#endregion

namespace SupportPulse.Core.Security.ActionFilter
{
    /// <summary>
    /// Redirects authenticated users away from pages that should only be visible to guests
    /// (e.g., login and sign‑up pages).
    /// </summary>
    public class RedirectIfAuthenticatedAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// The controller to redirect to. Default is <c>Home</c>.
        /// </summary>
        public string RedirectController { get; set; } = "Home";

        /// <summary>
        /// The action to redirect to. Default is <c>Index</c>.
        /// </summary>
        public string RedirectAction { get; set; } = "Index";

        /// <inheritdoc />
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;
            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                context.Result = new RedirectToActionResult(RedirectAction, RedirectController, null);
            }
        }
    }
}