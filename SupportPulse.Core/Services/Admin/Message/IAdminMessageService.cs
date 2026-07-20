#region Usings

using SupportPulse.Core.DTOs.Admin.Message;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.DTOs.Message;

#endregion

namespace SupportPulse.Core.Services.Admin.Message
{
    /// <summary>
    /// Defines operations for sending messages from an admin to a user in a locked chat.
    /// </summary>
    public interface IAdminMessageService
    {
        /// <summary>
        /// Sends a plain‑text message to a user (called from the SignalR hub).
        /// </summary>
        /// <param name="message">The plain‑text message data.</param>
        /// <param name="adminId">The identifier of the admin sending the message.</param>
        /// <returns>A result containing the created message and the list of receivers.</returns>
        Task<OperationResult<SendMessageResultWithReceiversDto>> SendMessageToUserAsync(
            SendPlainTextMessageToUserDto message, int adminId);

        /// <summary>
        /// Sends a message with optional text and/or files to a user (called from the API).
        /// </summary>
        /// <param name="message">The message data including optional text and files.</param>
        /// <param name="adminId">The identifier of the admin sending the message.</param>
        /// <returns>A result containing the created message and the list of receivers.</returns>
        Task<OperationResult<SendMessageResultWithReceiversDto>> SendMessageToUserAsync(
            SendMessageToUserDto message, int adminId);
    }
}