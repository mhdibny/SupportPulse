namespace SupportPulse.Core.DTOs.Admin.AutoLock
{
    /// <summary>
    /// Command object for queuing a chat for automatic assignment (or re‑assignment after auto‑unlock).
    /// </summary>
    public class AssignChatDto
    {
        /// <summary>The chat to assign.</summary>
        public int ChatId { get; set; }

        /// <summary>The support category the chat belongs to.</summary>
        public int SupportCategoryId { get; set; }

        /// <summary>
        /// An admin to exclude from assignment (e.g., the previous lock holder after an auto‑unlock).
        /// </summary>
        public int? ExcludedAdminId { get; set; }
    }
}