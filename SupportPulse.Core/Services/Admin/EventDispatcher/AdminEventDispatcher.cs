#region Usings

using Microsoft.Extensions.Logging;
using SupportPulse.Core.DTOs.Admin.EventDispatcher;
using SupportPulse.Core.DTOs.Admin.Notification;
using SupportPulse.Core.Services.Admin.PermissionCache;
using SupportPulse.Core.Services.PresenceTracker;
using SupportPulse.Data.Entities.User.Notification;
using SupportPulse.Data.Enums.Admin;
using System.Threading.Channels;

#endregion

namespace SupportPulse.Core.Services.Admin.EventDispatcher
{
    /// <summary>
    /// Implements admin event dispatching, delivering targeted Data Sync and notification messages
    /// to online admins based on fine‑grained permissions and support category membership.
    /// </summary>
    public class AdminEventDispatcher : IAdminEventDispatcher
    {
        #region Dependencies

        private readonly IAdminPermissionCacheService _authCache;
        private readonly IConnectionPresenceTracker _presence;
        private readonly IAdminEventNotifier _notifier;
        private readonly Channel<AdminNotification> _notificationChannel;
        private readonly ILogger<AdminEventDispatcher> _logger;

        #endregion

        #region Event Permission Map

        /// <summary>
        /// Maps each <see cref="AdminEventType"/> to its Data Sync permission (nullable)
        /// and Notification permission (nullable).
        /// </summary>
        private static readonly Dictionary<AdminEventType, (int? DataPermission, int? NotifPermission)> EventMap = new()
        {
            { AdminEventType.ChatUnlocked,                   (null, 101) },
            { AdminEventType.ChatLocked,                     (null, 102) },
            { AdminEventType.ChatEnded,                      (null, 103) },
            { AdminEventType.ChatEndedByUser,                (14,   null) },
            { AdminEventType.UserBanned,                     (6,    104) },
            { AdminEventType.UserUnbanned,                   (6,    105) },
            { AdminEventType.UserBanExpiryChanged,           (6,    106) },
            { AdminEventType.RoleCreated,                    (2,    107) },
            { AdminEventType.RoleEdited,                     (2,    108) },
            { AdminEventType.RoleDeleted,                    (2,    109) },
            { AdminEventType.SupportCategoryCreated,         (11,   110) },
            { AdminEventType.SupportCategoryEdited,          (11,   111) },
            // Automatic events
            { AdminEventType.ChatAutoAssigned,               (null, null) },
            { AdminEventType.ChatAutoUnlocked,               (null, 112)  },
            { AdminEventType.UserRolesChanged,               (6,    113) },
            { AdminEventType.UserSupportCategoriesChanged,   (13,   114) }
        };

        #endregion

        #region Constructor

        public AdminEventDispatcher(
            IAdminPermissionCacheService authCache,
            IConnectionPresenceTracker presence,
            IAdminEventNotifier notifier,
            Channel<AdminNotification> notificationChannel,
            ILogger<AdminEventDispatcher> logger)
        {
            _authCache = authCache;
            _presence = presence;
            _notifier = notifier;
            _notificationChannel = notificationChannel;
            _logger = logger;
        }

        #endregion

        #region DispatchAsync (main entry point)

