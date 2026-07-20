#region Usings

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using SupportPulse.Core.DTOs.Admin.Assign;
using SupportPulse.Core.DTOs.Admin.AutoLock;
using SupportPulse.Core.DTOs.Admin.EventDispatcher;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.Services.Admin.EventDispatcher;
using SupportPulse.Core.Services.Admin.OnlineAdminTracker;
using SupportPulse.Core.Services.Admin.Scoring;
using SupportPulse.Core.Services.Admin.Session;
using SupportPulse.Core.Services.Common;
using SupportPulse.Core.Settings;
using SupportPulse.Data.Context;
using SupportPulse.Data.Enums.Admin;
using SupportPulse.Data.Enums.Chat;

#endregion

namespace SupportPulse.Core.Services.Admin.Assign
{
    /// <summary>
    /// Implements automatic and manual chat assignment logic, including scoring,
    /// capacity checks, and event dispatching.
    /// </summary>
    public class AssignChatService : IAssignChatService
    {
        #region Constructor & Dependencies

        private readonly ApplicationDbContext _db;
        private readonly IScoringService _scoring;
        private readonly ILogger<AssignChatService> _logger;
        private readonly ChatAutoLockSettings _settings;
        private readonly IOperationResultAction _operation;
        private readonly IAdminEventDispatcher _eventDispatcher;
        private readonly ICurrentAdminSession _adminSession;
        private readonly IOnlineAdminTracker _onlineAdminTracker;

        // Prevents concurrent assignment for the same support category
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _categoryLocks = new();

        public AssignChatService(
            ApplicationDbContext db,
            IScoringService scoring,
            ILogger<AssignChatService> logger,
            ChatAutoLockSettings settings,
            IOperationResultAction operation,
            IAdminEventDispatcher eventDispatcher,
            ICurrentAdminSession adminSession,
            IOnlineAdminTracker onlineAdminTracker)
        {
            _db = db;
            _scoring = scoring;
            _logger = logger;
            _settings = settings;
            _operation = operation;
            _eventDispatcher = eventDispatcher;
            _adminSession = adminSession;
            _onlineAdminTracker = onlineAdminTracker;
        }

        #endregion

        #region Automatic Assignment

