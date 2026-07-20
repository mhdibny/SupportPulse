#region Usings

using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

#endregion

namespace SupportPulse.App.HubFilter
{
    /// <summary>
    /// A SignalR hub filter that prevents replay attacks by validating a one‑time nonce
    /// extracted from the JWT access token during connection establishment.
    /// </summary>
    public class NonceValidationHubFilter : IHubFilter
    {
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Hub paths that require nonce validation.
        /// </summary>
        private static readonly string[] SecuredHubPaths = { "/chat", "/hubs/admin" };

        #region Constructor & Dependencies

        /// <summary>
        /// Initializes a new instance of the <see cref="NonceValidationHubFilter"/> class.
        /// </summary>
        /// <param name="cache">The memory cache used to store consumed nonces.</param>
        public NonceValidationHubFilter(IMemoryCache cache)
        {
            _cache = cache;
        }

        #endregion

        /// <inheritdoc />
        public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
        {
            var httpContext = context.Context.GetHttpContext();
            if (httpContext == null ||
                !SecuredHubPaths.Any(p =>
                    httpContext.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
            {
                await next(context);
                return;
            }

            // Read the access token from the query string (not from Context.User)
            var accessToken = httpContext.Request.Query["access_token"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                context.Context.Abort();
                return;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(accessToken);

                // Extract the nonce claim from the JWT
                var nonceClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "Nonce")?.Value;
                if (string.IsNullOrWhiteSpace(nonceClaim))
                {
                    context.Context.Abort();
                    return;
                }

                // Prevent replay attacks: each nonce may only be used once per hub path
                if (_cache.TryGetValue($"nonce:{httpContext.Request.Path}:{nonceClaim}", out _))
                {
                    context.Context.Abort();
                    return;
                }

                _cache.Set($"nonce:{httpContext.Request.Path}:{nonceClaim}", true, TimeSpan.FromMinutes(15));
            }
            catch
            {
                context.Context.Abort();
                return;
            }

            await next(context);
        }
    }
}