#region Usings

using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.Core.DTOs.Admin.EventDispatcher
{
    /// <summary>
    /// Carries all necessary data for generating a notification and Data Sync
    /// payload for a single admin event.
    /// </summary>
    public class AdminEventContext
    {
        /// <summary>The type of the admin event.</summary>
        public AdminEventType EventType { get; set; }

        /// <summary>ID of the admin who performed the action.</summary>
        public int ActorAdminId { get; set; }

        /// <summary>Full name of the acting admin.</summary>
        public string ActorFullName { get; set; } = null!;

        /// <summary>Username of the acting admin.</summary>
        public string ActorUserName { get; set; } = null!;

        /// <summary>For <see cref="AdminEventType.ChatAutoAssigned"/>, the admin who received the assignment.</summary>
        public int? AssignedAdminId { get; set; }

        // ----- Target (affected entity) -----

        /// <summary>ID of the target user (if applicable).</summary>
        public int? TargetUserId { get; set; }

        /// <summary>Username of the target user.</summary>
        public string? TargetUserName { get; set; }

        /// <summary>Full name of the target user.</summary>
        public string? TargetFullName { get; set; }

        // ----- Chat context -----

        /// <summary>ID of the chat (if applicable).</summary>
        public int? ChatId { get; set; }

        /// <summary>Subject of the chat.</summary>
        public string? ChatSubject { get; set; }

        /// <summary>Unique string identifier of the chat.</summary>
        public string? ChatUniqId { get; set; }

        // ----- Support category context -----

        /// <summary>ID of the support category (if applicable).</summary>
        public int? SupportCategoryId { get; set; }

        /// <summary>Name of the support category.</summary>
        public string? SupportCategoryName { get; set; }

        // ----- Role context -----

        /// <summary>Name of the role (if applicable).</summary>
        public string? RoleName { get; set; }

        /// <summary>ID of the role.</summary>
        public int? RoleId { get; set; }

        /// <summary>
        /// Optional payload delivered to clients for Data Sync purposes
        /// (e.g., a <see cref="Admin.User.UserListDto"/> or <see cref="Admin.SupportCategory.SupportCategoryListDto"/>).
        /// </summary>
        public object? DataSyncPayload { get; set; }
    }
}