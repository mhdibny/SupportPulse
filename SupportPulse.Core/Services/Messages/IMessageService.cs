#region Usings

using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.DTOs.Message;

#endregion

namespace SupportPulse.Core.Services.Messages
{
    /// <summary>
    /// Defines operations for sending messages from regular users to the support team.
    /// </summary>
    public interface IMessageService
    {
        /// <summary>
        /// Sends a plain-text message to support.
        /// </summary>
        /// <param name="message">The plain‑text message data.</param>
        /// <param name="senderId">The identifier of the sending user.</param>
        /// <returns>A result containing the created message and the list of receivers.</returns>
        Task<OperationResult<SendMessageResultWithReceiversDto>> SendMessageByUserAsync(
            SendPlainTextMessageToSupportDto message, int senderId);

        /// <summary>
        /// Sends a message with optional text and/or files to support.
        /// </summary>
        /// <param name="message">The message data including optional text and files.</param>
        /// <param name="senderId">The identifier of the sending user.</param>
        /// <returns>A result containing the created message and the list of receivers.</returns>
        Task<OperationResult<SendMessageResultWithReceiversDto>> SendMessageByUserAsync(
            SendMessageToSupportDto message, int senderId);

        /// <summary>
        /// Marks all unseen messages in the specified chat (identified by its unique string)
        /// that were <b>not</b> sent by <paramref name="userId"/> as seen.
        /// Returns the identifier of the other party (the creator or the locking admin)
        /// that should be notified, or <c>null</c> if no notification is necessary.
        /// </summary>
        /// <param name="chatUniqId">The unique public identifier of the chat.</param>
        /// <param name="userId">The user performing the action (creator or locking admin).</param>
        /// <returns>The ID of the opposite party, or <c>null</c>.</returns>
        Task<int?> MarkMessagesAsSeenAsync(string chatUniqId, int userId);
    }
}