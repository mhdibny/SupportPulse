namespace SupportPulse.Data.Enums.Admin
{
    /// <summary>
    /// Defines the types of admin events that can generate Data Sync and/or notifications.
    /// </summary>
    public enum AdminEventType
    {
        ChatUnlocked,
        ChatLocked,
        ChatEnded,
        ChatEndedByUser,

        UserBanned,
        UserUnbanned,
        UserBanExpiryChanged,

        RoleCreated,
        RoleEdited,
        RoleDeleted,

        SupportCategoryCreated,
        SupportCategoryEdited,

        ChatAutoAssigned,
        ChatAutoUnlocked,

        UserRolesChanged,
        UserSupportCategoriesChanged
    }
}