        /// <inheritdoc />
        public async Task AssignChatAsync(AssignChatDto command, CancellationToken ct)
        {
            var categoryLock = _categoryLocks.GetOrAdd(command.SupportCategoryId, _ => new SemaphoreSlim(1, 1));
            await categoryLock.WaitAsync(ct);
            try
            {
                _logger.LogDebug("Starting auto-assign for Chat {ChatId} (ExcludedAdmin={Excluded})",
                    command.ChatId, command.ExcludedAdminId);

                // 1) Get currently online admins from memory
                var onlineAdminIds = _onlineAdminTracker.GetOnlineAdminIds();
                if (onlineAdminIds.Count == 0)
                {
                    _logger.LogInformation("No online admins at all, chat {ChatId} remains open", command.ChatId);
                    return;
                }

                // 2) Filter admins eligible for this support category, excluding the specified one if needed
                var eligibleAdminsQuery = _db.UserSupportCategories
                    .Where(usc => usc.SupportCategoryId == command.SupportCategoryId
                                  && onlineAdminIds.Contains(usc.UserId));

                if (command.ExcludedAdminId.HasValue)
                    eligibleAdminsQuery = eligibleAdminsQuery.Where(usc => usc.UserId != command.ExcludedAdminId.Value);

                var eligibleAdmins = await eligibleAdminsQuery.Select(usc => usc.UserId).ToListAsync(ct);

                if (eligibleAdmins.Count == 0)
                {
                    _logger.LogInformation("No eligible admins for category {CatId} (after exclusion), chat stays open",
                        command.SupportCategoryId);
                    return;
                }

                // 3) Calculate current load and score for each eligible admin
                var adminsWithLoad = new List<(int AdminId, int ActiveChats, int EndedToday, double IdleMinutes)>();
                foreach (var adminId in eligibleAdmins)
                {
                    var activeChats = await _db.Chats
                        .CountAsync(c => c.LockedByAdminId == adminId
                                         && !c.IsEnded
                                         && c.ChatStatusId == (int)ChatStatusEnum.Responding, ct);

                    if (activeChats >= Math.Min(_settings.MaxActiveChatsPerAdmin, _settings.MaxTotalActiveChatsPerAdmin))
                        continue;

                    var endedToday = await _db.Chats
                        .CountAsync(c => c.LockedByAdminId == adminId && c.IsEnded && c.CreatedTime.Date == DateTime.Now.Date, ct);

                    var idleMinutes = await CalculateIdleMinutesAsync(adminId, ct);

                    adminsWithLoad.Add((adminId, activeChats, endedToday, idleMinutes));
                }

                if (adminsWithLoad.Count == 0)
                {
                    _logger.LogInformation("All eligible admins full for category {CatId}, chat {ChatId} remains open",
                        command.SupportCategoryId, command.ChatId);
                    return;
                }

                // Select the best candidate
                var bestAdmin = adminsWithLoad
                    .OrderByDescending(a => _scoring.CalculateScore(a.ActiveChats, a.EndedToday, a.IdleMinutes))
                    .ThenBy(_ => Guid.NewGuid())
                    .First();

                // 4) Lock the chat
                var chat = await _db.Chats.FirstOrDefaultAsync(c => c.Id == command.ChatId, ct);
                if (chat == null || chat.LockedByAdminId != null)
                {
                    _logger.LogWarning("Chat {ChatId} already locked or not found, abort assign", command.ChatId);
                    return;
                }

                chat.LockedByAdminId = bestAdmin.AdminId;
                chat.LockedAt = DateTime.Now;
                chat.ChatStatusId = (int)ChatStatusEnum.Responding;

                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Chat {ChatId} auto-assigned to admin {AdminId}", command.ChatId, bestAdmin.AdminId);

                // 5) Dispatch notifications and Data Sync
                var chatInfo = await _db.Chats
                    .Where(c => c.Id == command.ChatId)
                    .Select(c => new { c.Subject, c.ChatUniqId })
                    .FirstOrDefaultAsync(ct);

                if (chatInfo != null)
                {
                    var context = new AdminEventContext
                    {
                        EventType = AdminEventType.ChatAutoAssigned,
                        ActorAdminId = 0,                  // System
                        ActorFullName = "سیستم",
                        ActorUserName = "system",
                        AssignedAdminId = bestAdmin.AdminId,
                        ChatId = command.ChatId,
                        ChatSubject = chatInfo.Subject,
                        ChatUniqId = chatInfo.ChatUniqId,
                        SupportCategoryId = command.SupportCategoryId
                    };
                    await _eventDispatcher.DispatchAsync(context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during auto-assign of chat {ChatId}", command.ChatId);
            }
            finally
            {
                categoryLock.Release();
            }
        }

        #endregion

        #region Manual Lock

        /// <inheritdoc />
        public async Task<OperationResult> ManualLockChatAsync(int chatId, int adminId)
        {
            var chatInfo = await _db.Chats
                .Where(c => c.Id == chatId)
                .Select(c => new ChatLockInfoDto
                {
                    Id = c.Id,
                    SupportCategoryId = c.SupportCategoryId,
                    IsEnded = c.IsEnded,
                    ChatStatusId = c.ChatStatusId,
                    LockedByAdminId = c.LockedByAdminId,
                    Subject = c.Subject,
                    ChatUniqId = c.ChatUniqId,
                    CreatorId = c.CreatorId
                })
                .FirstOrDefaultAsync();

            if (chatInfo == null || chatInfo.IsEnded || chatInfo.ChatStatusId != (int)ChatStatusEnum.Responding)
            {
                return _operation.SendError("چت برای قفل کردن معتبر نیست.");
            }

            if (chatInfo.LockedByAdminId != null)
            {
                return _operation.SendError("این چت قبلا توسط ادمین دیگری قفل شده است.");
            }

            int supportCategoryId = chatInfo.SupportCategoryId;

            var categoryLock = _categoryLocks.GetOrAdd(supportCategoryId, _ => new SemaphoreSlim(1, 1));
            await categoryLock.WaitAsync();
            try
            {
                var activeChats = await _db.Chats
                    .CountAsync(c => c.LockedByAdminId == adminId
                                     && !c.IsEnded
                                     && c.ChatStatusId == (int)ChatStatusEnum.Responding);

                if (activeChats >= _settings.MaxTotalActiveChatsPerAdmin)
                {
                    _logger.LogWarning("Admin {AdminId} has reached total max active chats ({Max})",
                        adminId, _settings.MaxTotalActiveChatsPerAdmin);
                    return _operation.SendError(
                        $"شما به حداکثر چت های فعال رسیده اید ({_settings.MaxTotalActiveChatsPerAdmin} چت)، لطفا به یک چت پایان دهید، یا از مدیر درخواست افزایش تعداد چت فعال داشتهب اشید.");
                }

                var entity = await _db.Chats.FirstAsync(c => c.Id == chatId);
                if (entity.LockedByAdminId != null)
                {
                    return _operation.SendError("ای وای، این چت لحظه آخر توسط ادمین دیگری قفل شد.", OperationStatus.Info);
                }

                entity.LockedByAdminId = adminId;
                entity.LockedAt = DateTime.Now;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Admin {AdminId} manually locked chat {ChatId}", adminId, chatId);
            }
            finally
            {
                categoryLock.Release();
            }

            #region Dispatch

            var context = new AdminEventContext
            {
                EventType = AdminEventType.ChatLocked,
                ActorAdminId = adminId,
                ActorFullName = _adminSession.GetAdminFullName(),
                ActorUserName = _adminSession.GetAdminUserName(),
                ChatId = chatId,
                ChatSubject = chatInfo.Subject,
                ChatUniqId = chatInfo.ChatUniqId,
                SupportCategoryId = chatInfo.SupportCategoryId,
                DataSyncPayload = new { ChatId = chatId, IsChatLocked = true }
            };
            await _eventDispatcher.DispatchAsync(context);

            #endregion

            return _operation.SendSuccess("این چت با موفقیت برای شما قفل شد، ادمین های به این چت دیگر دسترسی ندارند.");
        }

        #endregion

        #region Manual Unlock

        /// <inheritdoc />
        public async Task<OperationResult> ManualUnlockChatAsync(int chatId, int adminId)
        {
            var chatInfo = await _db.Chats
                .Where(c => c.Id == chatId)
                .Select(c => new ChatLockInfoDto
                {
                    Id = c.Id,
                    SupportCategoryId = c.SupportCategoryId,
                    IsEnded = c.IsEnded,
                    ChatStatusId = c.ChatStatusId,
                    LockedByAdminId = c.LockedByAdminId,
                    Subject = c.Subject,
                    ChatUniqId = c.ChatUniqId,
                    CreatorId = c.CreatorId
                })
                .FirstOrDefaultAsync();

            if (chatInfo == null || chatInfo.IsEnded || chatInfo.ChatStatusId != (int)ChatStatusEnum.Responding)
            {
                return _operation.SendError("چت برای قفل کردن معتبر نیست.");
            }

            if (chatInfo.LockedByAdminId != adminId)
            {
                return _operation.SendError("شما مالک این چت نیستید.");
            }

            int supportCategoryId = chatInfo.SupportCategoryId;

            var categoryLock = _categoryLocks.GetOrAdd(supportCategoryId, _ => new SemaphoreSlim(1, 1));
            await categoryLock.WaitAsync();
            try
            {
                var entity = await _db.Chats.FirstAsync(c => c.Id == chatId);
                if (entity.LockedByAdminId != adminId)
                {
                    return _operation.SendError("وضعیت چت تغییر کرده!", OperationStatus.Info);
                }

                entity.LockedByAdminId = null;
                entity.LockedAt = null;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Admin {AdminId} manually unlocked chat {ChatId}", adminId, chatId);
            }
            finally
            {
                categoryLock.Release();
            }

            #region Dispatch

            var context = new AdminEventContext
            {
                EventType = AdminEventType.ChatUnlocked,
                ActorAdminId = adminId,
                ActorFullName = _adminSession.GetAdminFullName(),
                ActorUserName = _adminSession.GetAdminUserName(),
                ChatId = chatId,
                ChatSubject = chatInfo.Subject,
                ChatUniqId = chatInfo.ChatUniqId,
                SupportCategoryId = chatInfo.SupportCategoryId,
                DataSyncPayload = new { ChatId = chatId, IsChatLocked = false }
            };
            await _eventDispatcher.DispatchAsync(context);

            #endregion

            return _operation.SendSuccess(
                "چت با موفقیت آزاد شد، اکنون، ادمین های دیگر میتوانند روی این چت قفل کنند.");
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Calculates the number of minutes since the last user message in any chat locked by the given admin.
        /// </summary>
        private async Task<double> CalculateIdleMinutesAsync(int adminId, CancellationToken ct)
        {
            var latestUserMessages = await _db.Messages
                .Where(m => m.Chat!.LockedByAdminId == adminId && !m.Chat.IsEnded && m.SenderId == m.Chat.CreatorId)
                .GroupBy(m => m.ChatId)
                .Select(g => g.Max(m => m.Time))
                .ToListAsync(ct);

            if (latestUserMessages.Count == 0)
                return 60.0;

            var maxDate = latestUserMessages.Max();
            return (DateTime.Now - maxDate).TotalMinutes;
        }

        #endregion
    }
}