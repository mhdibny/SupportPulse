#region Usings

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SupportPulse.Core.Services.Users;
using System.Security.Claims;

#endregion

namespace SupportPulse.App.Middleware
{
    /// <summary>
    /// Validates the user's security stamp on each request. If the stamp is invalid,
    /// the user is signed out and redirected to the login page.
    /// </summary>
    public class SecurityStampValidatorMiddleware
    {
        private readonly RequestDelegate _next;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityStampValidatorMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public SecurityStampValidatorMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        #endregion

        /// <summary>
        /// Invokes the middleware, checking the security stamp of the authenticated user.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="userService">The user service for stamp validation.</param>
        public async Task InvokeAsync(HttpContext context, IUserService userService)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var stampClaim = context.User.FindFirst("SecurityStamp")?.Value;

                if (int.TryParse(userIdClaim, out int userId) && stampClaim != null)
                {
                    if (!await userService.IsSecurityStampValidAsync(userId, stampClaim))
                    {
                        // Security stamp is invalid – force sign out and redirect to login
                        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                        context.Response.Cookies.Delete("X-Refresh-Token", new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Path = "/"
                        });

                        context.Response.Redirect("/Login");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}