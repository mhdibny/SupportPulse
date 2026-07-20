#region Usings

using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPulse.Core.Security.ActionFilter;
using SupportPulse.Core.Services.TokenService;
using SupportPulse.Core.Services.Users;

#endregion

namespace SupportPulse.App.Controllers.Identity
{
    /// <summary>
    /// Handles authentication pages (login, sign‑up) and logout.
    /// </summary>
    public class IdentityController : Controller
    {
        #region Constructor & Dependencies

        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly ITokenService _tokenService;

        public IdentityController(
            IUserService userService,
            IMapper mapper,
            ITokenService tokenService)
        {
            _userService = userService;
            _mapper = mapper;
            _tokenService = tokenService;
        }

        #endregion

        #region Sign Up

        /// <summary>
        /// Displays the sign‑up page. Redirects authenticated users to the home page.
        /// </summary>
        [ResponseCache(Duration = 60)]
        [HttpGet]
        [Route("/SignUp")]
        [RedirectIfAuthenticated]
        public async Task<IActionResult> SignUp()
        {
            return View();
        }

        #endregion

        #region Login

        /// <summary>
        /// Displays the login page. Redirects authenticated users to the home page.
        /// </summary>
        [ResponseCache(Duration = 60)]
        [HttpGet]
        [Route("/Login")]
        [RedirectIfAuthenticated]
        public async Task<IActionResult> Login()
        {
            return View();
        }

        #endregion

        #region Logout

        /// <summary>
        /// Signs out the current user by revoking all refresh tokens, rotating the security stamp,
        /// clearing the authentication cookie, and redirecting to the login page.
        /// </summary>
        [Authorize]
        [HttpGet("/Logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                // Revoke all refresh tokens so they cannot be reused
                await _tokenService.RevokeAllRefreshTokensAsync(userId);

                // Rotate the security stamp to immediately invalidate all existing sessions
                await _userService.UpdateSecurityStampAsync(userId);
            }

            // Clear the authentication cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Identity");
        }

        #endregion
    }
}