#region Usings

using AutoMapper;
using SupportPulse.Core.Services.Chats;
using SupportPulse.Core.Services.IconMapping;
using SupportPulse.Core.Services.SupportCategories;
using SupportPulse.Core.Services.Users;
using SupportPulse.Core.ViewModels.Pages.Home;

#endregion

namespace SupportPulse.App.Factories.Home
{
    /// <summary>
    /// Builds the home page view model for a specific user,
    /// including support categories, user chats, and the latest messages of the first chat.
    /// </summary>
    public class HomeViewModelFactory
    {
        #region Constructor & Dependencies

        private readonly ISupportCategoryService _categoryService;
        private readonly IChatService _chatService;
        private readonly IIconMappingService _iconMappingService;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;

        public HomeViewModelFactory(ISupportCategoryService categoryService, IChatService chatService, IIconMappingService iconMappingService, IMapper mapper, IUserService userService)
        {
            _categoryService = categoryService;
            _chatService = chatService;
            _iconMappingService = iconMappingService;
            _mapper = mapper;
            _userService = userService;
        }

        #endregion

        #region Index

        /// <summary>
        /// Creates the <see cref="HomeVM"/> for the home page of the specified user.
        /// </summary>
        /// <param name="userId">The current user identifier.</param>
        public async Task<HomeVM> CreateIndexModelAsync(int userId)
        {
            var model = new HomeVM();

            // Load and enrich support categories with icon classes
            model.SupportCategories = await _categoryService.GetCategoriesAsync();
            model.SupportCategories.ForEach(s =>
                s.IconClass = _iconMappingService.GetIconClassByIconKey(s.IconKey));

            // Load the user's chats and enrich with icon classes
            model.UserChats = await _chatService.GetUserChatsAsync(userId);
            model.UserChats.ForEach(u =>
                u.SupportCategoryIconClass = _iconMappingService.GetIconClassByIconKey(u.SupportCategoryIconKey));

            // Pre‑load the latest messages of the most recent chat (if any)
            if (model.UserChats?.Any() == true)
            {
                string lastChatUniqId = model.UserChats.First().UniqChatId;
                var lastChatMessages = await _chatService.GetMessageOfChatAsync(lastChatUniqId, userId);
                model.LatestChatMessages = lastChatMessages.Data;
            }

            return model;
        }

        #endregion
    }
}