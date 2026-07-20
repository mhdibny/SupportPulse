using Microsoft.AspNetCore.Http;
using SupportPulse.Core.DTOs.Token;
using SupportPulse.Core.DTOs.User;
using SupportPulse.Data.Entities.User.RefreshToken;

namespace SupportPulse.Core.Services.TokenService
{
    public interface ITokenService
    {
        /// <summary>
        /// تولید Access Token (JWT) با عمر کوتاه
        /// </summary>
        string GenerateAccessToken(UserForLoginDto user);

        /// <summary>
        /// تولید Refresh Token و ذخیره در دیتابیس
        /// </summary>
        Task<string> GenerateRefreshTokenAsync(int userId);

        /// <summary>
        /// تنظیم Refresh Token در کوکی HttpOnly
        /// </summary>
        Task<bool> SetRefreshTokenCookieAsync(HttpResponse response, string refreshToken);

        /// <summary>
        /// تولید Refresh Token و تنظیم همزمان در کوکی
        /// </summary>
        Task GenerateAndSetRefreshTokenCookieAsync(HttpResponse response, int userId);

        /// <summary>
        /// اعتبارسنجی Refresh Token از دیتابیس
        /// </summary>
        Task<RefreshToken?> ValidateAndGetRefreshTokenAsync(string token);

        /// <summary>
        /// باطل کردن یک Refresh Token
        /// </summary>
        Task RevokeRefreshTokenAsync(string token);

        /// <summary>
        /// باطل کردن تمام Refresh Token‌های یک کاربر
        /// </summary>
        Task RevokeAllRefreshTokensAsync(int userId);

        /// <summary>
        /// تمدید Access Token با استفاده از Refresh Token
        /// </summary>
        Task<TokenRenewalResultDto> RenewAccessTokenAsync(string? currentRefreshToken);
    }
}