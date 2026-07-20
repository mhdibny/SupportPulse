#region Usings

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPulse.App.Hubs.Chat;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.DTOs.Message;
using SupportPulse.Core.Services.Hubs.Base;
using SupportPulse.Core.Services.Hubs.Chats;
using SupportPulse.Core.Services.Messages;
using SupportPulse.Core.Utilities.ClaimsPrincipals;

#endregion

namespace SupportPulse.App.Controllers.Api.Chat
{
    /// <summary>
    /// API endpoint for sending file‑based messages to support (used by the user‑side chat).
    /// </summary>
    [Authorize]
    [Route("api/chat")]
    [ApiController]
    public class ChatApiController : ControllerBase
    {
        #region Constructor & Dependencies

        private readonly IMessageService _messageService;
        private readonly IHubSystemMessage<ChatHub> _chatHubSystemMessage;
        private readonly IChatHubService _chatHubService;
        private readonly IMapper _mapper;

        public ChatApiController(
            IMessageService messageService,
            IHubSystemMessage<ChatHub> chatHubSystemMessage,
            IChatHubService chatHubService,
            IMapper mapper)
        {
            _messageService = messageService;
            _chatHubSystemMessage = chatHubSystemMessage;
            _chatHubService = chatHubService;
            _mapper = mapper;
        }

        #endregion

        #region Send File

        /// <summary>
        /// Handles file uploads from the user's chat and broadcasts the resulting message.
        /// </summary>
        /// <param name="message">The message data including files and optional text.</param>
        [HttpPost("send-file")]
        public async Task<IActionResult> SendFile([FromForm] SendMessageToSupportDto message)
        {
            int userId = User.GetUserIdAsInt();

            if (!ModelState.IsValid)
            {
                string? firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault();

                await _chatHubSystemMessage.SendValidationErrorToUserAsync(
                    userId, firstError ?? "خطای اعتبار سنجی");
                return BadRequest();
            }

            var result = await _messageService.SendMessageByUserAsync(message, userId);
            if (!result.IsSuccess)
            {
                await _chatHubSystemMessage.SendSystemMessageToUserAsync(
                    userId, _mapper.Map<SystemAlertDto>(result));
                return BadRequest();
            }

            var data = result.Data;
            await _chatHubService.SendChatMessageToUsersAsync(
                data.ReceiverUserIds, data.MessageResult.Message);
            return Ok();
        }

        #endregion
    }
}