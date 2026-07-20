#region Usings

using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SupportPulse.App.Security.HubModelValidator;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.DTOs.User;
using SupportPulse.Core.Services.TokenService;
using SupportPulse.Core.Services.Users;
using SupportPulse.Core.ViewModels.User;
using System.Security.Claims;

#endregion

namespace SupportPulse.App.Controllers.Api.Identity
{
    /// <summary>
    /// API endpoints for user authentication (login and registration).
    /// </summary>
    [ApiController]
    [Route("api/identity")]
    public class IdentityApiController : ControllerBase
    {
        #region Constructor & Dependencies

        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public IdentityApiController(
            IUserService userService,
            ITokenService tokenService,
            IMapper mapper)
        {
            _userService = userService;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        #endregion

        #region Login

        /// <summary>
        /// Authenticates a user with username and password,
        /// issues a refresh token cookie, and signs in the user via MVC cookie.
        /// </summary>
        /// <param name="login">The login credentials.</param>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserVM login)
        {
            var validationResult = HubModelValidator.Validate(login);
            if (!validationResult.IsSuccess)
            {
                return BadRequest(validationResult.Alert);
            }

            var loginResult = await _userService.LoginUserAsync(login);
            if (!loginResult.IsSuccess || loginResult.Data.UserId == 0)
            {
                return Unauthorized(_mapper.Map<SystemAlertDto>(loginResult));
            }

            await _tokenService.GenerateAndSetRefreshTokenCookieAsync(Response, loginResult.Data.UserId);
            await SignInMvcAsync(loginResult.Data);

            return Ok();
        }

        #endregion

        #region Register

        /// <summary>
        /// Registers a new user, issues a refresh token cookie, and signs in the user.
        /// </summary>
        /// <param name="registerUser">The registration data.</param>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] SignUpUserVM registerUser)
        {
            var validationResult = HubModelValidator.Validate(registerUser);
            if (!validationResult.IsSuccess)
            {
                return BadRequest(validationResult.Alert);
            }

            var signUpResult = await _userService.SignUpUserAsync(registerUser);
            if (!signUpResult.IsSuccess)
            {
                return BadRequest(_mapper.Map<SystemAlertDto>(signUpResult));
            }

            await _tokenService.GenerateAndSetRefreshTokenCookieAsync(Response, signUpResult.Data.UserId);
            await SignInMvcAsync(signUpResult.Data);

            return Ok();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Creates an MVC cookie authentication session for the specified user.
        /// </summary>
        private async Task SignInMvcAsync(UserForLoginDto user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new("FullName", user.FullName),
                new("UserName", user.UserName),
                new("SecurityStamp", user.SecurityStamp)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties
                {
                    IsPersistent = user.RememberMe ?? false,
                    ExpiresUtc = DateTime.UtcNow.AddDays(30)
                });
        }

        #endregion
    }
}