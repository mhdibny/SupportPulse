#region Usings

using Microsoft.AspNetCore.SignalR;
using SupportPulse.Core.Security.ActionFilter.Hub;
using SupportPulse.Core.Services.Admin.Users;
using SupportPulse.Core.Utilities.ClaimsPrincipals;
using System.Collections.Concurrent;
using System.Reflection;

#endregion

namespace SupportPulse.App.HubFilter
{
    /// <summary>
    /// A SignalR hub filter that enforces permission checks on hub methods decorated
    /// with <see cref="HubPermissionChecker"/> attributes.
    /// </summary>
    public class PermissionCheckerHubFilter : IHubFilter
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Caches the <see cref="HubPermissionChecker"/> attribute for each hub method
        /// to avoid repeated reflection lookups.
        /// </summary>
        private static readonly ConcurrentDictionary<MethodInfo, HubPermissionChecker?> _methodCache = new();

        #region Constructor & Dependencies

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionCheckerHubFilter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve scoped services.</param>
        public PermissionCheckerHubFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        #endregion

        /// <inheritdoc />
        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext invocationContext,
            Func<HubInvocationContext, ValueTask<object?>> next)
        {
            // 1. Verify the user is authenticated
            var user = invocationContext.Context.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
            {
                throw new HubException("احراز هویت الزامی است.");
            }

            // 2. Retrieve the permission attribute from the cache (no extra reflection)
            var method = invocationContext.HubMethod;
            var attribute = _methodCache.GetOrAdd(method, m => m.GetCustomAttribute<HubPermissionChecker>());

            // 3. If the method has no attribute, proceed without overhead
            if (attribute == null)
                return await next(invocationContext);

            // 4. Check the required permission
            using var scope = _serviceProvider.CreateScope();
            var adminUserService = scope.ServiceProvider.GetRequiredService<IAdminUserService>();
            var userId = user.GetUserIdAsInt();

            if (!await adminUserService.UserHasPermissionAsync(userId, attribute.PermissionId))
            {
                throw new HubException("شما مجوز لازم برای این عملیات را ندارید.");
            }

            return await next(invocationContext);
        }
    }
}