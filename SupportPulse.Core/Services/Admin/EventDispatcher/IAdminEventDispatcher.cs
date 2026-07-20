#region Usings

using SupportPulse.Core.DTOs.Admin.EventDispatcher;

#endregion

namespace SupportPulse.Core.Services.Admin.EventDispatcher
{
    /// <summary>
    /// Dispatches admin events for real‑time Data Sync and notification delivery.
    /// </summary>
    public interface IAdminEventDispatcher
    {
        /// <summary>
        /// Processes the given event context, builds notification and Data Sync messages,
        /// and delivers them to eligible online admins.
        /// </summary>
        /// <param name="context">The complete event context.</param>
        Task DispatchAsync(AdminEventContext context);
    }
}