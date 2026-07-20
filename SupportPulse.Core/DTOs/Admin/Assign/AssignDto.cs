namespace SupportPulse.Core.DTOs.Admin.Assign
{
    /// <summary>
    /// Holds key information about a chat for lock/unlock operations and event dispatching.
    /// </summary>
    public class ChatLockInfoDto
    {
        public int Id { get; set; }
        public int SupportCategoryId { get; set; }
        public bool IsEnded { get; set; }
        public int ChatStatusId { get; set; }
        public int? LockedByAdminId { get; set; }

        // Fields used for dispatching events
        public string Subject { get; set; } = null!;
        public string ChatUniqId { get; set; } = null!;
        public int CreatorId { get; set; }
    }
}