        /// <inheritdoc />
        public async Task DispatchAsync(AdminEventContext context)
        {
            if (!EventMap.TryGetValue(context.EventType, out var perms))
                return;

            // Special path: automatic chat assignment
            if (context.EventType == AdminEventType.ChatAutoAssigned)
            {
                await HandleAutoAssignedAsync(context);
                return;
            }

            // For support category creation/editing, skip the membership filter
            // (a newly created category has no members yet).
            bool skipSupportCategoryFilter =
                context.EventType == AdminEventType.SupportCategoryCreated ||
                context.EventType == AdminEventType.SupportCategoryEdited;

            // ---- 1. Build Actor ----
            var actor = new ActorDto
            {
                Id = context.ActorAdminId,
                FullName = context.ActorFullName,
                UserName = context.ActorUserName
            };

            // ---- 2. Build Target ----
            TargetDto? target = BuildTargetDto(context);

            // ---- 3. Build notification info ----
            NotificationInfoDto notifInfo = BuildNotificationInfo(context, actor, target);

            var notifDto = new AdminNotificationDto
            {
                Type = context.EventType.ToString(),
                Title = notifInfo.Title,
                Message = notifInfo.Message,
                Actor = actor,
                Target = target,
                Color = notifInfo.Color,
                Icon = notifInfo.Icon,
                CreatedAt = DateTime.Now
            };

            // ---- 4. Calculate Data Sync receivers ----
            HashSet<int> dataReceivers = null!;
            if (perms.DataPermission.HasValue)
            {
                dataReceivers = new HashSet<int>(
                    _authCache.GetAdminIdsByPermission(perms.DataPermission.Value));

                if (context.SupportCategoryId.HasValue && !skipSupportCategoryFilter)
                {
                    var catAdmins = _authCache.GetAdminIdsBySupportCategory(context.SupportCategoryId.Value);
                    dataReceivers.IntersectWith(catAdmins);
                }
            }
            else if (context.SupportCategoryId.HasValue && !skipSupportCategoryFilter)
            {
                dataReceivers = new HashSet<int>(
                    _authCache.GetAdminIdsBySupportCategory(context.SupportCategoryId.Value));
            }

            // ---- 5. Calculate Notification receivers ----
            HashSet<int> notifReceivers = null!;
            if (perms.NotifPermission.HasValue)
            {
                notifReceivers = new HashSet<int>(
                    _authCache.GetAdminIdsByPermission(perms.NotifPermission.Value));

                if (context.SupportCategoryId.HasValue && !skipSupportCategoryFilter)
                {
                    var catAdmins = _authCache.GetAdminIdsBySupportCategory(context.SupportCategoryId.Value);
                    notifReceivers.IntersectWith(catAdmins);
                }
            }

            var onlineIds = _presence.GetOnlineUserIds();

            // ---- 6. Send Data Sync ----
            if (dataReceivers != null && context.DataSyncPayload != null)
            {
                dataReceivers.IntersectWith(onlineIds);
                if (dataReceivers.Count > 0)
                {
                    await _notifier.SendDataSyncAsync(
                        context.EventType.ToString(),
                        context.DataSyncPayload,
                        dataReceivers);
                }
            }

            // ---- 7. Send Notification ----
            if (notifReceivers != null)
            {
                notifReceivers.IntersectWith(onlineIds);
                if (notifReceivers.Count > 0)
                {
                    await _notifier.SendNotificationAsync(notifDto, notifReceivers);

                    foreach (var adminId in notifReceivers)
                    {
                        await _notificationChannel.Writer.WriteAsync(new AdminNotification
                        {
                            AdminUserId = adminId,
                            NotificationType = notifDto.Type,
                            Title = notifDto.Title,
                            Message = notifDto.Message,
                            IsSeen = false,
                            CreatedAt = notifDto.CreatedAt
                        });
                    }
                }
            }
        }

        #endregion

        #region HandleAutoAssignedAsync

        /// <summary>
        /// Handles automatic chat assignment: sends a personalised notification to the assigned
        /// admin and broadcasts a Data Sync to all online admins of the same support category.
        /// </summary>
        private async Task HandleAutoAssignedAsync(AdminEventContext context)
        {
            int targetAdminId = context.AssignedAdminId!.Value;

            var actor = new ActorDto
            {
                Id = 0,
                FullName = "سیستم",
                UserName = "system"
            };
            var target = new TargetDto
            {
                Type = "Chat",
                Id = context.ChatId,
                Name = context.ChatSubject,
                UniqId = context.ChatUniqId
            };

            NotificationInfoDto info = BuildNotificationInfo(context, actor, target);

            var notifDto = new AdminNotificationDto
            {
                Type = context.EventType.ToString(),
                Title = info.Title,
                Message = info.Message,
                Actor = actor,
                Target = target,
                Color = info.Color,
                Icon = info.Icon,
                CreatedAt = DateTime.Now
            };

            // Notify only the assigned admin
            if (_presence.GetOnlineUserIds().Contains(targetAdminId))
            {
                await _notifier.SendNotificationAsync(notifDto, new[] { targetAdminId });
                await _notificationChannel.Writer.WriteAsync(new AdminNotification
                {
                    AdminUserId = targetAdminId,
                    NotificationType = notifDto.Type,
                    Title = notifDto.Title,
                    Message = notifDto.Message,
                    IsSeen = false,
                    CreatedAt = notifDto.CreatedAt
                });
            }

            // Broadcast Data Sync to all online admins of the support category
            if (context.SupportCategoryId.HasValue && context.DataSyncPayload != null)
            {
                var catAdmins = _authCache.GetAdminIdsBySupportCategory(context.SupportCategoryId.Value);
                var onlineCatAdmins = catAdmins.Intersect(_presence.GetOnlineUserIds()).ToList();

                if (onlineCatAdmins.Count > 0)
                {
                    await _notifier.SendDataSyncAsync("ChatLocked", context.DataSyncPayload, onlineCatAdmins);
                }
            }
        }

