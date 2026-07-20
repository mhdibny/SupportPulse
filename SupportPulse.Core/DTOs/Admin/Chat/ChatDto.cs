#region Usings

// No external usings required.

#endregion

namespace SupportPulse.Core.DTOs.Admin.Chat
{
    /// <summary>
    /// Represents a single chat in the admin chat list sidebar.
    /// </summary>
    public class AdminChatListDto
    {
        public int ChatId { get; set; }
        public string ChatUniqId { get; set; }
        public string Subject { get; set; } = null!;
        public string SupportCategoryName { get; set; } = null!;
        public int CreatorId { get; set; }
        public string CreatorFullName { get; set; } = null!;
        public DateTime CreatedTime { get; set; }
        public string? LastMessageText { get; set; }
        public DateTime? LastMessageDateTime { get; set; }
        public string SupportCategoryIconKey { get; set; } = null!;
        public string? SupportCategoryIconClass { get; set; }
        public bool IsChatLocked { get; set; }
    }

    /// <summary>
    /// Contains detailed information about a chat for the admin chat view.
    /// </summary>
    public class AdminChatDataDto
    {
        public int ChatId { get; set; }
        public string ChatUniqId { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string SupportCategoryName { get; set; } = null!;
        public DateTime CreatedTime { get; set; }
        public string SupportCategoryIconKey { get; set; } = null!;
        public string? SupportCategoryIconClass { get; set; }
        public bool IsChatLocked { get; set; }
        public int CreatorId { get; set; }
        public string CreatorUserName { get; set; } = null!;
        public string CreatorFirstName { get; set; } = null!;
        public string CreatorLastName { get; set; } = null!;
        public bool CreatorIsBanned { get; set; }
    }

    /// <summary>
    /// Internal DTO used when sending a message from an admin to obtain chat context.
    /// </summary>
    public class AdminMessageChatInfoDto
    {
        public int Id { get; set; }
        public int SupportCategoryId { get; set; }
        public string ChatUniqId { get; set; } = null!;
        public string AdminUserName { get; set; } = null!;
        public int CreatorId { get; set; }
    }

    /// <summary>
    /// Returned after an admin ends a chat, containing the creator ID and chat unique ID.
    /// </summary>
    public class ChatEndedDto
    {
        public int CreatorId { get; set; }
        public string ChatUniqId { get; set; }
    }

    /// <summary>
    /// Internal DTO that carries the minimal chat data needed for the end‑chat dispatch context.
    /// </summary>
    public class ChatEndContextDto
    {
        public string Subject { get; set; } = null!;
        public string ChatUniqId { get; set; } = null!;
        public int SupportCategoryId { get; set; }
        public int CreatorId { get; set; }
    }
}