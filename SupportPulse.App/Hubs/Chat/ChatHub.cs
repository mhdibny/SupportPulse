#region Usings

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SupportPulse.App.Security.HubModelValidator;
using SupportPulse.Core.DTOs.Admin.Chat;
using SupportPulse.Core.DTOs.Admin.Message;
using SupportPulse.Core.DTOs.Chat;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.DTOs.Message;
using SupportPulse.Core.Security.ActionFilter.Hub;
using SupportPulse.Core.Services.Admin.Assign;
using SupportPulse.Core.Services.Admin.Chat;
using SupportPulse.Core.Services.Admin.Message;
using SupportPulse.Core.Services.Chats;
using SupportPulse.Core.Services.Hubs.Base;
using SupportPulse.Core.Services.Hubs.Chats;
using SupportPulse.Core.Services.IconMapping;
using SupportPulse.Core.Services.Messages;
using SupportPulse.Core.Services.PresenceTracker;
using SupportPulse.Core.Services.Users;
using SupportPulse.Core.Utilities.ClaimsPrincipals;
using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.App.Hubs.Chat
{
    /// <summary>
    /// SignalR hub for real‑time chat communication between users and admins.
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        #region Constructor & Dependencies

        private readonly IChatService _chatService;
        private readonly IMessageService _messageService;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly IChatHubService _chatHubService;
        private readonly IHubSystemMessage<ChatHub> _hubSystemMessage;
        private readonly IIconMappingService _iconMappingService;
        private readonly IAdminChatService _adminChatService;
        private readonly IAssignChatService _assignService;
        private readonly IAdminMessageService _adminMessageService;
        private readonly IConnectionPresenceTracker _connectionTrackerService;

        public ChatHub(
            IChatService chatService,
            IMessageService messageService,
            IUserService userService,
            IMapper mapper,
            IChatHubService chatHubService,
            IHubSystemMessage<ChatHub> hubSystemMessage,
            IIconMappingService iconMappingService,
            IAdminChatService adminChatService,
            IAssignChatService assignService,
            IAdminMessageService adminMessageService,
            IConnectionPresenceTracker connectionTrackerService)
        {
            _chatService = chatService;
            _messageService = messageService;
            _userService = userService;
            _mapper = mapper;
            _chatHubService = chatHubService;
            _hubSystemMessage = hubSystemMessage;
            _iconMappingService = iconMappingService;
            _adminChatService = adminChatService;
            _assignService = assignService;
            _adminMessageService = adminMessageService;
            _connectionTrackerService = connectionTrackerService;
        }

        #endregion

        #region Connection Lifecycle

        /// <inheritdoc />
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("SetUserName", GetUserName());
            int userId = await GetUserId();

            if (_connectionTrackerService.AddConnection(userId))
            {
                // First connection – broadcast online status to relevant parties
                await NotifyOnlineStatus(userId, isOnline: true);
            }

            // Send presence of other users to the caller
            await SendPresenceOfOthersToCaller(userId);
            await base.OnConnectedAsync();
        }

        /// <inheritdoc />
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            int userId = await GetUserId();

            if (_connectionTrackerService.RemoveConnection(userId))
            {
                // Last connection removed – broadcast offline status
                await NotifyOnlineStatus(userId, isOnline: false);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Sends the online status of a specific target user for a given chat.
        /// </summary>
        private async Task SendPresenceToCallerForChat(int chatId, string chatUniqId, int targetUserId)
        {
            bool isOnline = _connectionTrackerService.HasConnection(targetUserId);
            await Clients.Caller.SendAsync("PresenceUpdate", chatUniqId, isOnline);
        }

        /// <summary>
        /// Sends presence information of the opposite party for all chats the caller participates in.
        /// </summary>
        private async Task SendPresenceOfOthersToCaller(int userId)
        {
            // If the caller is a regular user: status of admins who locked their chats
            var userChats = await _chatService.GetLockedChatsForUserPresenceAsync(userId);
            foreach (var chat in userChats)
            {
                if (_connectionTrackerService.HasConnection(chat.TargetUserId))
                {
                    await Clients.Caller.SendAsync("AdminOnline", chat.ChatUniqId);
                }
            }

            // If the caller is an admin: status of users whose chats they locked
            var adminChats = await _chatService.GetLockedChatsForAdminPresenceAsync(userId);
            foreach (var chat in adminChats)
            {
                if (_connectionTrackerService.HasConnection(chat.TargetUserId))
                {
                    await Clients.Caller.SendAsync("UserOnline", chat.ChatUniqId);
                }
            }
        }

        /// <summary>
        /// Notifies relevant users about the online/offline status change of a given user.
        /// </summary>
        private async Task NotifyOnlineStatus(int userId, bool isOnline)
        {
            // Notify admins who have locked chats of this user
            var userChats = await _chatService.GetLockedChatsForUserPresenceAsync(userId);
            foreach (var chat in userChats)
            {
                await Clients.User(chat.TargetUserId.ToString())
                    .SendAsync("PresenceUpdate", chat.ChatUniqId, isOnline);
            }

            // Notify creators of chats locked by this admin
            var adminChats = await _chatService.GetLockedChatsForAdminPresenceAsync(userId);
            foreach (var chat in adminChats)
            {
                await Clients.User(chat.TargetUserId.ToString())
                    .SendAsync("AdminPresenceUpdate", chat.ChatUniqId, isOnline);
            }
        }

        #endregion

        #region Typing Indicators

        /// <summary>
        /// Notifies the chat creator that an admin started typing.
        /// </summary>
        public async Task StartTyping(int chatId)
        {
            int adminId = await GetUserId();
            if (!await _chatService.IsChatLockedByAdminAsync(chatId, adminId)) return;

            string chatUniqId = await _chatService.GetChatUniqIdByIdAsync(chatId);
            int creatorId = await _chatService.GetChatCreatorIdAsync(chatId);
            if (creatorId > 0)
                await Clients.User(creatorId.ToString()).SendAsync("AdminTyping", chatUniqId, true);
        }

        /// <summary>
        /// Notifies the chat creator that an admin stopped typing.
        /// </summary>
        public async Task StopTyping(int chatId)
        {
            int adminId = await GetUserId();
            if (!await _chatService.IsChatLockedByAdminAsync(chatId, adminId)) return;

            string chatUniqId = await _chatService.GetChatUniqIdByIdAsync(chatId);
            int creatorId = await _chatService.GetChatCreatorIdAsync(chatId);
            if (creatorId > 0)
                await Clients.User(creatorId.ToString()).SendAsync("AdminTyping", chatUniqId, false);
        }

        /// <summary>
        /// Notifies the locking admin that a user started typing.
        /// </summary>
        public async Task StartTypingUser(string chatUniqId)
        {
            int userId = await GetUserId();
            if (!await _chatService.IsChatBelongsToUserAndLockedAsync(chatUniqId, userId)) return;

            int chatId = await _chatService.GetChatIdByUniqIdAsync(chatUniqId);
            int adminId = await _chatService.GetChatLockedAdminIdAsync(chatId);
            if (adminId > 0)
                await Clients.User(adminId.ToString()).SendAsync("UserTyping", chatUniqId, true);
        }

        /// <summary>
        /// Notifies the locking admin that a user stopped typing.
        /// </summary>
        public async Task StopTypingUser(string chatUniqId)
        {
            int userId = await GetUserId();
            if (!await _chatService.IsChatBelongsToUserAndLockedAsync(chatUniqId, userId)) return;

            int chatId = await _chatService.GetChatIdByUniqIdAsync(chatUniqId);
            int adminId = await _chatService.GetChatLockedAdminIdAsync(chatId);
            if (adminId > 0)
                await Clients.User(adminId.ToString()).SendAsync("UserTyping", chatUniqId, false);
        }

        #endregion

        #region System Message

        /// <summary>
        /// Sends a system alert message to the caller only.
        /// </summary>
        protected async Task SendSystemMessageAsync(SystemAlertDto alert)
        {
            await Clients.Caller.SendAsync("SystemMessage", alert);
        }

        #endregion

        #region User Methods

        /// <summary>
        /// Creates a new support chat for the authenticated user.
        /// </summary>
        public async Task CreateSupportChat(CreateSupportChatDto supportChat)
        {
            var validateResult = HubModelValidator.Validate(supportChat);
            if (!validateResult.IsSuccess)
            {
                await SendSystemMessageAsync(validateResult.Alert);
                return;
            }

            CreateChatSupportDto chat = new(await GetUserId(), supportChat.Subject, supportChat.SupportUnitId);

            OperationResult<SuccessCreatedChatDto> createChatResult = await _chatService.CreateChatAsync(chat);

            if (createChatResult.IsSuccess)
            {
                createChatResult.Data.SupportCategoryClass =
                    _iconMappingService.GetIconClassByIconKey(createChatResult.Data.SupportCategoryIconKey);
                createChatResult.Data.SupportCategoryIconKey = "";

                await Clients.User(await GetUserIdAsString()).SendAsync("NewChatCreated", createChatResult.Data);
                await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(createChatResult));
            }
            else
            {
                await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(createChatResult));
            }
        }

        /// <summary>
        /// Retrieves chat details and messages for the authenticated user.
        /// </summary>
        public async Task GetChatData(string chatUniqId)
        {
            OperationResult<ChatDto> chat = await _chatService.GetUserChatAsync(chatUniqId, await GetUserId());
            if (!chat.IsSuccess)
            {
                await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(chat));
                return;
            }

            chat.Data.SupportCategoryClass =
                _iconMappingService.GetIconClassByIconKey(chat.Data.SupportCategoryIconKey);
            chat.Data.SupportCategoryIconKey = "";

            await Clients.User(await GetUserIdAsString()).SendAsync("ReceiveChatData", chat.Data);

            OperationResult<List<MessageDto>> messages =
                await _chatService.GetMessageOfChatAsync(chatUniqId, await GetUserId());

            if (!messages.IsSuccess)
            {
                await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(messages));
                return;
            }

            await Clients.User(await GetUserIdAsString()).SendAsync("ReceiveChatMessages", messages.Data);

            bool isUserOnline = _connectionTrackerService.HasConnection(await GetUserId());
            await Clients.Caller.SendAsync("PresenceUpdate", chat.Data.ChatUniqId, isUserOnline);

            // Inform the user whether the locking admin is online
            int chatId = await _chatService.GetChatIdByUniqIdAsync(chatUniqId);
            int? lockedAdminId = await _chatService.GetChatLockedAdminIdAsync(chatId);
            if (lockedAdminId.HasValue)
            {
                bool isAdminOnline = _connectionTrackerService.HasConnection(lockedAdminId.Value);
                await Clients.Caller.SendAsync("AdminPresenceUpdate", chatUniqId, isAdminOnline);
            }
        }

        /// <summary>
        /// Sends a plain‑text message to the support team.
        /// </summary>
        public async Task SendMessageToSupport(SendPlainTextMessageToSupportDto message)
        {
            var validateResult = HubModelValidator.Validate(message);
            if (!validateResult.IsSuccess)
            {
                await SendSystemMessageAsync(validateResult.Alert);
                return;
            }

            var result = await _messageService.SendMessageByUserAsync(message, await GetUserId());
            if (!result.IsSuccess)
            {
                await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(result));
                return;
            }

            var data = result.Data;
            await _chatHubService.SendChatMessageToUsersAsync(data.ReceiverUserIds, data.MessageResult.Message);
        }

        /// <summary>
        /// Ends a chat by the requesting user.
        /// </summary>
        public async Task EndChat(string chatUniqId)
        {
            if (string.IsNullOrWhiteSpace(chatUniqId))
            {
                await SendSystemMessageAsync(new SystemAlertDto
                {
                    Message = "برای پایان دادن به گفتگو لطفا گفتگو را انتخاب کنید.",
                    Title = "خطای اعتبار سنجی",
                    Type = "warning"
                });
                return;
            }

            OperationResult endChatResult = await _chatService.EndChatAsync(chatUniqId, await GetUserId());
            if (!endChatResult.IsSuccess)
            {
                await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(endChatResult));
                return;
            }

            await Clients.User(await GetUserIdAsString()).SendAsync("ChatEnded", chatUniqId);
        }

        #endregion

        #region Mark Seen

        /// <summary>
        /// Marks all unseen messages from the other party as seen,
        /// and notifies them using the same <paramref name="chatUniqId"/>.
        /// </summary>
        /// <param name="chatUniqId">The unique public identifier of the chat.</param>
        public async Task MarkMessagesAsSeen(string chatUniqId)
        {
            var userId = Context.User.GetUserIdAsInt();
            var notifyUserId = await _messageService
                .MarkMessagesAsSeenAsync(chatUniqId, userId);

            if (notifyUserId.HasValue)
            {
                await Clients.User(notifyUserId.Value.ToString())
                    .SendAsync("MessagesSeen", chatUniqId, DateTime.Now);
            }
        }

        #endregion

        #region Admin Methods

        /// <summary>
        /// Ends a chat locked by the current admin.
        /// </summary>
        [HubPermissionChecker(AdminPermission.EndChat)]
        public async Task EndChatByAdmin(int chatId)
        {
            if (chatId == 0)
            {
                await SendSystemMessageAsync(new SystemAlertDto
                {
                    Message = "برای پایان دادن به گفتگو لطفا گفتگو را انتخاب کنید.",
                    Title = "خطای اعتبار سنجی",
                    Type = "warning"
                });
                return;
            }

            OperationResult<ChatEndedDto> endChatResult = await _adminChatService.EndChatAsync(chatId, await GetUserId());

            if (!endChatResult.IsSuccess)
            {
                await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(endChatResult));
                return;
            }

            await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(endChatResult));
            await Clients.User(await GetUserIdAsString()).SendAsync("ChatEndedByAdmin", chatId);
            await Clients.User(endChatResult.Data!.CreatorId.ToString()).SendAsync("ChatEndedByAdmin", endChatResult.Data.ChatUniqId);
        }

        /// <summary>
        /// Retrieves the list of chats visible to the admin.
        /// </summary>
        [HubPermissionChecker(AdminPermission.ViewChatList)]
        public async Task GetAdminChatList()
        {
            OperationResult<List<AdminChatListDto>> chats = await _adminChatService.GetChatListAsync(await GetUserId());
            if (!chats.IsSuccess)
            {
                await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(chats));
                return;
            }

            chats.Data.ForEach(c => c.SupportCategoryIconClass =
                _iconMappingService.GetIconClassByIconKey(c.SupportCategoryIconKey));

            await Clients.Caller.SendAsync("AdminChatsReceived", chats.Data);
        }

        /// <summary>
        /// Locks a free chat for the current admin.
        /// </summary>
        [HubPermissionChecker(AdminPermission.LockChat)]
        public async Task LockChat(int chatId)
        {
            OperationResult lockChat = await _assignService.ManualLockChatAsync(chatId, await GetUserId());
            if (!lockChat.IsSuccess)
            {
                await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(lockChat));
                return;
            }

            await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(lockChat));
            await Clients.Caller.SendAsync("ChatLocked", chatId);

            // Send presence updates
            int adminId = await GetUserId();
            var chatInfo = await _chatService.GetChatPresenceInfoAsync(chatId);
            if (chatInfo != null)
            {
                int creatorId = await _chatService.GetChatCreatorIdAsync(chatId);
                bool isUserOnline = _connectionTrackerService.HasConnection(creatorId);
                await Clients.Caller.SendAsync("PresenceUpdate", chatInfo.ChatUniqId, isUserOnline);

                bool isAdminOnline = _connectionTrackerService.HasConnection(adminId);
                await Clients.User(creatorId.ToString()).SendAsync("AdminPresenceUpdate", chatInfo.ChatUniqId, isAdminOnline);
            }
        }

        /// <summary>
        /// Unlocks a chat that is currently locked by the current admin.
        /// </summary>
        [HubPermissionChecker(AdminPermission.UnlockChat)]
        public async Task UnLockChat(int chatId)
        {
            OperationResult unLockChat = await _assignService.ManualUnlockChatAsync(chatId, await GetUserId());
            if (!unLockChat.IsSuccess)
            {
                await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(unLockChat));
                return;
            }

            await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(unLockChat));
            await Clients.Caller.SendAsync("ChatUnlocked", chatId);

            // Clear presence for both parties
            var chatInfo = await _chatService.GetChatPresenceInfoAsync(chatId);
            if (chatInfo != null)
            {
                await Clients.Caller.SendAsync("PresenceUpdate", chatInfo.ChatUniqId, false);

                int creatorId = await _chatService.GetChatCreatorIdAsync(chatId);
                await Clients.User(creatorId.ToString()).SendAsync("AdminPresenceUpdate", chatInfo.ChatUniqId, false);
            }
        }

        /// <summary>
        /// Retrieves detailed chat data for the admin view.
        /// </summary>
        [HubPermissionChecker(AdminPermission.ViewChatDetails)]
        public async Task GetAdminChatData(int chatId)
        {
            OperationResult<AdminChatDataDto> chat = await _adminChatService.GetChatDataAsync(chatId, await GetUserId());
            if (!chat.IsSuccess)
            {
                await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(chat));
                return;
            }

            chat.Data.SupportCategoryIconClass =
                _iconMappingService.GetIconClassByIconKey(chat.Data.SupportCategoryIconKey);
            chat.Data.SupportCategoryIconKey = "";

            await Clients.User(await GetUserIdAsString()).SendAsync("ReceiveChatData", chat.Data);

            OperationResult<List<MessageDto>> messages =
                await _adminChatService.GetMessageOfChatForAdminAsync(chatId, await GetUserId());

            if (!messages.IsSuccess)
            {
                await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(messages));
                return;
            }

            await Clients.User(await GetUserIdAsString()).SendAsync("ReceiveChatMessages", messages.Data);

            int creatorId = chat.Data.CreatorId;
            bool isUserOnline = _connectionTrackerService.HasConnection(creatorId);
            await Clients.Caller.SendAsync("PresenceUpdate", chat.Data.ChatUniqId, isUserOnline);
        }

        /// <summary>
        /// Sends a plain‑text message from an admin to the user in a locked chat.
        /// </summary>
        [HubPermissionChecker(AdminPermission.SendMessageInChat)]
        public async Task SendMessageToUser(SendPlainTextMessageToUserDto message)
        {
            var validateResult = HubModelValidator.Validate(message);
            if (!validateResult.IsSuccess)
            {
                await SendSystemMessageAsync(validateResult.Alert);
                return;
            }

            var result = await _adminMessageService.SendMessageToUserAsync(message, await GetUserId());
            if (!result.IsSuccess)
            {
                await SendSystemMessageAsync(_mapper.Map<SystemAlertDto>(result));
                return;
            }

            var data = result.Data;
            await _chatHubService.SendChatMessageToUsersAsync(data.ReceiverUserIds, data.MessageResult.Message);
        }

        #endregion

        #region Private Helpers

        private async Task<int> GetUserId()
        {
            return Context.User!.GetUserIdAsInt();
        }

        private async Task<string> GetUserIdAsString()
        {
            return Context.User!.GetUserId();
        }

        private async Task<string> GetUserName()
        {
            return Context.User.GetUserName();
        }

        #endregion
    }
}