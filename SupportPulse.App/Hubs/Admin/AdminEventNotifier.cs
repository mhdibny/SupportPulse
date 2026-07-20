#region Usings

using Microsoft.AspNetCore.SignalR;
using SupportPulse.Core.Services.Admin.EventDispatcher;

#endregion

namespace SupportPulse.App.Hubs.Admin
{
    /// <summary>
    /// Implementation of <see cref="IAdminEventNotifier"/> using the <see cref="AdminHub"/> SignalR hub.
    /// </summary>
    public class AdminEventNotifier : IAdminEventNotifier
    {
        private readonly IHubContext<AdminHub> _hubContext;

        #region Constructor & Dependencies

        public AdminEventNotifier(IHubContext<AdminHub> hubContext)
        {
            _hubContext = hubContext;
        }

        #endregion

        /// <inheritdoc />
        public async Task SendDataSyncAsync<TData>(string methodName, TData payload, IEnumerable<int> userIds)
        {
            var userIdStrings = userIds.Select(id => id.ToString()).ToList();
            await _hubContext.Clients.Users(userIdStrings).SendAsync(methodName, payload);
        }

        /// <inheritdoc />
        public async Task SendNotificationAsync(object notificationDto, IEnumerable<int> userIds)
        {
            var userIdStrings = userIds.Select(id => id.ToString()).ToList();
            await _hubContext.Clients.Users(userIdStrings).SendAsync("ReceiveNotification", notificationDto);
        }
    }
}