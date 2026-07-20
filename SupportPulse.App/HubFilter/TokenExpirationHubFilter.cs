#region Usings

using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;

#endregion

namespace SupportPulse.App.HubFilter
{
    /// <summary>
    /// A SignalR hub filter that aborts the connection if the JWT access token has expired.
    /// </summary>
    public class TokenExpirationHubFilter : IHubFilter
    {
        /// <inheritdoc />
        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext invocationContext,
            Func<HubInvocationContext, ValueTask<object?>> next)
        {
            var httpContext = invocationContext.Context.GetHttpContext();
            var accessToken = httpContext?.Request.Query["access_token"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(accessToken);

                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    invocationContext.Context.Abort();
                    return null;
                }
            }

            return await next(invocationContext);
        }
    }
}