#region Usings

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SupportPulse.Core.DTOs.Token;
using SupportPulse.Core.DTOs.User;
using SupportPulse.Core.Security.Password;
using SupportPulse.Core.Services.Admin.Ban;
using SupportPulse.Data.Context;
using SupportPulse.Data.Entities.User.RefreshToken;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

#endregion

namespace SupportPulse.Core.Services.TokenService
{
    /// <summary>
    /// Manages JWT access tokens, refresh tokens, and their lifecycle.
    /// </summary>
    public class TokenService : ITokenService
    {
        #region Constructor & Dependencies

        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IBanService _banService;

        public TokenService(
            IConfiguration configuration,
            ApplicationDbContext db,
            IHttpContextAccessor httpContextAccessor,
            IBanService banService)
        {
            _configuration = configuration;
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _banService = banService;
        }

        #endregion

        #region Access Token Generation

        /// <inheritdoc />
        public string GenerateAccessToken(UserForLoginDto user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new("FullName", user.FullName),
                new("UserName", user.UserName),
                new("SecurityStamp", user.SecurityStamp),
                new("Nonce", SecurityTool.GenerateNonce())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        #endregion

        #region Refresh Token Management

        /// <inheritdoc />
        public async Task<string> GenerateRefreshTokenAsync(int userId)
        {
            var token = SecurityTool.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                Token = token,
                UserId = userId,
                Expires = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();

            return token;
        }

        /// <inheritdoc />
        public async Task<bool> SetRefreshTokenCookieAsync(HttpResponse response, string refreshToken)
        {
            try
            {
                response.Cookies.Append("X-Refresh-Token", refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddDays(7)
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public async Task GenerateAndSetRefreshTokenCookieAsync(HttpResponse response, int userId)
        {
            var refreshToken = await GenerateRefreshTokenAsync(userId);
            await SetRefreshTokenCookieAsync(response, refreshToken);
        }

        #endregion

        #region Token Validation & Revocation

        /// <inheritdoc />
        public async Task<RefreshToken?> ValidateAndGetRefreshTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            return await _db.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsRevoked && t.Expires > DateTime.UtcNow);
        }

        /// <inheritdoc />
        public async Task RevokeRefreshTokenAsync(string token)
        {
            var storedToken = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (storedToken != null)
            {
                storedToken.IsRevoked = true;
                await _db.SaveChangesAsync();
            }
        }

        /// <inheritdoc />
        public async Task RevokeAllRefreshTokensAsync(int userId)
        {
            var tokens = await _db.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }

            await _db.SaveChangesAsync();
        }

        #endregion

        #region Token Renewal

        /// <inheritdoc />
        public async Task<TokenRenewalResultDto> RenewAccessTokenAsync(string? currentRefreshToken)
        {
            var result = new TokenRenewalResultDto();

            if (string.IsNullOrWhiteSpace(currentRefreshToken))
                return result;

            // Validate the refresh token and fetch the associated user in one query
            var tokenInfo = await _db.RefreshTokens
                .Where(t => t.Token == currentRefreshToken
                            && !t.IsRevoked
                            && t.Expires > DateTime.UtcNow)
                .Select(t => new TokenRenewalValidationDto
                {
                    User = _db.Users
                        .Where(u => u.Id == t.UserId)
                        .Select(u => new UserForLoginDto(
                            u.Id,
                            u.UserName,
                            u.FirstName + " " + u.LastName,
                            u.SecurityStamp,
                            u.IsBan,
                            u.BanExpiry,
                            false))
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (tokenInfo == null || tokenInfo.User == null)
                return result;

            var user = tokenInfo.User;

            // Handle ban expiry – lift it automatically if the ban has ended
            if (user.IsBan)
            {
                if (user.BanExpiry.HasValue && user.BanExpiry <= DateTime.Now)
                {
                    bool unBanUserResult = await _banService.UnBanUserBySystemAsync(user.UserId);
                    if (!unBanUserResult)
                        return result;
                }
                else
                {
                    // Permanent or still‑active temporary ban
                    return result;
                }
            }

            // Revoke the old token and issue a new one within the same SaveChanges
            var oldToken = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == currentRefreshToken);
            if (oldToken != null)
                oldToken.IsRevoked = true;

            var newRefreshToken = new RefreshToken
            {
                Token = SecurityTool.GenerateRefreshToken(),
                UserId = user.UserId,
                Expires = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            _db.RefreshTokens.Add(newRefreshToken);
            await _db.SaveChangesAsync();

            var accessToken = GenerateAccessToken(user);

            result.IsSuccess = true;
            result.AccessToken = accessToken;
            result.RefreshToken = newRefreshToken.Token;

            return result;
        }

        #endregion
    }
}