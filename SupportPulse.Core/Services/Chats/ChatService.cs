#region Usings

using Microsoft.EntityFrameworkCore;
using SupportPulse.Core.DTOs.Admin.AutoLock;
using SupportPulse.Core.DTOs.Admin.EventDispatcher;
using SupportPulse.Core.DTOs.Admin.Notification;
using SupportPulse.Core.DTOs.Chat;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.DTOs.Message;
using SupportPulse.Core.Security.Password;
using SupportPulse.Core.Services.Admin.EventDispatcher;
using SupportPulse.Core.Services.Common;
using SupportPulse.Core.Utilities.Converters;
using SupportPulse.Data.Context;
using SupportPulse.Data.Entities.Chat;
using SupportPulse.Data.Entities.User.Notification;
using SupportPulse.Data.Enums.Admin;
using SupportPulse.Data.Enums.Chat;
using SupportPulse.Data.Enums.Message;
using System.Threading.Channels;

#endregion

namespace SupportPulse.Core.Services.Chats
{
    /// <summary>
    /// Implements chat creation, retrieval, ending, and presence tracking for both regular users and admins.
    /// </summary>
    public class ChatService : IChatService
    {
        #region Constructor & Dependencies

        private readonly ApplicationDbContext _db;
        private readonly IOperationResultAction _operation;
        private readonly Channel<AssignChatDto> _assignChannel;
        private readonly IAdminEventNotifier _notifier;
        private readonly Channel<AdminNotification> _notificationChannel;
        private readonly IAdminEventDispatcher _eventDispatcher;

        public ChatService(ApplicationDbContext db, IOperationResultAction operation, Channel<AssignChatDto> assignChannel, IAdminEventNotifier notifier, Channel<AdminNotification> notificationChannel, IAdminEventDispatcher eventDispatcher)
        {
            _db = db;
            _operation = operation;
            _assignChannel = assignChannel;
            _notifier = notifier;
            _notificationChannel = notificationChannel;
            _eventDispatcher = eventDispatcher;
        }

        #endregion

        #region Chat Lifecycle

        /// <inheritdoc />
        public async Task<OperationResult<SuccessCreatedChatDto>> CreateChatAsync(CreateChatSupportDto chat)
        {
            // Validate the support category
            var category = await _db.SupportCategories
                .AsNoTracking()
                .Where(sc => sc.Id == chat.SupportCategoryId && sc.IsActive)
                .Select(sc => new { sc.Name, sc.IconKey })
                .SingleOrDefaultAsync();

            if (category is null)
            {
                return _operation.SendError<SuccessCreatedChatDto>(
                    "مقادیر وارد شده صحیح نمی‌باشد.", OperationStatus.ValidationError);
            }

            // Build the new chat entity
            var newChat = new Chat
            {
                Subject = chat.Subject,
                SupportCategoryId = chat.SupportCategoryId,
                CreatorId = chat.UserId,
                IsEnded = false,
                ChatStatusId = (int)ChatStatusEnum.Responding,
                CreatedTime = DateTime.Now,
                ChatUniqId = SecurityTool.GenerateChatUniqId()
            };

            try
            {
                await _db.Chats.AddAsync(newChat);
                await _db.SaveChangesAsync();
            }
            catch
            {
                return _operation.SendError<SuccessCreatedChatDto>(
                    "هنگام ایجاد چت پشتیبانی جدید، مشکلی رخ داد، لطفاً مجدد تلاش کنید.");
            }

            var chatDto = new SuccessCreatedChatDto(
                newChat.ChatUniqId,
                newChat.Subject,
                "درحال پاسخگویی",
                category.Name,
                category.IconKey,
                "");

            // Queue the new chat for automatic assignment
            await _assignChannel.Writer.WriteAsync(new AssignChatDto
            {
                ChatId = newChat.Id,
                SupportCategoryId = newChat.SupportCategoryId
            });

            return _operation.SendSuccess<SuccessCreatedChatDto>(
                "چت با پشتیبانی ساخته شد، لطفاً وارد چت شده و مشکلاتتان را بیان کنید.",
                entity: chatDto);
        }

