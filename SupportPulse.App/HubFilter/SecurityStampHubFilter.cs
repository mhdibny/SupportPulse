#region Usings

using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using SupportPulse.Core.Services.Users;

#endregion

namespace SupportPulse.App.HubFilter
{
    /// <summary>
    /// A SignalR hub filter that validates the current user's security stamp before
    /// allowing a hub method to execute. If the stamp is no longer valid, the connection is aborted.
    /// </summary>
    public class SecurityStampHubFilter : IHubFilter
    {
        /// <summary>
        /// Hub paths that require security stamp validation.
        /// </summary>
        private static readonly string[] SecuredHubPaths = { "/chat", "/hubs/admin" };

        #region IHubFilter

        /// <inheritdoc />
        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext invocationContext,
            Func<HubInvocationContext, ValueTask<object?>> next)
        {
            var httpContext = invocationContext.Context.GetHttpContext();
            if (httpContext == null)
                return await next(invocationContext);

            // Skip if the hub path is not in the secured list
            if (!SecuredHubPaths.Any(p =>
                    httpContext.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
            {
                return await next(invocationContext);
            }

            // Ensure the user is authenticated (should normally be the case)
            var user = invocationContext.Context.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                invocationContext.Context.Abort();
                return null;
            }

            // Validate the security stamp
            var userService = invocationContext.ServiceProvider.GetRequiredService<IUserService>();
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var stampClaim = user.FindFirst("SecurityStamp")?.Value;

            if (int.TryParse(userIdClaim, out int userId) && stampClaim != null)
            {
                if (!await userService.IsSecurityStampValidAsync(userId, stampClaim))
                {
                    invocationContext.Context.Abort();
                    return null;
                }
            }

            return await next(invocationContext);
        }

        #endregion
    }
}