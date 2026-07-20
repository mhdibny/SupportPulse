namespace SupportPulse.Core.Services.PresenceTracker
{
    /// <summary>
    /// Tracks SignalR connections for all users (regular users and admins)
    /// and provides online status information.
    /// </summary>
    public interface IConnectionPresenceTracker
    {
        /// <summary>
        /// Increments the connection counter for the specified user.
        /// Returns <c>true</c> if this was the first connection.
        /// </summary>
        bool AddConnection(int userId);

        /// <summary>
        /// Decrements the connection counter for the specified user.
        /// Returns <c>true</c> if the last connection was removed.
        /// </summary>
        bool RemoveConnection(int userId);

        /// <summary>
        /// Checks whether the user has at least one active connection.
        /// </summary>
        bool HasConnection(int userId);

        /// <summary>
        /// Returns the set of user IDs who currently have at least one active connection.
        /// </summary>
        HashSet<int> GetOnlineUserIds();
    }
}