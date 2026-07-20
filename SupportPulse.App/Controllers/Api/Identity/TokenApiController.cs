#region Usings

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPulse.Core.DTOs.Token;
using SupportPulse.Core.Services.TokenService;

#endregion

namespace SupportPulse.App.Controllers.Api.Identity
{
    /// <summary>
    /// API endpoint for automatic JWT token renewal using a refresh token cookie.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/token")]
    public class TokenApiController : ControllerBase
    {
        #region Constructor & Dependencies

        private readonly ITokenService _tokenService;

        public TokenApiController(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        #endregion

        #region Token Renewal

        /// <summary>
        /// Validates the current refresh token cookie, issues a new access token,
        /// and sets a new refresh token cookie.
        /// </summary>
        [HttpGet("auto-renew")]
        public async Task<IActionResult> AutoRenew()
        {
            TokenRenewalResultDto result = await
                _tokenService.RenewAccessTokenAsync(Request.Cookies["X-Refresh-Token"]);

            if (!result.IsSuccess)
            {
                return Unauthorized();
            }

            await _tokenService.SetRefreshTokenCookieAsync(Response, result.RefreshToken);
            return Ok(new { accessToken = result.AccessToken });
        }

        #endregion
    }
}