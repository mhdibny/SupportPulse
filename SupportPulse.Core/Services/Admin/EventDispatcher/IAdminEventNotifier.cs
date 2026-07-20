namespace SupportPulse.Core.Services.Admin.EventDispatcher
{
    /// <summary>
    /// Abstracts the delivery of real‑time messages (Data Sync and notifications) to admin users.
    /// </summary>
    public interface IAdminEventNotifier
    {
        /// <summary>
        /// Sends a Data Sync payload to a set of users.
        /// </summary>
        /// <typeparam name="TData">The payload type.</typeparam>
        /// <param name="methodName">The client‑side method name (e.g., "UserBanned").</param>
        /// <param name="payload">The data payload.</param>
        /// <param name="userIds">The target user identifiers.</param>
        Task SendDataSyncAsync<TData>(string methodName, TData payload, IEnumerable<int> userIds);

        /// <summary>
        /// Sends a notification message to a set of users.
        /// </summary>
        /// <param name="notificationDto">The notification data transfer object.</param>
        /// <param name="userIds">The target user identifiers.</param>
        Task SendNotificationAsync(object notificationDto, IEnumerable<int> userIds);
    }
}