#region Usings

using Microsoft.EntityFrameworkCore;
using SupportPulse.Core.DTOs.Chat;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.DTOs.Message;
using SupportPulse.Core.Services.Common;
using SupportPulse.Core.Services.Files;
using SupportPulse.Data.Context;
using SupportPulse.Data.Entities.Chat.Message;
using SupportPulse.Data.Entities.Chat.Message.MessageContent;
using SupportPulse.Data.Enums.Message;


#endregion

namespace SupportPulse.Core.Services.Messages
{
    /// <summary>
    /// Handles sending messages from users to the support system, including plain text and file messages.
    /// </summary>
    public class MessageService : IMessageService
    {
        #region Constructor & Dependencies

        private readonly ApplicationDbContext _db;
        private readonly IOperationResultAction _operation;
        private readonly IFileService _fileService;

        public MessageService(
            ApplicationDbContext db,
            IOperationResultAction operation,
            IFileService fileService)
        {
            _db = db;
            _operation = operation;
            _fileService = fileService;
        }

        #endregion

        #region Send Plain Text Message

        /// <inheritdoc />
        public async Task<OperationResult<SendMessageResultWithReceiversDto>> SendMessageByUserAsync(
            SendPlainTextMessageToSupportDto message, int senderId)
        {
            // Validate chat existence and get essential info (including LockedByAdminId)
            var chatInfo = await _db.Chats
                .Where(c => c.ChatUniqId == message.ChatUniqId && c.CreatorId == senderId)
                .Select(c => new UserMessageChatInfoDto
                {
                    Id = c.Id,
                    IsEnded = c.IsEnded,
                    SupportCategoryId = c.SupportCategoryId,
                    ChatUniqId = c.ChatUniqId,
                    SenderUserName = _db.Users.Where(u => u.Id == senderId).Select(u => u.UserName).FirstOrDefault()!,
                    LockedByAdminId = c.LockedByAdminId
                })
                .SingleOrDefaultAsync();

            if (chatInfo is null)
                return _operation.SendError<SendMessageResultWithReceiversDto>("چت یافت نشد.");

            if (chatInfo.IsEnded)
                return _operation.SendError<SendMessageResultWithReceiversDto>("این گفتگو به پایان رسیده است.");

            if (string.IsNullOrWhiteSpace(message.MessageData))
                return _operation.SendError<SendMessageResultWithReceiversDto>("پیام نمی‌تواند خالی باشد.");

            // Build and persist the plain‑text message
            var newMessage = new Message
            {
                ChatId = chatInfo.Id,
                SenderId = senderId,
                IsSeen = false,
                Time = DateTime.Now,
                MessageContent = new MessageContent
                {
                    MessageTypeId = (int)MessageTypes.PlainText,
                    Data = message.MessageData
                }
            };

            _db.Messages.Add(newMessage);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch
            {
                return _operation.SendError<SendMessageResultWithReceiversDto>("خطا در ذخیره پیام.");
            }

            // Determine receivers: the sender and the locking admin (if any)
            var receivers = new List<string> { senderId.ToString() };
            if (chatInfo.LockedByAdminId.HasValue)
                receivers.Add(chatInfo.LockedByAdminId.Value.ToString());

            var newMessageDto = new NewMessageDto(
                newMessage.Id,
                chatInfo.SupportCategoryId,
                chatInfo.SenderUserName!,
                "",
                chatInfo.ChatUniqId,
                newMessage.Time,
                message.MessageData,
                new List<UserAttachFileDto>()
            );

            var result = new SendMessageResultWithReceiversDto
            {
                MessageResult = new SendMessageResultDto
                {
                    Message = newMessageDto,
                    SupportCategoryId = chatInfo.SupportCategoryId
                },
                ReceiverUserIds = receivers
            };

            return _operation.SendSuccess(entity: result);
        }

        #endregion

        #region Send File / Mixed Message

