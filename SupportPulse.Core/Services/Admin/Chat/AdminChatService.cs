#region Usings

using Microsoft.EntityFrameworkCore;
using SupportPulse.Core.DTOs.Admin.Chat;
using SupportPulse.Core.DTOs.Admin.EventDispatcher;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.DTOs.Message;
using SupportPulse.Core.Services.Admin.EventDispatcher;
using SupportPulse.Core.Services.Admin.Session;
using SupportPulse.Core.Services.Admin.SupportCategories;
using SupportPulse.Core.Services.Common;
using SupportPulse.Core.Utilities.Converters;
using SupportPulse.Data.Context;
using SupportPulse.Data.Enums.Admin;
using SupportPulse.Data.Enums.Chat;
using SupportPulse.Data.Enums.Message;

#endregion

namespace SupportPulse.Core.Services.Admin.Chat
{
    /// <summary>
    /// Implements admin chat retrieval, message history, and chat ending with event dispatching.
    /// </summary>
    public class AdminChatService : IAdminChatService
    {
        #region Constructor & Dependencies

        private readonly IAdminSupportCategoryService _adminSupportCategoryService;
        private readonly IOperationResultAction _operation;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentAdminSession _adminSession;
        private readonly IAdminEventDispatcher _eventDispatcher;

        public AdminChatService(
            IAdminSupportCategoryService adminSupportCategoryService,
            IOperationResultAction operation,
            ApplicationDbContext db,
            ICurrentAdminSession adminSession,
            IAdminEventDispatcher eventDispatcher)
        {
            _adminSupportCategoryService = adminSupportCategoryService;
            _operation = operation;
            _db = db;
            _adminSession = adminSession;
            _eventDispatcher = eventDispatcher;
        }

        #endregion

        #region Get Chat List

        /// <inheritdoc />
        public async Task<OperationResult<List<AdminChatListDto>>> GetChatListAsync(int adminId)
        {
            List<int> userSupportCategories = await
                _adminSupportCategoryService.GetUserSupportCategoryIdsAsync(adminId);

            if (userSupportCategories.Count == 0)
            {
                return _operation.SendError<List<AdminChatListDto>>(
                    "شما مجوز پاسخگویی به هیچ واحد پشتیبانی را ندارید.");
            }

            var userChatList = await _db.Chats
                .AsNoTracking()
                .Where(r => userSupportCategories.Contains(r.SupportCategoryId)
                            && (r.LockedByAdminId == adminId || r.LockedByAdminId == null)
                            && r.IsEnded == false)
                .Select(s => new AdminChatListDto
                {
                    ChatId = s.Id,
                    CreatedTime = s.CreatedTime,
                    CreatorId = s.CreatorId,
                    ChatUniqId = s.ChatUniqId,
                    CreatorFullName = s.Creator!.FullName,
                    Subject = s.Subject,
                    SupportCategoryIconKey = s.SupportCategory!.IconKey,
                    SupportCategoryName = s.SupportCategory.Name,
                    LastMessageText = s.Messages!
                        .Where(m => m.MessageContent!.MessageTypeId == (int)MessageTypes.PlainText)
                        .OrderByDescending(m => m.Time)
                        .Select(m => m.MessageContent!.Data)
                        .FirstOrDefault(),
                    LastMessageDateTime = s.Messages!
                        .OrderByDescending(m => m.Time)
                        .Select(m => m.Time)
                        .FirstOrDefault(),
                    IsChatLocked = s.LockedByAdminId != null
                })
                .ToListAsync();

            return _operation.SendSuccess(entity: userChatList);
        }

        #endregion

        #region Get Chat Data

