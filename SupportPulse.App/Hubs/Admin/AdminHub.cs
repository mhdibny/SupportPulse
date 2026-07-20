#region Usings

using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using SupportPulse.App.Security.HubModelValidator;
using SupportPulse.Core.DTOs.Admin.Ban;
using SupportPulse.Core.DTOs.Admin.Role;
using SupportPulse.Core.DTOs.Admin.SupportCategory;
using SupportPulse.Core.DTOs.Admin.User;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.Security.ActionFilter.Hub;
using SupportPulse.Core.Services.Admin.Ban;
using SupportPulse.Core.Services.Admin.OnlineAdminTracker;
using SupportPulse.Core.Services.Admin.Roles;
using SupportPulse.Core.Services.Admin.SupportCategories;
using SupportPulse.Core.Services.Admin.Users;
using SupportPulse.Core.Services.IconMapping;
using SupportPulse.Core.Services.PresenceTracker;
using SupportPulse.Core.Utilities.ClaimsPrincipals;
using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.App.Hubs.Admin
{
    /// <summary>
    /// SignalR hub for real‑time admin panel communication.
    /// All mutating methods are protected by <see cref="HubPermissionChecker"/> attributes.
    /// </summary>
    public class AdminHub : Hub
    {
        #region Constructor & Dependencies

        private readonly IRoleService _roleService;
        private readonly IMapper _mapper;
        private readonly IAdminUserService _adminUserService;
        private readonly IBanService _banService;
        private readonly IAdminSupportCategoryService _supportCategoryService;
        private readonly IIconMappingService _iconMapping;
        private readonly IOnlineAdminTracker _onlineAdminTracker;
        private readonly IConnectionPresenceTracker _connectionTracker;

        public AdminHub(
            IRoleService roleService,
            IMapper mapper,
            IAdminUserService adminUserService,
            IBanService banService,
            IAdminSupportCategoryService supportCategoryService,
            IIconMappingService iconMapping,
            IOnlineAdminTracker onlineAdminTracker,
            IConnectionPresenceTracker connectionTracker)
        {
            _roleService = roleService;
            _mapper = mapper;
            _adminUserService = adminUserService;
            _banService = banService;
            _supportCategoryService = supportCategoryService;
            _iconMapping = iconMapping;
            _onlineAdminTracker = onlineAdminTracker;
            _connectionTracker = connectionTracker;
        }

        #endregion

        #region Connection Lifecycle

        /// <inheritdoc />
        public override async Task OnConnectedAsync()
        {
            int userId = GetUserId();
            _connectionTracker.AddConnection(userId);
            _onlineAdminTracker.AddConnection(userId);
            await base.OnConnectedAsync();
        }

        /// <inheritdoc />
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            int userId = GetUserId();
            _connectionTracker.RemoveConnection(userId);
            _onlineAdminTracker.RemoveConnection(userId);
            await base.OnDisconnectedAsync(exception);
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Sends a system alert message to the current caller.
        /// </summary>
        private async Task SendSystemAlert(SystemAlertDto alert)
            => await Clients.Caller.SendAsync("SystemMessage", alert);

        /// <summary>
        /// Handles a failed operation result by sending the error to the caller.
        /// Returns <c>true</c> if the operation succeeded; otherwise, <c>false</c>.
        /// </summary>
        private async Task<bool> HandleFailure(OperationResult result)
        {
            if (result.IsSuccess)
                return true;

            await SendSystemAlert(_mapper.Map<SystemAlertDto>(result));
            return false;
        }

        /// <summary>
        /// Generic version of <see cref="HandleFailure(OperationResult)"/> for results with data.
        /// </summary>
        private async Task<bool> HandleFailure<T>(OperationResult<T> result)
        {
            if (result.IsSuccess)
                return true;

            await SendSystemAlert(_mapper.Map<SystemAlertDto>(result));
            return false;
        }

        /// <summary>
        /// Validates that the given ID is greater than zero and sends an error to the caller if not.
        /// </summary>
        private async Task<bool> ValidateId(int id, string entityName, string action = "ویرایش")
        {
            if (id > 0)
                return true;

            await SendSystemAlert(new SystemAlertDto
            {
                Message = $"لطفاً شناسه {entityName} را برای {action} وارد کنید",
                Type = "warning",
                Title = "خطای اعتبارسنجی"
            });
            return false;
        }

        /// <summary>
        /// Validates the given model using <see cref="HubModelValidator"/> and sends the first error to the caller.
        /// </summary>
        private async Task<bool> ValidateModel<T>(T model) where T : class
        {
            var validationResult = HubModelValidator.Validate(model);
            if (validationResult.IsSuccess)
                return true;

            await SendSystemAlert(validationResult.Alert!);
            return false;
        }

        /// <summary>
        /// Extracts the current admin's user ID from claims.
        /// </summary>
        private int GetUserId()
        {
            return Context.User!.GetUserIdAsInt();
        }

        #endregion

        #region Icon Mappings

        /// <summary>
        /// Returns all available icon mappings to the caller.
        /// </summary>
        public async Task GetAllIconMappings()
        {
            var icons = _iconMapping.GetAllIconMappings();
            await Clients.Caller.SendAsync("ReceiveIconMappings", icons);
        }

        #endregion

        #region Roles — List

        /// <summary>
        /// Returns the full list of roles.
        /// </summary>
        [HubPermissionChecker(AdminPermission.RoleList)]
        public async Task GetRoles()
        {
            var result = await _roleService.GetRoleListAsync();
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("ReceiveRoles", result.Data);
        }

        /// <summary>
        /// Searches roles by name and/or permission.
        /// </summary>
        [HubPermissionChecker(AdminPermission.RoleList)]
        public async Task SearchInRoles(SearchRoleDto search)
        {
            var result = await _roleService.SearchInRolesAsync(search);
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("RoleSearchResult", result.Data);
        }

        #endregion

        #region Roles — Edit

        /// <summary>
        /// Returns role data for the edit form.
        /// </summary>
        [HubPermissionChecker(AdminPermission.EditRole)]
        public async Task GetRoleForEdit(int roleId)
        {
            if (!await ValidateId(roleId, "نقش", "ویرایش"))
                return;

            var result = await _roleService.GetRoleForEditAsync(roleId);
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("ReceiveRoleForEdit", result.Data);
        }

        /// <summary>
        /// Returns all system permissions.
        /// </summary>
        public async Task GetPermissions()
        {
            var permissions = await _roleService.GetPermissionsAsync();
            await Clients.Caller.SendAsync("ReceivePermissions", permissions);
        }

        /// <summary>
        /// Applies changes to an existing role.
        /// </summary>
        [HubPermissionChecker(AdminPermission.EditRole)]
        public async Task EditRole(EditRoleDto role)
        {
            if (!await ValidateModel(role))
                return;

            var result = await _roleService.EditRoleAsync(role);
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("SuccessEditRole", _mapper.Map<SystemAlertDto>(result));
        }

        #endregion

        #region Roles — Add

        /// <summary>
        /// Creates a new role.
        /// </summary>
        [HubPermissionChecker(AdminPermission.AddRole)]
        public async Task AddRole(AddRoleDto role)
        {
            if (!await ValidateModel(role))
                return;

            var result = await _roleService.AddRoleAsync(role);
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("RoleSuccessfullyCreated", _mapper.Map<SystemAlertDto>(result));
        }

        #endregion

        #region Roles — Delete

        /// <summary>
        /// Returns role data for the delete confirmation dialog.
        /// </summary>
        [HubPermissionChecker(AdminPermission.DeleteRole)]
        public async Task GetRoleForDelete(int roleId)
        {
            if (!await ValidateId(roleId, "نقش", "حذف"))
                return;

            var result = await _roleService.GetRoleForDelete(roleId);
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("ReceiveRoleForDelete", result.Data);
        }

        /// <summary>
        /// Deletes a role permanently.
        /// </summary>
        [HubPermissionChecker(AdminPermission.DeleteRole)]
        public async Task DeleteRole(int roleId)
        {
            if (!await ValidateId(roleId, "نقش", "حذف"))
                return;

            var result = await _roleService.DeleteRoleAsync(roleId);
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("RoleSuccessfullyDeleted", _mapper.Map<SystemAlertDto>(result));
        }

        #endregion

        #region Users — List

        /// <summary>
        /// Returns a paginated, filterable user list.
        /// </summary>
        [HubPermissionChecker(AdminPermission.UserList)]
        public async Task GetUsers(UserPageRequestDto? paging, UserSearchTermDto? search)
        {
            var result = await _adminUserService.GetUserListAsync(search, paging);
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("ReceiveUserList", result.Data);
        }

        #endregion

        #region Ban Management

        /// <summary>
        /// Returns the ban history for a specific user.
        /// </summary>
        [HubPermissionChecker(AdminPermission.ViewBanHistory)]
        public async Task GetUserBanHistories(int userId)
        {
            if (!await ValidateId(userId, "کاربر"))
                return;

            var result = await _banService.GetUserBanHistoryListAsync(userId);
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("ReceiveUserBanHistories", result.Data);
        }

        /// <summary>
        /// Applies a ban to a user.
        /// </summary>
        [HubPermissionChecker(AdminPermission.BanUser)]
        public async Task BanUser(BanUserDto ban)
        {
            if (!await ValidateModel(ban))
                return;

            var result = await _banService.BanUserAsync(ban, GetUserId());
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("UserBanned", _mapper.Map<SystemAlertDto>(result));
        }

        /// <summary>
        /// Lifts a ban from a user.
        /// </summary>
        [HubPermissionChecker(AdminPermission.UnBanUser)]
        public async Task UnBanUser(UnBanUserDto unBan)
        {
            if (!await ValidateModel(unBan))
                return;

            var result = await _banService.UnBanUserAsync(unBan, GetUserId());
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("UserUnBanned", _mapper.Map<SystemAlertDto>(result));
        }

        /// <summary>
        /// Changes the expiry time of an existing ban.
        /// </summary>
        [HubPermissionChecker(AdminPermission.ChangeBanExpiry)]
        public async Task ChangeUserBanExpiry(ChangeBanExpiryTimeDto changeBan)
        {
            if (!await ValidateModel(changeBan))
                return;

            var result = await _banService.ChangeUserBanExpiryAsync(changeBan, GetUserId());
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("UserBanChanged", _mapper.Map<SystemAlertDto>(result));
        }

        #endregion

        #region User Role Assignment

        /// <summary>
        /// Returns the list of roles available for assignment.
        /// </summary>
        [HubPermissionChecker(AdminPermission.AssignRoleToUser)]
        public async Task GetRolesForAssignToUser()
        {
            var result = await _roleService.GetRolesListAsync();
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("RoleListReceived", result.Data);
        }

        /// <summary>
        /// Returns the roles currently assigned to a user.
        /// </summary>
        [HubPermissionChecker(AdminPermission.AssignRoleToUser)]
        public async Task GetUserRolesAsync(int userId)
        {
            if (!await ValidateId(userId, "کاربر", "افزودن/ویرایش نقش"))
                return;

            var result = await _adminUserService.GetUserRolesAsync(userId);
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("UserDataForChangeRoleReceived", result.Data);
        }

        /// <summary>
        /// Saves the role assignment for a user.
        /// </summary>
        [HubPermissionChecker(AdminPermission.AssignRoleToUser)]
        public async Task ChangeUserRolesAsync(UserRolesDto roles)
        {
            if (!await ValidateModel(roles))
                return;

            var result = await _adminUserService.AddOrEditUserRolesAsync(roles);
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("UserRolesChanged", _mapper.Map<SystemAlertDto>(result));
        }

        #endregion

        #region Support Categories

        /// <summary>
        /// Returns support categories formatted for user assignment.
        /// </summary>
        [HubPermissionChecker(AdminPermission.AssignSupportCategoryToUser)]
        public async Task GetSupportCategoriesForAssignToUser()
        {
            var categories = await _supportCategoryService.GetSupportCategoryListForAssignToUserAsync();
            await Clients.Caller.SendAsync("SupportCategoryListReceived", categories);
        }

        /// <summary>
        /// Returns the flat list of support categories.
        /// </summary>
        [HubPermissionChecker(AdminPermission.AssignSupportCategoryToUser)]
        public async Task GetSupportCategories()
        {
            var result = await _supportCategoryService.GetSupportCategoryListAsync();
            await Clients.Caller.SendAsync("ReceiveSupportCategories", result);
        }

        /// <summary>
        /// Creates a new support category.
        /// </summary>
        [HubPermissionChecker(AdminPermission.AddSupportCategory)]
        public async Task AddSupportCategory(AddSupportCategoryDto supportCategory)
        {
            if (!await ValidateModel(supportCategory))
                return;

            var result = await _supportCategoryService.AddSupportCategoryAsync(supportCategory);
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("SupportCategorySuccessfullyCreated", _mapper.Map<SystemAlertDto>(result));
        }

        /// <summary>
        /// Returns a support category's data for the edit form.
        /// </summary>
        [HubPermissionChecker(AdminPermission.EditSupportCategory)]
        public async Task GetSupportCategoryForEdit(int supportCategoryId)
        {
            if (!await ValidateId(supportCategoryId, "واحد پشتیبانی", "ویرایش"))
                return;

            var result = await _supportCategoryService.GetSupportCategoryForEditAsync(supportCategoryId);
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("ReceiveSupportCategoryForEdit", result.Data);
        }

        /// <summary>
        /// Applies changes to an existing support category.
        /// </summary>
        [HubPermissionChecker(AdminPermission.EditSupportCategory)]
        public async Task EditSupportCategory(EditSupportCategoryDto editSupportCategory)
        {
            if (!await ValidateModel(editSupportCategory))
                return;

            var result = await _supportCategoryService.EditSupportCategoryAsync(editSupportCategory);
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("SuccessEditSupportCategory", _mapper.Map<SystemAlertDto>(result));
        }

        /// <summary>
        /// Returns the support categories currently assigned to a user.
        /// </summary>
        [HubPermissionChecker(AdminPermission.AssignSupportCategoryToUser)]
        public async Task GetUserSupportCategories(int userId)
        {
            if (!await ValidateId(userId, "کاربر", "افزودن/ویرایش واحد پشتیبانی"))
                return;

            var result = await _adminUserService.GetUserSupportCategoriesAsync(userId);
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("UserDataForChangeSupportCategoryReceived", result.Data);
        }

        /// <summary>
        /// Saves the support category assignment for a user.
        /// </summary>
        [HubPermissionChecker(AdminPermission.AssignSupportCategoryToUser)]
        public async Task ChangeUserSupportCategories(UserSupportCategoryDto supportCategory)
        {
            if (!await ValidateModel(supportCategory))
                return;

            var result = await _adminUserService.AddOrEditUserSupportCategoriesAsync(supportCategory);
            if (!await HandleFailure(result))
                return;

            await Clients.Caller.SendAsync("UserSupportCategoriesChanged", _mapper.Map<SystemAlertDto>(result));
        }

        #endregion
    }
}