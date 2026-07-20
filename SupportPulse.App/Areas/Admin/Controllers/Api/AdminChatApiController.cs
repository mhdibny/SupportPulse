#region Usings

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPulse.App.Hubs.Chat;
using SupportPulse.Core.DTOs.Admin.Message;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.Security.ActionFilter;
using SupportPulse.Core.Services.Admin.Message;
using SupportPulse.Core.Services.Hubs.Base;
using SupportPulse.Core.Services.Hubs.Chats;
using SupportPulse.Core.Utilities.ClaimsPrincipals;
using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.App.Areas.Admin.Controllers.Api
{
    /// <summary>
    /// API controller for admin chat file uploads.
    /// </summary>
    [Area("Admin")]
    [Authorize]
    [Route("api/admin/chat")]
    [PermissionChecker(AdminPermission.SendMessageInChat)]
    [ApiController]
    public class AdminChatApiController : ControllerBase
    {
        #region Constructor & Dependencies

        private readonly IAdminMessageService _adminMessageService;
        private readonly IChatHubService _chatHubService;
        private readonly IHubSystemMessage<ChatHub> _chatHubSystemMessage;
        private readonly IMapper _mapper;

        public AdminChatApiController(
            IAdminMessageService adminMessageService,
            IChatHubService chatHubService,
            IHubSystemMessage<ChatHub> chatHubSystemMessage,
            IMapper mapper)
        {
            _adminMessageService = adminMessageService;
            _chatHubService = chatHubService;
            _chatHubSystemMessage = chatHubSystemMessage;
            _mapper = mapper;
        }

        #endregion

        #region Send File

        /// <summary>
        /// Uploads files as an admin message in a locked chat.
        /// </summary>
        /// <param name="message">The message containing files and optional text.</param>
        [HttpPost("send-file")]
        public async Task<IActionResult> SendFile([FromForm] SendMessageToUserDto message)
        {
            int adminId = User.GetUserIdAsInt();

            if (!ModelState.IsValid)
            {
                string? firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault();

                await _chatHubSystemMessage.SendValidationErrorToUserAsync(
                    adminId, firstError ?? "خطای اعتبار سنجی");
                return BadRequest();
            }

            var result = await _adminMessageService.SendMessageToUserAsync(message, adminId);

            if (!result.IsSuccess)
            {
                await _chatHubSystemMessage.SendSystemMessageToUserAsync(
                    adminId, _mapper.Map<SystemAlertDto>(result));
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