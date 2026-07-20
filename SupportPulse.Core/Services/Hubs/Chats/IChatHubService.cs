#region Usings

using SupportPulse.Core.DTOs.Message;

#endregion

namespace SupportPulse.Core.Services.Hubs.Chats
{
    /// <summary>
    /// Defines functionality to broadcast a new chat message to multiple users via SignalR.
    /// </summary>
    public interface IChatHubService
    {
        /// <summary>
        /// Sends a new chat message to the specified list of user IDs.
        /// </summary>
        /// <param name="userIds">The list of user identifiers to receive the message.</param>
        /// <param name="message">The new message data transfer object.</param>
        Task SendChatMessageToUsersAsync(List<string> userIds, NewMessageDto message);
    }
}