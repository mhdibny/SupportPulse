#region Usings

using Microsoft.AspNetCore.SignalR;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.Services.Hubs.Base;

#endregion

namespace SupportPulse.App.Hubs.Base
{
    /// <summary>
    /// A generic implementation of <see cref="IHubSystemMessage{THub}"/> that sends system alert messages
    /// to individual users through a SignalR hub context.
    /// </summary>
    /// <typeparam name="THub">The type of the SignalR hub.</typeparam>
    public class GenericHubSystemSender<THub> : IHubSystemMessage<THub> where THub : Hub
    {
        #region Constructor & Dependencies

        private readonly IHubContext<THub> _hubContext;

        public GenericHubSystemSender(IHubContext<THub> hubContext)
        {
            _hubContext = hubContext;
        }

        #endregion

        #region System Messages

        /// <inheritdoc />
        public virtual async Task SendSystemMessageToUserAsync(int userId, SystemAlertDto alert)
        {
            await _hubContext.Clients.User(userId.ToString()).SendAsync("SystemMessage", alert);
        }

        /// <inheritdoc />
        public virtual async Task SendSuccessToUserAsync(int userId, string message, string title = "موفقیت")
        {
            await SendSystemMessageToUserAsync(userId,
                new SystemAlertDto { Message = message, Title = title, Type = "success" });
        }

        /// <inheritdoc />
        public virtual async Task SendErrorToUserAsync(int userId, string message, string title = "خطا")
        {
            await SendSystemMessageToUserAsync(userId,
                new SystemAlertDto { Message = message, Title = title, Type = "error" });
        }

        /// <inheritdoc />
        public virtual async Task SendWarningToUserAsync(int userId, string message, string title = "هشدار")
        {
            await SendSystemMessageToUserAsync(userId,
                new SystemAlertDto { Message = message, Title = title, Type = "warning" });
        }

        /// <inheritdoc />
        public virtual async Task SendValidationErrorToUserAsync(int userId, string message, string title = "خطای اعتبار سنجی")
        {
            await SendSystemMessageToUserAsync(userId,
                new SystemAlertDto { Message = message, Title = title, Type = "warning" });
        }

        /// <inheritdoc />
        public virtual async Task SendInfoToUserAsync(int userId, string message, string title = "اطلاعات")
        {
            await SendSystemMessageToUserAsync(userId,
                new SystemAlertDto { Message = message, Title = title, Type = "info" });
        }

        #endregion
    }
}