        /// <inheritdoc />
        public async Task<OperationResult<SendMessageResultWithReceiversDto>> SendMessageByUserAsync(
            SendMessageToSupportDto message, int senderId)
        {
            // Ensure at least text or a file is provided
            if (string.IsNullOrWhiteSpace(message.MessageData) &&
                (message.AttachFiles == null || !message.AttachFiles.Any()))
            {
                return _operation.SendError<SendMessageResultWithReceiversDto>("پیام نمی‌تواند خالی باشد.");
            }

            // Validate chat and fetch info (including LockedByAdminId)
            var chatInfo = await _db.Chats
                .Where(c => c.ChatUniqId == message.ChatUniqId && c.CreatorId == senderId)
                .Select(c => new UserMessageChatInfoDto
                {
                    Id = c.Id,
                    IsEnded = c.IsEnded,
                    SupportCategoryId = c.SupportCategoryId,
                    ChatUniqId = c.ChatUniqId,
                    SenderUserName = _db.Users.Where(u => u.Id == senderId).Select(u => u.UserName).FirstOrDefault()!,
                    LockedByAdminId = c.LockedByAdminId
                })
                .SingleOrDefaultAsync();

            if (chatInfo is null)
                return _operation.SendError<SendMessageResultWithReceiversDto>("چت یافت نشد یا متعلق به شما نیست.");

            if (chatInfo.IsEnded)
                return _operation.SendError<SendMessageResultWithReceiversDto>("این گفتگو به پایان رسیده است.");

            // Save uploaded files
            List<AttachFileDto> savedFiles;
            MessageTypes messageType;
            try
            {
                savedFiles = await _fileService.SaveFilesAsync(message.AttachFiles);
                messageType = string.IsNullOrWhiteSpace(message.MessageData)
                    ? MessageTypes.AttachFile
                    : MessageTypes.PlainTextAndAttachFile;
            }
            catch
            {
                return _operation.SendError<SendMessageResultWithReceiversDto>();
            }

            // Build message content with files
            var messageContent = new MessageContent
            {
                MessageTypeId = (int)messageType,
                Data = messageType == MessageTypes.AttachFile ? null : message.MessageData,
                AttachFiles = savedFiles.Select(sf => new AttachFile
                {
                    SavedPath = sf.SavePath,
                    OriginalFileName = sf.OriginalName
                }).ToList()
            };

            var newMessage = new Message
            {
                ChatId = chatInfo.Id,
                SenderId = senderId,
                IsSeen = false,
                Time = DateTime.Now,
                MessageContent = messageContent
            };

            _db.Messages.Add(newMessage);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch
            {
                return _operation.SendError<SendMessageResultWithReceiversDto>("خطا در ذخیره پیام.");
            }

            // Build receivers list (sender + locking admin)
            var receivers = new List<string> { senderId.ToString() };
            if (chatInfo.LockedByAdminId.HasValue)
                receivers.Add(chatInfo.LockedByAdminId.Value.ToString());

            var newMessageDto = new NewMessageDto(
                newMessage.Id,
                chatInfo.SupportCategoryId,
                chatInfo.SenderUserName!,
                "",
                chatInfo.ChatUniqId,
                newMessage.Time,
                message.MessageData ?? "",
                savedFiles.Select(sf => new UserAttachFileDto
                {
                    DownloadName = sf.SavePath,
                    OriginalName = sf.OriginalName
                }).ToList()
            );

            var result = new SendMessageResultWithReceiversDto
            {
                MessageResult = new SendMessageResultDto
                {
                    Message = newMessageDto,
                    SupportCategoryId = chatInfo.SupportCategoryId
                },
                ReceiverUserIds = receivers
            };

            return _operation.SendSuccess(entity: result);
        }

        #endregion

        /// <inheritdoc />
        public async Task<int?> MarkMessagesAsSeenAsync(string chatUniqId, int userId)
        {
            var chatInfo = await _db.Chats
                .AsNoTracking()
                .Where(c => c.ChatUniqId == chatUniqId)
                .Select(c => new
                {
                    c.Id,
                    c.CreatorId,
                    c.LockedByAdminId
                })
                .FirstOrDefaultAsync();

            if (chatInfo == null) return null;

            // Only the creator or the admin who currently locked the chat can mark messages as seen
            if (userId != chatInfo.CreatorId && userId != chatInfo.LockedByAdminId) return null;

            // ── 2. Batch update all unseen messages from the OTHER party ──
            await _db.Messages
                .Where(m => m.ChatId == chatInfo.Id && m.SenderId != userId && !m.IsSeen)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.IsSeen, true)
                    .SetProperty(m => m.SeenAt, DateTime.Now)
                );

            // ── 3. Determine notification target ──
            if (userId == chatInfo.CreatorId && chatInfo.LockedByAdminId.HasValue)
                return chatInfo.LockedByAdminId.Value;

            if (userId == chatInfo.LockedByAdminId)
                return chatInfo.CreatorId;

            return null;
        }
    }
}
