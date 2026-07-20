namespace SupportPulse.Core.DTOs.Admin.Notification
{
    /// <summary>
    /// Represents the admin who performed the action that triggered a notification.
    /// </summary>
    public class ActorDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string UserName { get; set; } = null!;
    }

    /// <summary>
    /// Contains the display information (title, message, color, icon) for a single notification.
    /// </summary>
    public class NotificationInfoDto
    {
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Color { get; set; } = null!;
        public string Icon { get; set; } = null!;
    }

    /// <summary>
    /// Describes the target of an admin event (User, Chat, Role, or SupportCategory).
    /// </summary>
    public class TargetDto
    {
        /// <summary>Type of the target (e.g., "User", "Chat", "Role", "SupportCategory").</summary>
        public string Type { get; set; } = null!;

        public int? Id { get; set; }
        public string? Name { get; set; }

        /// <summary>Unique string identifier (used for chats and users).</summary>
        public string? UniqId { get; set; }
    }

    /// <summary>
    /// Complete notification object sent to admin clients for display and history.
    /// </summary>
    public class AdminNotificationDto
    {
        /// <summary>The event type (e.g., "UserBanned").</summary>
        public string Type { get; set; } = null!;

        /// <summary>Short title for the notification toast.</summary>
        public string Title { get; set; } = null!;

        /// <summary>Formatted message with embedded links.</summary>
        public string Message { get; set; } = null!;

        /// <summary>The admin who performed the action.</summary>
        public ActorDto Actor { get; set; } = null!;

        /// <summary>The entity that was affected (optional).</summary>
        public TargetDto? Target { get; set; }

        /// <summary>Hex color for the notification accent.</summary>
        public string Color { get; set; } = "#6366f1";

        /// <summary>Font‑Awesome icon class.</summary>
        public string Icon { get; set; } = "fa-bell";

        /// <summary>UTC timestamp when the notification was created.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}