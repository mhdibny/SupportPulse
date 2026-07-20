#region Usings

using SupportPulse.Core.DTOs.Admin.AutoLock;
using SupportPulse.Core.DTOs.Common;

#endregion

namespace SupportPulse.Core.Services.Admin.Assign
{
    /// <summary>
    /// Defines operations for automatic and manual chat assignment and lock management.
    /// </summary>
    public interface IAssignChatService
    {
        /// <summary>
        /// Automatically assigns an unlocked chat to the best available online admin,
        /// taking into account current load, performance, and idle time.
        /// </summary>
        /// <param name="command">The assignment command containing the chat and support category identifiers.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        Task AssignChatAsync(AssignChatDto command, CancellationToken cancellationToken);

        /// <summary>
        /// Allows an admin to manually lock a free chat for themselves.
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <param name="adminId">The identifier of the admin attempting to lock the chat.</param>
        /// <returns>An <see cref="OperationResult"/> describing success or failure.</returns>
        Task<OperationResult> ManualLockChatAsync(int chatId, int adminId);

        /// <summary>
        /// Allows an admin to manually unlock a chat that they currently hold locked.
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <param name="adminId">The identifier of the admin currently holding the lock.</param>
        /// <returns>An <see cref="OperationResult"/> describing success or failure.</returns>
        Task<OperationResult> ManualUnlockChatAsync(int chatId, int adminId);
    }
}