        /// <inheritdoc />
        public async Task<OperationResult> EndChatAsync(string uniqChatId, int userId)
        {
            var chat = await _db.Chats
                .FirstOrDefaultAsync(c => c.ChatUniqId == uniqChatId && c.CreatorId == userId);

            if (chat is null)
                return _operation.SendError("گفتگوی مورد نظر پیدا نشد.");

            if (chat.IsEnded)
                return _operation.SendError("این گفتگو قبلاً پایان یافته است.");

            chat.IsEnded = true;
            chat.ChatStatusId = (int)ChatStatusEnum.Completed;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch
            {
                return _operation.SendError("هنگام انجام عملیات خطایی رخ داد، لطفاً مجدد تلاش کنید.");
            }

            // ── Event dispatch after successful save ──
            var chatInfo = await _db.Chats
                .AsNoTracking()
                .Where(c => c.ChatUniqId == uniqChatId)
                .Select(c => new
                {
                    c.Id,
                    c.Subject,
                    c.SupportCategoryId,
                    c.LockedByAdminId
                })
                .FirstOrDefaultAsync();

            if (chatInfo != null)
            {
                if (chatInfo.LockedByAdminId.HasValue)
                {
                    // Scenario A: chat locked → notify only the locking admin
                    int adminId = chatInfo.LockedByAdminId.Value;

                    // 1) Data Sync – remove chat from admin's panel
                    await _notifier.SendDataSyncAsync("ChatEnded", chatInfo.Id, new[] { adminId });

                    // 2) Real‑time notification
                    var notifDto = new AdminNotificationDto
                    {
                        Type = AdminEventType.ChatEndedByUser.ToString(),
                        Title = "پایان چت توسط کاربر",
                        Message = $"کاربر چت «{chatInfo.Subject}» را پایان داد.",
                        Actor = new ActorDto { Id = 0, FullName = "کاربر", UserName = "user" },
                        Target = new TargetDto
                        {
                            Type = "Chat",
                            Id = chatInfo.Id,
                            Name = chatInfo.Subject,
                            UniqId = uniqChatId
                        },
                        Color = "#6b7280",
                        Icon = "fa-user-times",
                        CreatedAt = DateTime.Now
                    };
                    await _notifier.SendNotificationAsync(notifDto, new[] { adminId });

                    // 3) Persist notification via channel (non‑blocking)
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
                else
                {
                    // Scenario B: chat free → broadcast Data Sync to all online admins of the support category
                    var context = new AdminEventContext
                    {
                        EventType = AdminEventType.ChatEndedByUser,
                        ActorAdminId = 0,
                        ActorFullName = "کاربر",
                        ActorUserName = "user",
                        ChatId = chatInfo.Id,
                        ChatSubject = chatInfo.Subject,
                        ChatUniqId = uniqChatId,
                        SupportCategoryId = chatInfo.SupportCategoryId,
                        DataSyncPayload = chatInfo.Id
                    };
                    await _eventDispatcher.DispatchAsync(context);
                }
            }

            return _operation.SendSuccess($"گفتگو {chat.Subject} با موفقیت پایان یافت و به وضعیت تکمیل‌شده تغییر کرد.");
        }


        #endregion

        #region User Chats

        /// <inheritdoc />
        public async Task<List<UserChatsDto>> GetUserChatsAsync(int userId)
        {
            return await _db.Chats
                .AsNoTracking()
                .Where(c => c.CreatorId == userId)
                .OrderByDescending(c => c.CreatedTime)
                .Select(c => new UserChatsDto(
                    c.ChatUniqId,
                    c.Subject,
                    c.ChatStatus!.Name,
                    c.SupportCategory!.Name,
                    c.CreatedTime,
                    c.SupportCategory.IconKey,
                    "", // IconClass will be filled later by the caller
                    c.Messages!
                        .Where(m => m.MessageContent!.MessageTypeId == (int)MessageTypes.PlainText)
                        .OrderByDescending(m => m.Time)
                        .Select(m => m.MessageContent!.Data)
                        .FirstOrDefault(),
                    c.Messages!
                        .OrderByDescending(m => m.Time)
                        .Select(m => m.Time)
                        .FirstOrDefault()
                ))
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<OperationResult<ChatDto>> GetUserChatAsync(string uniqChatId, int userId)
        {
            bool ownsChat = await _db.Chats.AnyAsync(c => c.ChatUniqId == uniqChatId && c.CreatorId == userId);
            if (!ownsChat)
            {
                return _operation.SendError<ChatDto>("لطفا اطلاعات را دستکاری نکنید!");
            }

            var chat = await _db.Chats
                .AsNoTracking()
                .AsSplitQuery()
                .Where(c => c.ChatUniqId == uniqChatId)
                .Select(s => new ChatDto(
                    s.ChatUniqId,
                    s.Subject,
                    s.Creator!.UserName,
                    s.ChatStatus!.Name,
                    s.SupportCategory!.Name,
                    s.CreatedTime.ToShamsi())
                {
                    SupportCategoryIconKey = s.SupportCategory.IconKey
                })
                .SingleOrDefaultAsync();

            if (chat is not null)
            {
                return _operation.SendSuccess(entity: chat);
            }

            return _operation.SendError<ChatDto>(
                "هنگام دریافت اطلاعات چت مشکلی پیش آمد، لطفا مجدد تلاش کنید.", OperationStatus.Error);
        }

        /// <inheritdoc />
        public async Task<OperationResult<List<MessageDto>>> GetMessageOfChatAsync(string uniqChatId, int userId)
        {
            var messages = await _db.Chats
                .AsNoTracking()
                .Where(c => c.ChatUniqId == uniqChatId && c.CreatorId == userId)
                .Select(c => c.Messages!
                    .OrderBy(m => m.Time)
                    .Select(m => new MessageDto
                    {
                        ChatUniqId = m.Chat!.ChatUniqId,
                        IsSeen = m.IsSeen,
                        MessageTypeId = m.MessageContent!.MessageTypeId,
                        Data = m.MessageContent.Data,
                        AttachFiles = m.MessageContent.AttachFiles!.Select(at => new UserAttachFileDto
                        {
                            DownloadName = at.SavedPath,
                            OriginalName = at.OriginalFileName
                        }).ToList(),
                        SenderName = m.Sender!.FullName,
                        Time = m.Time.ToDayTime(),
                        SenderUserName = m.SenderId == c.CreatorId ? m.Sender.UserName : ""
                    })
                    .ToList()
                )
                .FirstOrDefaultAsync();

            if (messages is null)
            {
                return _operation.SendError<List<MessageDto>>("لطفا اطلاعات را دستکاری نکنید!");
            }

            return _operation.SendSuccess(entity: messages);
        }

        /// <inheritdoc />
        public async Task<OperationResult<List<MessageDto>>> GetMessageOfChatAsync(int chatId, int userId)
        {
            var messages = await _db.Chats
                .AsNoTracking()
                .Where(c => c.Id == chatId && c.CreatorId == userId)
                .Select(c => c.Messages!
                    .OrderBy(m => m.Time)
                    .Select(m => new MessageDto
                    {
                        ChatUniqId = m.Chat!.ChatUniqId,
                        IsSeen = m.IsSeen,
                        MessageTypeId = m.MessageContent!.MessageTypeId,
                        Data = m.MessageContent.Data,
                        AttachFiles = m.MessageContent.AttachFiles!.Select(at => new UserAttachFileDto
                        {
                            DownloadName = at.SavedPath,
                            OriginalName = at.OriginalFileName
                        }).ToList(),
                        SenderName = m.Sender!.FullName,
                        Time = m.Time.ToDayTime(),
                        SenderUserName = m.Sender.UserName
                    })
                    .ToList()
                )
                .FirstOrDefaultAsync();

            if (messages is null)
            {
                return _operation.SendError<List<MessageDto>>("لطفا اطلاعات را دستکاری نکنید!");
            }

            return _operation.SendSuccess(entity: messages);
        }

        /// <inheritdoc />
        public async Task<int> GetUserChatByUniqChatIdAsync(string uniqChatId)
        {
            int? chatId = await _db.Chats
                .AsNoTracking()
                .Where(c => c.ChatUniqId == uniqChatId)
                .Select(s => s.Id)
                .SingleOrDefaultAsync();

            return chatId ?? 0;
        }

        /// <inheritdoc />
        public async Task<bool> IsThisUserHaveThisChat(string chatUniqId, int userId)
        {
            return await _db.Chats.AnyAsync(c => c.CreatorId == userId && c.ChatUniqId == chatUniqId);
        }

        #endregion

        #region Chat Identification

        /// <inheritdoc />
        public async Task<string> GetChatUniqIdByIdAsync(int chatId)
        {
            return await _db.Chats
                .Where(c => c.Id == chatId)
                .Select(c => c.ChatUniqId)
                .FirstOrDefaultAsync() ?? string.Empty;
        }

        /// <inheritdoc />
        public async Task<int> GetChatIdByUniqIdAsync(string uniqChatId)
        {
            return await _db.Chats
                .Where(c => c.ChatUniqId == uniqChatId)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();
        }

        #endregion

        #region Lock Status

        /// <inheritdoc />
        public async Task<bool> IsChatLockedByAdminAsync(int chatId, int adminUserId)
        {
            return await _db.Chats.AnyAsync(c => c.Id == chatId && c.LockedByAdminId == adminUserId);
        }

        /// <inheritdoc />
        public async Task<bool> IsChatBelongsToUserAndLockedAsync(string chatUniqId, int userId)
        {
            return await _db.Chats.AnyAsync(c =>
                c.ChatUniqId == chatUniqId && c.CreatorId == userId && c.LockedByAdminId != null);
        }

        /// <inheritdoc />
        public async Task<int> GetChatLockedAdminIdAsync(int chatId)
        {
            return await _db.Chats
                .Where(c => c.Id == chatId && c.LockedByAdminId != null)
                .Select(c => c.LockedByAdminId!.Value)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<int> GetChatCreatorIdAsync(int chatId)
        {
            return await _db.Chats
                .Where(c => c.Id == chatId)
                .Select(c => c.CreatorId)
                .FirstOrDefaultAsync();
        }

        #endregion

        #region Presence

        /// <inheritdoc />
        public async Task<List<ChatPresenceInfoDto>> GetLockedChatsForUserPresenceAsync(int creatorUserId)
        {
            return await _db.Chats
                .Where(c => c.CreatorId == creatorUserId && c.LockedByAdminId != null && !c.IsEnded)
                .Select(c => new ChatPresenceInfoDto
                {
                    ChatId = c.Id,
                    ChatUniqId = c.ChatUniqId,
                    TargetUserId = c.LockedByAdminId!.Value
                })
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<ChatPresenceInfoDto>> GetLockedChatsForAdminPresenceAsync(int adminUserId)
        {
            return await _db.Chats
                .Where(c => c.LockedByAdminId == adminUserId && !c.IsEnded)
                .Select(c => new ChatPresenceInfoDto
                {
                    ChatId = c.Id,
                    ChatUniqId = c.ChatUniqId,
                    TargetUserId = c.CreatorId
                })
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<ChatPresenceInfoDto?> GetChatPresenceInfoAsync(int chatId)
        {
            return await _db.Chats
                .Where(c => c.Id == chatId)
                .Select(c => new ChatPresenceInfoDto
                {
                    ChatId = c.Id,
                    ChatUniqId = c.ChatUniqId,
                    TargetUserId = 0  // Not used in this query
                })
                .FirstOrDefaultAsync();
        }

        #endregion
    }
}