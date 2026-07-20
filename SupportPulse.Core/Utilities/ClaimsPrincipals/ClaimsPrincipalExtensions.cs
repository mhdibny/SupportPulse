#region Usings

using System.Security.Claims;

#endregion

namespace SupportPulse.Core.Utilities.ClaimsPrincipals
{
    /// <summary>
    /// Provides extension methods for <see cref="ClaimsPrincipal"/>
    /// to extract commonly used claim values.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Retrieves the full name from the "FullName" claim.
        /// </summary>
        /// <param name="user">The <see cref="ClaimsPrincipal"/> instance.</param>
        /// <returns>The full name, or <c>null</c> if the claim is not present.</returns>
        public static string? GetFullName(this ClaimsPrincipal user)
        {
            return user.FindFirst("FullName")?.Value;
        }

        /// <summary>
        /// Retrieves the user identifier from the <see cref="ClaimTypes.NameIdentifier"/> claim.
        /// </summary>
        /// <param name="user">The <see cref="ClaimsPrincipal"/> instance.</param>
        /// <returns>The user identifier as a string, or <c>null</c> if the claim is not present.</returns>
        public static string? GetUserId(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Retrieves the username from the "UserName" claim.
        /// </summary>
        /// <param name="user">The <see cref="ClaimsPrincipal"/> instance.</param>
        /// <returns>The username, or <c>null</c> if the claim is not present.</returns>
        public static string? GetUserName(this ClaimsPrincipal user)
        {
            return user.FindFirst("UserName")?.Value;
        }

        /// <summary>
        /// Retrieves the user identifier as an integer.
        /// </summary>
        /// <param name="user">The <see cref="ClaimsPrincipal"/> instance.</param>
        /// <returns>The parsed user identifier.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="user"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ClaimTypes.NameIdentifier"/> claim is missing or not a valid integer.
        /// </exception>
        public static int GetUserIdAsInt(this ClaimsPrincipal user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(idClaim))
                throw new InvalidOperationException("User does not have a NameIdentifier claim.");

            if (!int.TryParse(idClaim, out var userId))
                throw new InvalidOperationException("UserId claim is not a valid integer.");

            return userId;
        }
    }
}