#region Usings

using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.DTOs.User;
using SupportPulse.Core.ViewModels.User;

#endregion

namespace SupportPulse.Core.Services.Users
{
    /// <summary>
    /// Defines operations for user authentication, registration, and security stamp validation.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Registers a new user and returns login-ready information.
        /// </summary>
        /// <param name="user">The sign‑up view model.</param>
        /// <returns>An <see cref="OperationResult{UserForLoginDto}"/> containing the user data on success.</returns>
        Task<OperationResult<UserForLoginDto>> SignUpUserAsync(SignUpUserVM user);

        /// <summary>
        /// Authenticates a user with the provided credentials.
        /// </summary>
        /// <param name="user">The login view model.</param>
        /// <returns>An <see cref="OperationResult{UserForLoginDto}"/> with the user data if login succeeds.</returns>
        Task<OperationResult<UserForLoginDto>> LoginUserAsync(LoginUserVM user);

        /// <summary>
        /// Returns the list of user IDs (as strings) that belong to the specified support category.
        /// </summary>
        /// <param name="supportCategoryId">The support category identifier.</param>
        Task<List<string>> GetSupportCategoryReceiverUserIdListAsync(int supportCategoryId);

        /// <summary>
        /// Returns the list of user IDs (as integers) that belong to the specified support category.
        /// </summary>
        /// <param name="supportCategoryId">The support category identifier.</param>
        Task<List<int>> GetSupportCategoryReceiverUserIdListAsIntAsync(int supportCategoryId);

        /// <summary>
        /// Checks whether the user is an admin (has at least one role).
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        Task<bool> IsAdminAsync(int userId);

        /// <summary>
        /// Retrieves login-ready user data by user ID.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        Task<UserForLoginDto?> GetUserForLoginByIdAsync(int userId);

        /// <summary>
        /// Generates a new security stamp for the user and persists it.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        Task UpdateSecurityStampAsync(int userId);

        /// <summary>
        /// Validates that the provided security stamp matches the one stored for the user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="stamp">The security stamp to validate.</param>
        /// <returns><c>true</c> if the stamp is valid; otherwise, <c>false</c>.</returns>
        Task<bool> IsSecurityStampValidAsync(int userId, string stamp);

        /// <summary>
        /// Retrieves panel information for the specified user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>
        /// A <see cref="UserPanelInformationDto"/> with the user's panel data, or <c>null</c> if the user is not found.
        /// </returns>
        Task<UserPanelInformationDto?> GetUserPanelInformationAsync(int userId);
    }
}