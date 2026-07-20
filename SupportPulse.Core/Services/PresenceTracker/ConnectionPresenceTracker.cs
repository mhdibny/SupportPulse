#region Usings

using System.Collections.Concurrent;

#endregion

namespace SupportPulse.Core.Services.PresenceTracker
{
    /// <summary>
    /// Thread‑safe implementation of <see cref="IConnectionPresenceTracker"/> using a
    /// <see cref="ConcurrentDictionary{TKey, TValue}"/>.
    /// </summary>
    public class ConnectionPresenceTracker : IConnectionPresenceTracker
    {
        #region Fields

        private readonly ConcurrentDictionary<int, int> _connections = new();

        #endregion

        #region Add / Remove

        /// <inheritdoc />
        public bool AddConnection(int userId)
        {
            int newCount = _connections.AddOrUpdate(userId, 1, (_, count) => count + 1);
            return newCount == 1;
        }

        /// <inheritdoc />
        public bool RemoveConnection(int userId)
        {
            int newCount = _connections.AddOrUpdate(userId, 0, (_, count) => count - 1);
            if (newCount <= 0)
            {
                _connections.TryRemove(userId, out _);
                return true;
            }
            return false;
        }

        #endregion

        #region Queries

        /// <inheritdoc />
        public bool HasConnection(int userId)
        {
            return _connections.TryGetValue(userId, out int count) && count > 0;
        }

        /// <inheritdoc />
        public HashSet<int> GetOnlineUserIds()
        {
            return new HashSet<int>(
                _connections.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key));
        }

        #endregion
    }
}