namespace SupportPulse.Data.Enums.Admin
{
    /// <summary>
    /// Fine‑grained permissions for admin operations and notification subscriptions.
    /// </summary>
    public enum AdminPermission : int
    {
        // ========== General Permissions (1–99) ==========
        Dashboard = 1,
        RoleList = 2,
        AddRole = 3,
        EditRole = 4,
        DeleteRole = 5,
        UserList = 6,
        BanUser = 7,
        UnBanUser = 8,
        ChangeBanExpiry = 9,
        /// <summary>View ban history (GetUserBanHistories).</summary>
        ViewBanHistory = 10,

        SupportCategoryList = 11,
        AddSupportCategory = 12,
        EditSupportCategory = 13,

        /// <summary>View chat list (GetAdminChatList).</summary>
        ViewChatList = 14,
        /// <summary>View chat details (GetAdminChatData).</summary>
        ViewChatDetails = 15,
        /// <summary>Lock a chat (LockChat).</summary>
        LockChat = 16,
        /// <summary>Unlock a chat (UnLockChat).</summary>
        UnlockChat = 17,
        /// <summary>End a chat (EndChatByAdmin).</summary>
        EndChat = 18,
        /// <summary>Send a message as an admin in a chat.</summary>
        SendMessageInChat = 19,

        /// <summary>Assign or edit a user's roles.</summary>
        AssignRoleToUser = 20,
        /// <summary>Assign or edit a user's support categories.</summary>
        AssignSupportCategoryToUser = 21,

        // ========== Notification Permissions (101+) ==========
        ReceiveChatUnlockedNotification = 101,
        ReceiveChatLockedNotification = 102,
        ReceiveChatEndedNotification = 103,
        ReceiveUserBannedNotification = 104,
        ReceiveUserUnbannedNotification = 105,
        ReceiveUserBanExpiryChangedNotification = 106,
        ReceiveRoleCreatedNotification = 107,
        ReceiveRoleEditedNotification = 108,
        ReceiveRoleDeletedNotification = 109,
        ReceiveSupportCategoryCreatedNotification = 110,
        ReceiveSupportCategoryEditedNotification = 111,
        ReceiveChatAutoUnlockedNotification = 112,
        ReceiveUserRolesChangedNotification = 113,
        ReceiveUserSupportCategoriesChangedNotification = 114
    }
}