        /// <inheritdoc />
        public async Task<OperationResult<AdminChatDataDto>> GetChatDataAsync(int chatId, int adminId)
        {
            List<int> userSupportCategories = await
                _adminSupportCategoryService.GetUserSupportCategoryIdsAsync(adminId);

            if (userSupportCategories.Count == 0)
            {
                return _operation.SendError<AdminChatDataDto>(
                    "شما مجوز پاسخگویی به هیچ واحد پشتیبانی را ندارید.");
            }

            var chatData = await _db.Chats
                .AsNoTracking()
                .Where(r => r.Id == chatId
                            && userSupportCategories.Contains(r.SupportCategoryId)
                            && (r.LockedByAdminId == adminId || r.LockedByAdminId == null))
                .Select(s => new AdminChatDataDto
                {
                    ChatId = s.Id,
                    CreatedTime = s.CreatedTime,
                    ChatUniqId = s.ChatUniqId,
                    CreatorFirstName = s.Creator!.FirstName,
                    CreatorLastName = s.Creator.LastName,
                    CreatorId = s.CreatorId,
                    CreatorUserName = s.Creator.UserName,
                    IsChatLocked = s.LockedByAdminId != null,
                    Subject = s.Subject,
                    SupportCategoryIconKey = s.SupportCategory!.IconKey,
                    SupportCategoryName = s.SupportCategory.Name,
                    CreatorIsBanned = s.Creator.IsBan
                })
                .SingleOrDefaultAsync();

            if (chatData is null)
            {
                return _operation.SendError<AdminChatDataDto>(
                    "چتی با مشخصات وارد شده یافت نشد.");
            }

            return _operation.SendSuccess(entity: chatData);
        }

        #endregion

        #region Get Messages

        /// <inheritdoc />
        public async Task<OperationResult<List<MessageDto>>> GetMessageOfChatForAdminAsync(
            int chatId, int adminId)
        {
            // Verify admin belongs to the chat's support category
            var chatSupportCategoryId = await _db.Chats
                .Where(c => c.Id == chatId)
                .Select(c => c.SupportCategoryId)
                .FirstOrDefaultAsync();

            if (chatSupportCategoryId == 0)
                return _operation.SendError<List<MessageDto>>("چت مورد نظر یافت نشد.");

            bool hasAccess = await _db.UserSupportCategories
                .AnyAsync(usc => usc.UserId == adminId && usc.SupportCategoryId == chatSupportCategoryId);

            if (!hasAccess)
                return _operation.SendError<List<MessageDto>>("شما مجوز مشاهدهٔ این چت را ندارید.");

            var messages = await _db.Chats
                .AsNoTracking()
                .Where(c => c.Id == chatId)
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
                return _operation.SendError<List<MessageDto>>("هیچ پیامی یافت نشد.");

            return _operation.SendSuccess(entity: messages);
        }

        #endregion

        #region End Chat

        /// <inheritdoc />
        public async Task<OperationResult<ChatEndedDto>> EndChatAsync(int chatId, int adminId)
        {
            // Fetch only necessary fields and verify ownership
            var chatInfo = await _db.Chats
                .Where(r => r.Id == chatId && r.LockedByAdminId == adminId)
                .Select(c => new ChatEndContextDto
                {
                    Subject = c.Subject,
                    ChatUniqId = c.ChatUniqId,
                    SupportCategoryId = c.SupportCategoryId,
                    CreatorId = c.CreatorId
                })
                .SingleOrDefaultAsync();

            if (chatInfo == null)
            {
                return _operation.SendError<ChatEndedDto>("چت مورد نظر یافت نشد.");
            }

            var entity = await _db.Chats.FindAsync(chatId);
            if (entity!.IsEnded)
            {
                return _operation.SendError<ChatEndedDto>("این چت قبلا به پایان رسیده است.");
            }

            entity.IsEnded = true;
            entity.ChatStatusId = (int)ChatStatusEnum.Completed;
            await _db.SaveChangesAsync();

            ChatEndedDto endedChat = new()
            {
                CreatorId = chatInfo.CreatorId,
                ChatUniqId = chatInfo.ChatUniqId
            };

            string message = $"چت {chatInfo.Subject} با موفقیت به پایان رسید، و وضعیت به تکمیل شده تغییر کرد.";

            #region Dispatch

            var context = new AdminEventContext
            {
                EventType = AdminEventType.ChatEnded,
                ActorAdminId = adminId,
                ActorFullName = _adminSession.GetAdminFullName(),
                ActorUserName = _adminSession.GetAdminUserName(),
                ChatId = chatId,
                ChatSubject = chatInfo.Subject,
                ChatUniqId = chatInfo.ChatUniqId,
                SupportCategoryId = chatInfo.SupportCategoryId,
                DataSyncPayload = chatId
            };

            await _eventDispatcher.DispatchAsync(context);

            #endregion

            return _operation.SendSuccess(message, OperationStatus.Success, endedChat);
        }

        #endregion
    }
}