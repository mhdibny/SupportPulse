#region Usings

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Threading.Channels;
using SupportPulse.Core.DTOs.Admin.AutoLock;
using SupportPulse.Core.DTOs.Admin.EventDispatcher;
using SupportPulse.Core.Services.Admin.EventDispatcher;
using SupportPulse.Core.Settings;
using SupportPulse.Data.Context;
using SupportPulse.Data.Enums.Admin;
using SupportPulse.Data.Enums.Chat;

#endregion

namespace SupportPulse.App.BackgroundServices
{
    /// <summary>
    /// Background service that periodically checks for locked chats where the admin is inactive,
    /// automatically unlocks them, and re‑queues them for assignment.
    /// </summary>
    public class AutoUnlockBackgroundService : BackgroundService
    {
        #region Constructor & Dependencies

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoUnlockBackgroundService> _logger;
        private readonly ChatAutoLockSettings _settings;
        private readonly Channel<AssignChatDto> _assignChannel;

        public AutoUnlockBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<AutoUnlockBackgroundService> logger,
            IOptions<ChatAutoLockSettings> settings,
            Channel<AssignChatDto> assignChannel)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _settings = settings.Value;
            _assignChannel = assignChannel;
        }

        #endregion

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoUnlockService started.");
            ColorLog(ConsoleColor.Green, "AutoUnlock Service started. Will check for inactive chats every " +
                $"{_settings.UnlockCheckIntervalMinutes} minute(s).");

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.UnlockCheckIntervalMinutes));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    ColorLog(ConsoleColor.Cyan, "[AutoUnlock] Running unlock check cycle...");
                    _logger.LogDebug("AutoUnlock cycle started at {Time}", DateTime.Now);

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var timeout = DateTime.Now.AddMinutes(-_settings.AutoUnlockTimeoutMinutes);

                    // Find chats that are locked, not ended, and have an unseen user message older than the timeout
                    var unlockCandidates = await db.Chats
                        .Where(c => c.LockedByAdminId != null && !c.IsEnded && c.ChatStatusId == (int)ChatStatusEnum.Responding)
                        .Where(c =>
                            // Case 1: last unseen user message is older than the timeout
                            c.Messages!
                                .Where(m => m.SenderId == c.CreatorId && !m.IsSeen)
                                .OrderByDescending(m => m.Time)
                                .Select(m => (DateTime?)m.Time)
                                .FirstOrDefault() < timeout
                            ||
                            // Case 2: user never sent a message and the lock time is older than the timeout
                            (!c.Messages!.Any(m => m.SenderId == c.CreatorId) && c.LockedAt < timeout))
                        .Select(c => new { c.Id, c.SupportCategoryId, c.Subject })
                        .ToListAsync(stoppingToken);

                    ColorLog(ConsoleColor.Yellow, $"[AutoUnlock] Found {unlockCandidates.Count} candidate(s) for unlock.");

                    foreach (var chat in unlockCandidates)
                    {
                        _logger.LogInformation("Unlocking chat {ChatId} due to admin inactivity (last user message older than {Timeout} min)",
                            chat.Id, _settings.AutoUnlockTimeoutMinutes);

                        ColorLog(ConsoleColor.Magenta, $"[AutoUnlock] Unlocking chat {chat.Id} '{chat.Subject}'...");

                        var entity = await db.Chats.FindAsync(new object[] { chat.Id }, stoppingToken);
                        if (entity == null) continue;

                        var previousAdminId = entity.LockedByAdminId;

                        entity.LockedByAdminId = null;
                        entity.LockedAt = null;
                        await db.SaveChangesAsync(stoppingToken);

                        // Dispatch auto‑unlock event
                        // (AdminEventDispatcher now maps ChatAutoUnlocked to DataSync "ChatUnlocked" for all admins with ViewChatList,
                        //  and notification to those with permission 112.)
                        var chatInfo = await db.Chats
                            .Where(c => c.Id == chat.Id)
                            .Select(c => new { c.Subject, c.ChatUniqId })
                            .FirstOrDefaultAsync(stoppingToken);

                        if (chatInfo != null)
                        {
                            var context = new AdminEventContext
                            {
                                EventType = AdminEventType.ChatAutoUnlocked,
                                ActorAdminId = 0,
                                ActorFullName = "سیستم",
                                ActorUserName = "system",
                                ChatId = chat.Id,
                                ChatSubject = chatInfo.Subject,
                                ChatUniqId = chatInfo.ChatUniqId,
                                SupportCategoryId = chat.SupportCategoryId,
                                DataSyncPayload = new { ChatId = chat.Id, IsChatLocked = false }
                            };

                            var eventDispatcher = scope.ServiceProvider.GetRequiredService<IAdminEventDispatcher>();
                            await eventDispatcher.DispatchAsync(context);
                        }

                        // Re‑queue for assignment, excluding the previous admin
                        await _assignChannel.Writer.WriteAsync(new AssignChatDto
                        {
                            ChatId = chat.Id,
                            SupportCategoryId = chat.SupportCategoryId,
                            ExcludedAdminId = previousAdminId
                        }, stoppingToken);

                        ColorLog(ConsoleColor.Green, $"[AutoUnlock] Chat {chat.Id} unlocked and re‑queued for assignment.");
                    }

                    if (unlockCandidates.Count == 0)
                    {
                        ColorLog(ConsoleColor.Gray, "[AutoUnlock] No chats needed to be unlocked at this cycle.");
                    }

                    ColorLog(ConsoleColor.Cyan, "[AutoUnlock] Cycle finished.\n");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    ColorLog(ConsoleColor.Yellow, "[AutoUnlock] Service cancellation requested. Exiting gracefully.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AutoUnlock cycle");
                    ColorLog(ConsoleColor.Red, $"[AutoUnlock] Error: {ex.Message}");
                }
            }
        }

        #region Helpers

        /// <summary>
        /// Writes a colored log message to the console.
        /// </summary>
        private static void ColorLog(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {message}");
            Console.ResetColor();
        }

        #endregion
    }
}