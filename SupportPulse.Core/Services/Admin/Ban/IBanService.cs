#region Usings

using SupportPulse.Core.DTOs.Admin.Ban;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.Core.Services.Admin.Ban
{
    /// <summary>
    /// Defines ban management operations: viewing history, banning, unbanning,
    /// changing ban expiry, and system‑driven auto‑unban.
    /// </summary>
    public interface IBanService
    {
        /// <summary>
        /// Returns the ban history for the specified user, along with basic user information.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        Task<OperationResult<UserBanHistoryListDto>> GetUserBanHistoryListAsync(int userId);

        /// <summary>
        /// Applies a ban to a user and dispatches a <see cref="AdminEventType.UserBanned"/> event.
        /// </summary>
        /// <param name="ban">The ban details.</param>
        /// <param name="adminId">The identifier of the admin who is applying the ban.</param>
        Task<OperationResult> BanUserAsync(BanUserDto ban, int adminId);

        /// <summary>
        /// Lifts a ban from a user and dispatches a <see cref="AdminEventType.UserUnbanned"/> event.
        /// </summary>
        /// <param name="unBan">The unban details.</param>
        /// <param name="adminId">The identifier of the admin who is lifting the ban.</param>
        Task<OperationResult> UnBanUserAsync(UnBanUserDto unBan, int adminId);

        /// <summary>
        /// Changes the expiry of an existing ban and dispatches a <see cref="AdminEventType.UserBanExpiryChanged"/> event.
        /// </summary>
        /// <param name="changeBan">The new ban expiry information.</param>
        /// <param name="adminId">The identifier of the admin performing the change.</param>
        Task<OperationResult> ChangeUserBanExpiryAsync(ChangeBanExpiryTimeDto changeBan, int adminId);

        /// <summary>
        /// Automatically lifts a ban because its expiry has passed. No event is dispatched.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns><c>true</c> if the unban was successful; otherwise, <c>false</c>.</returns>
        Task<bool> UnBanUserBySystemAsync(int userId);
    }
}