        #endregion

        #region BuildTargetDto

        /// <summary>
        /// Builds the <see cref="TargetDto"/> based on the event type and available context data.
        /// </summary>
        private TargetDto? BuildTargetDto(AdminEventContext context)
        {
            switch (context.EventType)
            {
                case AdminEventType.UserBanned:
                case AdminEventType.UserUnbanned:
                case AdminEventType.UserBanExpiryChanged:
                case AdminEventType.UserRolesChanged:
                case AdminEventType.UserSupportCategoriesChanged:
                    return new TargetDto
                    {
                        Type = "User",
                        Id = context.TargetUserId,
                        Name = context.TargetFullName,
                        UniqId = context.TargetUserName
                    };

                case AdminEventType.ChatUnlocked:
                case AdminEventType.ChatLocked:
                case AdminEventType.ChatEnded:
                case AdminEventType.ChatAutoAssigned:
                case AdminEventType.ChatAutoUnlocked:
                    return new TargetDto
                    {
                        Type = "Chat",
                        Id = context.ChatId,
                        Name = context.ChatSubject,
                        UniqId = context.ChatUniqId
                    };

                case AdminEventType.RoleCreated:
                case AdminEventType.RoleEdited:
                case AdminEventType.RoleDeleted:
                    return new TargetDto
                    {
                        Type = "Role",
                        Id = context.RoleId,
                        Name = context.RoleName
                    };

                case AdminEventType.SupportCategoryCreated:
                case AdminEventType.SupportCategoryEdited:
                    return new TargetDto
                    {
                        Type = "SupportCategory",
                        Id = context.SupportCategoryId,
                        Name = context.SupportCategoryName
                    };

                default:
                    return null;
            }
        }

        #endregion

        #region BuildNotificationInfo

