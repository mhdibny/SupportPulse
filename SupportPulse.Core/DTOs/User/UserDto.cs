namespace SupportPulse.Core.DTOs.User
{
    /// <summary>
    /// Internal DTO used for login validation – holds the password hash temporarily.
    /// </summary>
    internal sealed record UserLoginValidationData(
        int Id,
        string UserName,
        string FullName,
        string SecurityStamp,
        string PasswordHash,
        bool IsBan,
        DateTime? BanExpiry
    );

    /// <summary>
    /// DTO returned after a successful login or token renewal, containing the essential
    /// user data needed to create a JWT and establish a session.
    /// </summary>
    public record UserForLoginDto(
        int UserId,
        string UserName,
        string FullName,
        string SecurityStamp,
        bool IsBan,
        DateTime? BanExpiry,
        bool? RememberMe = false
    );

    public class UserPanelInformationDto
    {
        public string UserName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
    }
}