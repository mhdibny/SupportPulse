#region Usings

using SupportPulse.Core.DTOs.Chat;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.DTOs.Message;

#endregion

namespace SupportPulse.Core.Services.Chats
{
    /// <summary>
    /// Defines operations for creating, retrieving, and ending support chats,
    /// as well as querying chat presence and lock status.
    /// </summary>
    public interface IChatService
    {
        #region Chat lifecycle

        /// <summary>
        /// Creates a new support chat and queues it for automatic assignment to an online admin.
        /// </summary>
        /// <param name="chat">The chat creation data.</param>
        Task<OperationResult<SuccessCreatedChatDto>> CreateChatAsync(CreateChatSupportDto chat);

        /// <summary>
        /// Ends a chat by its unique identifier, if the requesting user owns it.
        /// </summary>
        /// <param name="uniqChatId">The unique chat identifier.</param>
        /// <param name="userId">The requesting user identifier.</param>
        Task<OperationResult> EndChatAsync(string uniqChatId, int userId);

        #endregion

        #region User chats

        /// <summary>
        /// Returns all chats belonging to the specified user.
        /// </summary>
        Task<List<UserChatsDto>> GetUserChatsAsync(int userId);

        /// <summary>
        /// Retrieves a single chat's details for a user.
        /// </summary>
        Task<OperationResult<ChatDto>> GetUserChatAsync(string uniqChatId, int userId);

        /// <summary>
        /// Retrieves all messages of a chat for a user (by unique chat ID).
        /// </summary>
        Task<OperationResult<List<MessageDto>>> GetMessageOfChatAsync(string uniqChatId, int userId);

        /// <summary>
        /// Retrieves all messages of a chat for a user (by numeric chat ID).
        /// </summary>
        Task<OperationResult<List<MessageDto>>> GetMessageOfChatAsync(int chatId, int userId);

        /// <summary>
        /// Gets the numeric chat ID for the given unique chat identifier.
        /// </summary>
        Task<int> GetUserChatByUniqChatIdAsync(string uniqChatId);

        /// <summary>
        /// Checks whether a chat with the given unique ID belongs to the specified user.
        /// </summary>
        Task<bool> IsThisUserHaveThisChat(string chatUniqId, int userId);

        #endregion

        #region Chat identification

        /// <summary>
        /// Returns the unique string ID for the given numeric chat ID.
        /// </summary>
        Task<string> GetChatUniqIdByIdAsync(int chatId);

        /// <summary>
        /// Returns the numeric chat ID for the given unique string ID.
        /// </summary>
        Task<int> GetChatIdByUniqIdAsync(string uniqChatId);

        #endregion

        #region Lock status

        /// <summary>
        /// Checks whether the specified admin has currently locked the chat.
        /// </summary>
        Task<bool> IsChatLockedByAdminAsync(int chatId, int adminUserId);

        /// <summary>
        /// Checks whether a chat belongs to a user and is currently locked by an admin.
        /// </summary>
        Task<bool> IsChatBelongsToUserAndLockedAsync(string chatUniqId, int userId);

        /// <summary>
        /// Returns the ID of the admin who currently locked the chat, or 0 if not locked.
        /// </summary>
        Task<int> GetChatLockedAdminIdAsync(int chatId);

        /// <summary>
        /// Returns the creator user ID for the given chat.
        /// </summary>
        Task<int> GetChatCreatorIdAsync(int chatId);

        #endregion

        #region Presence

        /// <summary>
        /// Returns locked chats for a creator user, including the locking admin ID.
        /// </summary>
        Task<List<ChatPresenceInfoDto>> GetLockedChatsForUserPresenceAsync(int creatorUserId);

        /// <summary>
        /// Returns locked chats for an admin, including the creator user ID.
        /// </summary>
        Task<List<ChatPresenceInfoDto>> GetLockedChatsForAdminPresenceAsync(int adminUserId);

        /// <summary>
        /// Returns basic presence information for a chat.
        /// </summary>
        Task<ChatPresenceInfoDto?> GetChatPresenceInfoAsync(int chatId);

        #endregion
    }
}