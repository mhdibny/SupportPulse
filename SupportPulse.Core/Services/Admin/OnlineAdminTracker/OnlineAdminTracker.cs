#region Usings

using System.Collections.Concurrent;

#endregion

namespace SupportPulse.Core.Services.Admin.OnlineAdminTracker
{
    /// <summary>
    /// Thread‑safe implementation of <see cref="IOnlineAdminTracker"/> using a
    /// <see cref="ConcurrentDictionary{TKey, TValue}"/> with connection counters.
    /// </summary>
    public class OnlineAdminTracker : IOnlineAdminTracker
    {
        #region Fields

        private readonly ConcurrentDictionary<int, int> _connections = new();

        #endregion

        #region Add / Remove

        /// <inheritdoc />
        public void AddConnection(int userId)
        {
            _connections.AddOrUpdate(userId, 1, (_, count) => count + 1);
        }

        /// <inheritdoc />
        public void RemoveConnection(int userId)
        {
            int newCount = _connections.AddOrUpdate(userId, 0, (_, count) => count - 1);
            if (newCount <= 0)
            {
                _connections.TryRemove(userId, out _);
            }
        }

        #endregion

        #region Query

        /// <inheritdoc />
        public IReadOnlyList<int> GetOnlineAdminIds()
        {
            return _connections.Keys.ToList();
        }

        #endregion
    }
}