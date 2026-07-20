#region Usings

using SupportPulse.Core.DTOs.Admin.Chat;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.DTOs.Message;
using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.Core.Services.Admin.Chat
{
    /// <summary>
    /// Defines operations for admin chat management: listing, viewing details,
    /// retrieving messages, and ending chats.
    /// </summary>
    public interface IAdminChatService
    {
        /// <summary>
        /// Returns the list of chats that the admin can see (locked by them or free).
        /// </summary>
        /// <param name="adminId">The admin user identifier.</param>
        Task<OperationResult<List<AdminChatListDto>>> GetChatListAsync(int adminId);

        /// <summary>
        /// Retrieves detailed information about a specific chat for an admin.
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <param name="adminId">The admin user identifier.</param>
        Task<OperationResult<AdminChatDataDto>> GetChatDataAsync(int chatId, int adminId);

        /// <summary>
        /// Retrieves the messages of a chat for an admin, provided the admin has access.
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <param name="adminId">The admin user identifier.</param>
        Task<OperationResult<List<MessageDto>>> GetMessageOfChatForAdminAsync(int chatId, int adminId);

        /// <summary>
        /// Ends a chat that is currently locked by the specified admin.
        /// Dispatches a <see cref="AdminEventType.ChatEnded"/> event.
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <param name="adminId">The admin user identifier.</param>
        Task<OperationResult<ChatEndedDto>> EndChatAsync(int chatId, int adminId);
    }
}