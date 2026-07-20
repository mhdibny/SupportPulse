#region Usings

using SupportPulse.Core.DTOs.Chat;
using SupportPulse.Core.DTOs.Message;
using SupportPulse.Core.DTOs.User;
using SupportPulse.Core.ViewModels.User;

#endregion

namespace SupportPulse.Core.ViewModels.Pages.Home
{
    /// <summary>
    /// View model for the user home page, containing support categories,
    /// the user's active chats, and the latest messages of the first chat.
    /// </summary>
    public class HomeVM
    {
        /// <summary>
        /// List of active support categories available for creating new chats.
        /// </summary>
        public List<SupportCategoryDto> SupportCategories { get; set; }

        /// <summary>
        /// Chats belonging to the current user.
        /// </summary>
        public List<UserChatsDto> UserChats { get; set; }

        /// <summary>
        /// Pre‑loaded latest messages for the first chat (if any).
        /// </summary>
        public List<MessageDto>? LatestChatMessages { get; set; }
    }
}