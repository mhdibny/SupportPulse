namespace SupportPulse.Core.Services.Admin.OnlineAdminTracker
{
    /// <summary>
    /// Tracks online admin users based on SignalR connections.
    /// Used for automatic chat assignment (<c>AssignChatService</c>).
    /// </summary>
    public interface IOnlineAdminTracker
    {
        /// <summary>
        /// Registers a new connection for the specified admin.
        /// </summary>
        /// <param name="userId">The admin user identifier.</param>
        void AddConnection(int userId);

        /// <summary>
        /// Removes a connection for the specified admin.
        /// </summary>
        /// <param name="userId">The admin user identifier.</param>
        void RemoveConnection(int userId);

        /// <summary>
        /// Returns the identifiers of all admins that currently have at least one active connection.
        /// </summary>
        IReadOnlyList<int> GetOnlineAdminIds();
    }
}