        /// <summary>
        /// Produces the notification text, colour, and icon for each event type.
        /// </summary>
        private NotificationInfoDto BuildNotificationInfo(
            AdminEventContext ctx, ActorDto actor, TargetDto? target)
        {
            return ctx.EventType switch
            {
                AdminEventType.ChatUnlocked => new NotificationInfoDto
                {
                    Title = "چت آزاد شد",
                    Message = $"ادمین {actor.FullName} چت «{target?.Name ?? "؟"}» را آزاد کرد.",
                    Color = "#10b981",
                    Icon = "fa-unlock"
                },
                AdminEventType.ChatLocked => new NotificationInfoDto
                {
                    Title = "چت قفل شد",
                    Message = $"ادمین {actor.FullName} چت «{target?.Name ?? "؟"}» را به خود اختصاص داد.",
                    Color = "#6366f1",
                    Icon = "fa-lock"
                },
                AdminEventType.ChatEnded => new NotificationInfoDto
                {
                    Title = "چت پایان یافت",
                    Message = $"ادمین {actor.FullName} چت «{target?.Name ?? "؟"}» را پایان داد.",
                    Color = "#6b7280",
                    Icon = "fa-check-circle"
                },
                AdminEventType.ChatEndedByUser => new NotificationInfoDto
                {
                    Title = "پایان چت",
                    Message = $"کاربر چت «{ctx.ChatSubject ?? "؟"}» را پایان داد.",
                    Color = "#6b7280",
                    Icon = "fa-user-times"
                },
                AdminEventType.UserBanned => new NotificationInfoDto
                {
                    Title = "کاربر مسدود شد",
                    Message = $"ادمین {actor.FullName} کاربر {target?.Name ?? "؟"} را مسدود کرد.",
                    Color = "#f59e0b",
                    Icon = "fa-ban"
                },
                AdminEventType.UserUnbanned => new NotificationInfoDto
                {
                    Title = "رفع مسدودیت",
                    Message = $"ادمین {actor.FullName} مسدودیت کاربر {target?.Name ?? "؟"} را برداشت.",
                    Color = "#10b981",
                    Icon = "fa-check-circle"
                },
                AdminEventType.UserBanExpiryChanged => new NotificationInfoDto
                {
                    Title = "تغییر مدت مسدودیت",
                    Message = $"ادمین {actor.FullName} مدت مسدودیت کاربر {target?.Name ?? "؟"} را تغییر داد.",
                    Color = "#f59e0b",
                    Icon = "fa-clock"
                },
                AdminEventType.RoleCreated => new NotificationInfoDto
                {
                    Title = "نقش جدید",
                    Message = $"ادمین {actor.FullName} نقش «{target?.Name ?? "؟"}» را ایجاد کرد.",
                    Color = "#6366f1",
                    Icon = "fa-plus-circle"
                },
                AdminEventType.RoleEdited => new NotificationInfoDto
                {
                    Title = "ویرایش نقش",
                    Message = $"ادمین {actor.FullName} نقش «{target?.Name ?? "؟"}» را ویرایش کرد.",
                    Color = "#6366f1",
                    Icon = "fa-edit"
                },
                AdminEventType.RoleDeleted => new NotificationInfoDto
                {
                    Title = "حذف نقش",
                    Message = $"ادمین {actor.FullName} نقش «{target?.Name ?? "؟"}» را حذف کرد.",
                    Color = "#ef4444",
                    Icon = "fa-trash-alt"
                },
                AdminEventType.SupportCategoryCreated => new NotificationInfoDto
                {
                    Title = "واحد جدید",
                    Message = $"ادمین {actor.FullName} واحد پشتیبانی «{target?.Name ?? "؟"}» را ایجاد کرد.",
                    Color = "#6366f1",
                    Icon = "fa-plus-circle"
                },
                AdminEventType.SupportCategoryEdited => new NotificationInfoDto
                {
                    Title = "ویرایش واحد",
                    Message = $"ادمین {actor.FullName} واحد پشتیبانی «{target?.Name ?? "؟"}» را ویرایش کرد.",
                    Color = "#6366f1",
                    Icon = "fa-edit"
                },
                AdminEventType.ChatAutoAssigned => new NotificationInfoDto
                {
                    Title = "چت جدید تخصیص یافت",
                    Message = $"چت جدید با موضوع «{ctx.ChatSubject ?? "؟"}» به شما تخصیص یافت.",
                    Color = "#10b981",
                    Icon = "fa-hand-point-right"
                },
                AdminEventType.ChatAutoUnlocked => new NotificationInfoDto
                {
                    Title = "آزادسازی خودکار چت",
                    Message = $"چت «{ctx.ChatSubject ?? "؟"}» به دلیل عدم فعالیت به‌طور خودکار آزاد شد.",
                    Color = "#f59e0b",
                    Icon = "fa-clock"
                },
                AdminEventType.UserRolesChanged => new NotificationInfoDto
                {
                    Title = "نقش‌های کاربر تغییر کرد",
                    Message = $"ادمین {actor.FullName} نقش‌های کاربر {target?.Name ?? "?"} را تغییر داد.",
                    Color = "#6366f1",
                    Icon = "fa-user-shield"
                },
                AdminEventType.UserSupportCategoriesChanged => new NotificationInfoDto
                {
                    Title = "واحدهای کاربر تغییر کرد",
                    Message = $"ادمین {actor.FullName} واحدهای پشتیبانی کاربر {target?.Name ?? "?"} را تغییر داد.",
                    Color = "#10b981",
                    Icon = "fa-headset"
                },
                _ => new NotificationInfoDto
                {
                    Title = "رویداد مدیریتی",
                    Message = $"{actor.FullName} یک عملیات انجام داد.",
                    Color = "#6366f1",
                    Icon = "fa-bell"
                }
            };
        }

        #endregion
    }
}