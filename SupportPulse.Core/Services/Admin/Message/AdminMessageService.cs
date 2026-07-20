#region Usings

using Microsoft.EntityFrameworkCore;
using SupportPulse.Core.DTOs.Admin.Message;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.DTOs.Message;
using SupportPulse.Core.Services.Common;
using SupportPulse.Core.Services.Files;
using SupportPulse.Data.Context;
using SupportPulse.Data.Entities.Chat.Message.MessageContent;
using SupportPulse.Data.Enums.Message;

#endregion

namespace SupportPulse.Core.Services.Admin.Message
{
    /// <summary>
    /// Handles message sending by admins in locked chats, supporting both plain text and file attachments.
    /// </summary>
    public class AdminMessageService : IAdminMessageService
    {
        #region Constructor & Dependencies

        private readonly ApplicationDbContext _db;
        private readonly IOperationResultAction _operation;
        private readonly IFileService _fileService;

        public AdminMessageService(
            ApplicationDbContext db,
            IOperationResultAction operation,
            IFileService fileService)
        {
            _db = db;
            _operation = operation;
            _fileService = fileService;
        }

        #endregion

        #region Send Plain Text Message (Hub)

        /// <inheritdoc />
        public async Task<OperationResult<SendMessageResultWithReceiversDto>> SendMessageToUserAsync(
            SendPlainTextMessageToUserDto message, int adminId)
        {
            // Validate the chat is locked by the admin and not ended
            var chatInfo = await _db.Chats
                .Where(c => c.Id == message.ChatId
                            && c.LockedByAdminId == adminId
                            && !c.IsEnded)
                .Select(c => new AdminMessageChatInfoDto
                {
                    Id = c.Id,
                    SupportCategoryId = c.SupportCategoryId,
                    ChatUniqId = c.ChatUniqId,
                    AdminFullName = _db.Users.Where(u => u.Id == adminId).Select(u => u.FullName).FirstOrDefault()!,
                    CreatorId = c.CreatorId,
                })
                .SingleOrDefaultAsync();

            if (chatInfo is null)
            {
                return _operation.SendError<SendMessageResultWithReceiversDto>(
                    "چت یافت نشد، یا توسط شما قفل نشده است، یا به پایان رسیده است.");
            }

            if (string.IsNullOrWhiteSpace(message.MessageData))
            {
                return _operation.SendError<SendMessageResultWithReceiversDto>("پیام نمی‌تواند خالی باشد.");
            }

            // Build and persist the plain‑text message
            var newMessage = new Data.Entities.Chat.Message.Message
            {
                ChatId = chatInfo.Id,
                SenderId = adminId,
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

            // Receivers: the sending admin + the chat creator
            var receivers = new List<string> { adminId.ToString(), chatInfo.CreatorId.ToString() };

            var newMessageDto = new NewMessageDto(
                newMessage.Id,
                chatInfo.SupportCategoryId,
                "",
                chatInfo.AdminFullName,
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

        #region Send File / Mixed Message (API)

        /// <inheritdoc />
        public async Task<OperationResult<SendMessageResultWithReceiversDto>> SendMessageToUserAsync(
            SendMessageToUserDto message, int adminId)
        {
            if (message.AttachFiles == null || message.AttachFiles.Count == 0)
            {
                return _operation.SendError<SendMessageResultWithReceiversDto>("حداقل یک فایل باید ارسال شود.");
            }

            // Validate chat lock and status
            var chatInfo = await _db.Chats
                .Where(c => c.Id == message.ChatId
                            && c.LockedByAdminId == adminId
                            && !c.IsEnded)
                .Select(c => new AdminMessageChatInfoDto
                {
                    Id = c.Id,
                    SupportCategoryId = c.SupportCategoryId,
                    ChatUniqId = c.ChatUniqId,
                    AdminFullName = _db.Users.Where(u => u.Id == adminId).Select(u => u.FullName).FirstOrDefault()!,
                    CreatorId = c.CreatorId
                })
                .SingleOrDefaultAsync();

            if (chatInfo is null)
            {
                return _operation.SendError<SendMessageResultWithReceiversDto>(
                    "چت یافت نشد، یا توسط شما قفل نشده است، یا به پایان رسیده است.");
            }

            // Save uploaded files
            List<AttachFileDto> savedFiles;
            try
            {
                savedFiles = await _fileService.SaveFilesAsync(message.AttachFiles);
            }
            catch
            {
                return _operation.SendError<SendMessageResultWithReceiversDto>();
            }

            // Determine message type based on content
            MessageTypes messageType = string.IsNullOrWhiteSpace(message.MessageData)
                ? MessageTypes.AttachFile
                : MessageTypes.PlainTextAndAttachFile;

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

            var newMessage = new Data.Entities.Chat.Message.Message
            {
                ChatId = chatInfo.Id,
                SenderId = adminId,
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

            // Receivers: the admin + the chat creator
            var receivers = new List<string>
            {
                adminId.ToString(),
                chatInfo.CreatorId.ToString()
            };

            var newMessageDto = new NewMessageDto(
                newMessage.Id,
                chatInfo.SupportCategoryId,
                "",
                chatInfo.AdminFullName!,
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
    }
}