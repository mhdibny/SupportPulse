#region Usings

using SupportPulse.Core.DTOs.User;

#endregion

namespace SupportPulse.Core.DTOs.Token
{
    /// <summary>
    /// Contains the result of a token renewal operation.
    /// </summary>
    public class TokenRenewalResultDto
    {
        /// <summary>
        /// Indicates whether the renewal was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// The newly issued access token (JWT), if successful.
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// The newly issued refresh token, if successful.
        /// </summary>
        public string? RefreshToken { get; set; }
    }

    /// <summary>
    /// Internal DTO used during token renewal to pass user data without anonymous types.
    /// </summary>
    internal sealed class TokenRenewalValidationDto
    {
        /// <summary>
        /// The user data extracted from the validated refresh token.
        /// </summary>
        public UserForLoginDto? User { get; set; }
    }
}