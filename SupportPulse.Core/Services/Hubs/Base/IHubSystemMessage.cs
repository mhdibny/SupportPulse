#region Usings

using SupportPulse.Core.DTOs.Common;

#endregion

namespace SupportPulse.Core.Services.Hubs.Base
{
    /// <summary>
    /// Provides methods to send system messages (alerts) to a specific user via a SignalR hub.
    /// </summary>
    /// <typeparam name="THub">The type of the SignalR hub.</typeparam>
    public interface IHubSystemMessage<THub>
    {
        /// <summary>
        /// Sends a system alert message to the specified user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="alert">The alert data to send.</param>
        Task SendSystemMessageToUserAsync(int userId, SystemAlertDto alert);

        /// <summary>
        /// Sends a success message to the specified user.
        /// </summary>
        Task SendSuccessToUserAsync(int userId, string message, string title = "Success");

        /// <summary>
        /// Sends an error message to the specified user.
        /// </summary>
        Task SendErrorToUserAsync(int userId, string message, string title = "Error");

        /// <summary>
        /// Sends a warning message to the specified user.
        /// </summary>
        Task SendWarningToUserAsync(int userId, string message, string title = "Warning");

        /// <summary>
        /// Sends a validation error message to the specified user.
        /// </summary>
        Task SendValidationErrorToUserAsync(int userId, string message, string title = "Validation Error");

        /// <summary>
        /// Sends an informational message to the specified user.
        /// </summary>
        Task SendInfoToUserAsync(int userId, string message, string title = "Information");
    }
}