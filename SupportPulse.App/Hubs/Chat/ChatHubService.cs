#region Usings

using Microsoft.AspNetCore.SignalR;
using SupportPulse.Core.DTOs.Message;
using SupportPulse.Core.Services.Hubs.Base;
using SupportPulse.Core.Services.Hubs.Chats;

#endregion

namespace SupportPulse.App.Hubs.Chat
{
    /// <summary>
    /// Implements <see cref="IChatHubService"/> to broadcast new chat messages to connected users.
    /// </summary>
    public class ChatHubService : IChatHubService
    {
        #region Constructor & Dependencies

        private readonly IHubContext<ChatHub> _chatHubContext;
        private readonly IHubSystemMessage<ChatHub> _chatHubSystemMessage;

        public ChatHubService(
            IHubContext<ChatHub> chatHubContext,
            IHubSystemMessage<ChatHub> chatHubSystemMessage)
        {
            _chatHubContext = chatHubContext;
            _chatHubSystemMessage = chatHubSystemMessage;
        }

        #endregion

        #region Send Chat Message

        /// <summary>
        /// Sends a new message to a list of users via the Chat SignalR hub.
        /// </summary>
        /// <param name="userIds">The user identifiers that should receive the message.</param>
        /// <param name="message">The new message data to broadcast.</param>
        public async Task SendChatMessageToUsersAsync(List<string> userIds, NewMessageDto message)
        {
            await _chatHubContext.Clients.Users(userIds).SendAsync("ReceiveMessage", message);
        }

        #endregion
    }
}