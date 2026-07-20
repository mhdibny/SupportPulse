#region Usings

using System.Threading.Channels;
using SupportPulse.Data.Context;
using SupportPulse.Data.Entities.User.Notification;

#endregion

namespace SupportPulse.App.BackgroundServices
{
    /// <summary>
    /// Background service that consumes admin notifications from a channel and persists them to the database.
    /// </summary>
    public class NotificationPersistenceService : BackgroundService
    {
        #region Constructor & Dependencies

        private readonly Channel<AdminNotification> _channel;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationPersistenceService> _logger;

        public NotificationPersistenceService(
            Channel<AdminNotification> channel,
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationPersistenceService> logger)
        {
            _channel = channel;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        #endregion

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationPersistenceService started.");
            await foreach (var notification in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.AdminNotifications.Add(notification);
                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error persisting notification for admin {AdminId}", notification.AdminUserId);
                }
            }
        }
    }
}