#region Usings

using Microsoft.EntityFrameworkCore;
using SupportPulse.Core.DTOs.Admin.EventDispatcher;
using SupportPulse.Core.DTOs.Admin.SupportCategory;
using SupportPulse.Core.DTOs.Admin.User;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.Services.Admin.EventDispatcher;
using SupportPulse.Core.Services.Admin.Session;
using SupportPulse.Core.Services.Common;
using SupportPulse.Core.Services.IconMapping;
using SupportPulse.Data.Context;
using SupportPulse.Data.Entities.User.SupportCategory;
using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.Core.Services.Admin.SupportCategories
{
    /// <summary>
    /// Manages support categories – listing, creation, editing, and event dispatching.
    /// </summary>
    public class AdminSupportCategoryService : IAdminSupportCategoryService
    {
        #region Constructor & Dependencies

        private readonly ApplicationDbContext _db;
        private readonly IOperationResultAction _operation;
        private readonly IIconMappingService _iconMappingService;
        private readonly ICurrentAdminSession _adminSession;
        private readonly IAdminEventDispatcher _eventDispatcher;

        public AdminSupportCategoryService(
            ApplicationDbContext db,
            IOperationResultAction operation,
            IIconMappingService iconMappingService,
            ICurrentAdminSession adminSession,
            IAdminEventDispatcher eventDispatcher)
        {
            _db = db;
            _operation = operation;
            _iconMappingService = iconMappingService;
            _adminSession = adminSession;
            _eventDispatcher = eventDispatcher;
        }

        #endregion

        #region Query

        /// <inheritdoc />
        public async Task<List<SupportCategoryListDto>> GetSupportCategoryListAsync()
        {
            return await _db.SupportCategories
                .AsNoTracking()
                .Select(s => new SupportCategoryListDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    IsActive = s.IsActive,
                    UserCount = s.Users!.Count
                })
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<SupportCategoryForAssignToUserDto>> GetSupportCategoryListForAssignToUserAsync()
        {
            return await _db.SupportCategories
                .AsNoTracking()
                .Select(s => new SupportCategoryForAssignToUserDto
                {
                    Id = s.Id,
                    Details = s.Details,
                    Name = s.Name,
                    IconKey = s.IconKey,
                    IsActive = s.IsActive
                })
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<int>> GetUserSupportCategoryIdsAsync(int userId)
        {
            return await _db.UserSupportCategories
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .Select(s => s.SupportCategoryId)
                .ToListAsync();
        }

        #endregion

        #region Single Category Operations

        /// <inheritdoc />
        public async Task<OperationResult<EditSupportCategoryDto>> GetSupportCategoryForEditAsync(
            int supportCategoryId)
        {
            var supportCategory = await _db.SupportCategories
                .Where(r => r.Id == supportCategoryId)
                .Select(s => new EditSupportCategoryDto
                {
                    Id = s.Id,
                    Details = s.Details,
                    Name = s.Name,
                    IsActive = s.IsActive,
                    IconKey = s.IconKey,
                    Users = s.Users!.Select(us => new UsersSupportCategoriesDto
                    {
                        UserId = us.UserId,
                        FullName = us.User!.FullName,
                        UserName = us.User.UserName
                    }).ToList()
                })
                .SingleOrDefaultAsync();

            if (supportCategory is null)
            {
                return _operation.SendError<EditSupportCategoryDto>("واحد پشتیبانی مورد نظر یافت نشد");
            }

            return _operation.SendSuccess(entity: supportCategory);
        }

        #endregion

        #region Add / Edit

        /// <inheritdoc />
        public async Task<OperationResult> AddSupportCategoryAsync(AddSupportCategoryDto supportCategory)
        {
            try
            {
                // Validate the icon key against known mappings
                if (!string.IsNullOrWhiteSpace(supportCategory.IconKey) &&
                    _iconMappingService.GetAllIconMappings().All(a => a.IconKey != supportCategory.IconKey))
                {
                    supportCategory.IconKey = null;
                }

                SupportCategory add = new()
                {
                    Name = supportCategory.Name,
                    Details = supportCategory.Details,
                    IsActive = true,
                    IconKey = supportCategory.IconKey
                };

                await _db.SupportCategories.AddAsync(add);
                await _db.SaveChangesAsync();

                // Dispatch event – ID is now available after SaveChanges
                var addedDto = new SupportCategoryListDto
                {
                    Id = add.Id,
                    Name = add.Name,
                    IsActive = add.IsActive,
                    UserCount = 0
                };

                var context = new AdminEventContext
                {
                    EventType = AdminEventType.SupportCategoryCreated,
                    ActorAdminId = _adminSession.GetAdminId(),
                    ActorFullName = _adminSession.GetAdminFullName(),
                    ActorUserName = _adminSession.GetAdminUserName(),
                    SupportCategoryId = add.Id,
                    SupportCategoryName = add.Name,
                    DataSyncPayload = addedDto
                };

                await _eventDispatcher.DispatchAsync(context);
            }
            catch
            {
                return _operation.SendError();
            }

            return _operation.SendSuccess("واحد پشتیبانی با موفقیت افزوده شد.");
        }

        /// <inheritdoc />
        public async Task<OperationResult> EditSupportCategoryAsync(EditSupportCategoryDto supportCategory)
        {
            SupportCategory? supportCategoryInDb = await _db.SupportCategories.FindAsync(supportCategory.Id);
            if (supportCategoryInDb is null)
            {
                return _operation.SendError("واحد پشتیبانی مورد نظر یافت نشد");
            }

            // Validate icon key
            if (!string.IsNullOrWhiteSpace(supportCategory.IconKey) &&
                _iconMappingService.GetAllIconMappings().All(a => a.IconKey != supportCategory.IconKey))
            {
                supportCategory.IconKey = null;
            }

            supportCategoryInDb.Name = supportCategory.Name;
            supportCategoryInDb.Details = supportCategory.Details;
            supportCategoryInDb.IsActive = supportCategory.IsActive;
            supportCategoryInDb.IconKey = supportCategory.IconKey;

            _db.SupportCategories.Update(supportCategoryInDb);
            await _db.SaveChangesAsync();

            // Fetch lightweight user count for the DTO
            int userCount = await _db.UserSupportCategories
                .CountAsync(us => us.SupportCategoryId == supportCategory.Id);

            // Dispatch event
            var updatedDto = new SupportCategoryListDto
            {
                Id = supportCategoryInDb.Id,
                Name = supportCategoryInDb.Name,
                IsActive = supportCategoryInDb.IsActive,
                UserCount = userCount
            };

            var context = new AdminEventContext
            {
                EventType = AdminEventType.SupportCategoryEdited,
                ActorAdminId = _adminSession.GetAdminId(),
                ActorFullName = _adminSession.GetAdminFullName(),
                ActorUserName = _adminSession.GetAdminUserName(),
                SupportCategoryId = updatedDto.Id,
                SupportCategoryName = updatedDto.Name,
                DataSyncPayload = updatedDto
            };

            await _eventDispatcher.DispatchAsync(context);

            return _operation.SendSuccess("واحد پشتیبانی با موفقیت ویرایش شد");
        }

        #endregion
    }
}