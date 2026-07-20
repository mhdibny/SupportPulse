#region Usings

using Microsoft.EntityFrameworkCore;
using SupportPulse.Core.DTOs.Admin.Ban;
using SupportPulse.Core.DTOs.Admin.EventDispatcher;
using SupportPulse.Core.DTOs.Admin.User;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.Security.Password;
using SupportPulse.Core.Services.Admin.EventDispatcher;
using SupportPulse.Core.Services.Admin.Session;
using SupportPulse.Core.Services.Admin.Users;
using SupportPulse.Core.Services.Common;
using SupportPulse.Core.Utilities.Converters;
using SupportPulse.Data.Context;
using SupportPulse.Data.Entities.User;
using SupportPulse.Data.Entities.User.Ban;
using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.Core.Services.Admin.Ban
{
    /// <summary>
    /// Implements ban lifecycle management: history retrieval, banning, unbanning,
    /// expiry changes, and automatic unban by the system.
    /// </summary>
    public class BanService : IBanService
    {
        #region Constructor & Dependencies

        private readonly ApplicationDbContext _db;
        private readonly IOperationResultAction _operation;
        private readonly IAdminUserService _adminUserService;
        private readonly ICurrentAdminSession _adminSession;
        private readonly IAdminEventDispatcher _eventDispatcher;

        public BanService(
            ApplicationDbContext db,
            IOperationResultAction operation,
            IAdminUserService adminUserService,
            ICurrentAdminSession adminSession,
            IAdminEventDispatcher eventDispatcher)
        {
            _db = db;
            _operation = operation;
            _adminUserService = adminUserService;
            _adminSession = adminSession;
            _eventDispatcher = eventDispatcher;
        }

        #endregion

        #region Query

        /// <inheritdoc />
        public async Task<OperationResult<UserBanHistoryListDto>> GetUserBanHistoryListAsync(int userId)
        {
            UserInformationForBanDto? userInformation = await _adminUserService
                .GetUserInformationForBanAsync(userId);

            if (userInformation is null || userInformation.UserId == 0)
            {
                return _operation.SendError<UserBanHistoryListDto>(
                    "کاربر مورد نظر یافت نشد.", OperationStatus.ValidationError);
            }

            List<UserBanHistoryDto> userBanList = await _db.UserBanHistories
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .Select(s => new UserBanHistoryDto
                {
                    Id = s.Id,
                    Action = s.Action,
                    ActionDate = s.ActionDate.ToShamsiWithTime(),
                    BanExpiryDate = s.BanExpiry.ToShamsiWithTime(),
                    BannedByAdminId = s.BannedByAdminId,
                    BannedByAdminUserName = s.Admin!.UserName ?? "System",
                    Reason = s.Reason
                })
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            var banHistoryResult = new UserBanHistoryListDto
            {
                BanHistories = userBanList,
                UserInformation = userInformation
            };

            return _operation.SendSuccess(entity: banHistoryResult);
        }

        #endregion

        #region Ban

        /// <inheritdoc />
        public async Task<OperationResult> BanUserAsync(BanUserDto ban, int adminId)
        {
            User? userInDb = await _adminUserService.GetUserByIdAsync(ban.UserId);
            if (userInDb is null || userInDb.Id == 0)
            {
                return _operation.SendError("کاربر مورد نظر یافت نشد");
            }

            DateTime? banExpiry = ban.BanExpiryDate.ShamsiToMiladi();
            if (banExpiry.HasValue && DateTime.Now.AddMinutes(1) >= banExpiry.Value)
            {
                return _operation.SendError(
                    "زمان مسدودیت باید حداقل ۱ دقیقه بعد از زمان فعلی باشد.",
                    OperationStatus.ValidationError);
            }

            if (userInDb.IsBan)
            {
                return _operation.SendError("این کاربر از قبل مسدود شده است.",
                    OperationStatus.ValidationError);
            }

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                UserBanHistory banHistory = new()
                {
                    UserId = userInDb.Id,
                    BannedByAdminId = adminId,
                    Action = "Ban",
                    BanExpiry = banExpiry,
                    Reason = ban.Reason
                };
                await _db.UserBanHistories.AddAsync(banHistory);

                userInDb.IsBan = true;
                userInDb.BanExpiry = banHistory.BanExpiry;

                // Invalidate all existing sessions
                userInDb.SecurityStamp = SecurityTool.GenerateSecurityStamp();
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // Dispatch UserBanned event
                var userDto = new UserListDto
                {
                    Id = userInDb.Id,
                    UserName = userInDb.UserName,
                    FirstName = userInDb.FirstName,
                    LastName = userInDb.LastName,
                    IsBanned = true,
                    IsAdmin = userInDb.Roles != null && userInDb.Roles.Any(),
                    RoleCount = userInDb.Roles?.Count ?? 0
                };

                var context = new AdminEventContext
                {
                    EventType = AdminEventType.UserBanned,
                    ActorAdminId = adminId,
                    ActorFullName = _adminSession.GetAdminFullName(),
                    ActorUserName = _adminSession.GetAdminUserName(),
                    TargetUserId = userInDb.Id,
                    TargetFullName = userInDb.FullName,
                    TargetUserName = userInDb.UserName,
                    DataSyncPayload = userDto
                };

                await _eventDispatcher.DispatchAsync(context);

                return _operation.SendSuccess($"کاربر {userInDb.FullName} با موفقیت مسدود شد.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return _operation.SendError("هنگام مسدود کردن کاربر خطایی رخ داد، لطفا مجدد تلاش کنید.");
            }
        }

        #endregion

        #region Unban

        /// <inheritdoc />
        public async Task<OperationResult> UnBanUserAsync(UnBanUserDto unBan, int adminId)
        {
            User? userInDb = await _adminUserService.GetUserByIdAsync(unBan.UserId);
            if (userInDb is null || userInDb.Id == 0)
            {
                return _operation.SendError("کاربر مورد نظر یافت نشد");
            }

            if (!userInDb.IsBan)
            {
                return _operation.SendError("این کاربر در حال حاضر مسدود نیست.", OperationStatus.Info);
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                UserBanHistory unBanHistory = new()
                {
                    UserId = userInDb.Id,
                    BannedByAdminId = adminId,
                    Action = "UnBan",
                    Reason = unBan.Reason
                };
                await _db.UserBanHistories.AddAsync(unBanHistory);

                userInDb.IsBan = false;
                userInDb.BanExpiry = null;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // Dispatch UserUnbanned event
                var userDto = new UserListDto
                {
                    Id = userInDb.Id,
                    UserName = userInDb.UserName,
                    FirstName = userInDb.FirstName,
                    LastName = userInDb.LastName,
                    IsBanned = false,
                    IsAdmin = userInDb.Roles != null && userInDb.Roles.Any(),
                    RoleCount = userInDb.Roles?.Count ?? 0
                };

                var context = new AdminEventContext
                {
                    EventType = AdminEventType.UserUnbanned,
                    ActorAdminId = adminId,
                    ActorFullName = _adminSession.GetAdminFullName(),
                    ActorUserName = _adminSession.GetAdminUserName(),
                    TargetUserId = userInDb.Id,
                    TargetFullName = userInDb.FullName,
                    TargetUserName = userInDb.UserName,
                    DataSyncPayload = userDto
                };

                await _eventDispatcher.DispatchAsync(context);

                return _operation.SendSuccess($"کاربر {userInDb.FullName} با موفقیت از حالت مسدودی خارج شد.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return _operation.SendError("هنگام خارج کردن کاربر از مسدودی خطایی رخ داد، لطفا مجدد تلاش کنید.");
            }
        }

        #endregion

        #region Change Ban Expiry

        /// <inheritdoc />
        public async Task<OperationResult> ChangeUserBanExpiryAsync(ChangeBanExpiryTimeDto changeBan, int adminId)
        {
            User? userInDb = await _adminUserService.GetUserByIdAsync(changeBan.UserId);
            if (userInDb is null || userInDb.Id == 0)
            {
                return _operation.SendError("کاربر مورد نظر یافت نشد");
            }

            DateTime? newBanExpiry = changeBan.BanExpiryDate.ShamsiToMiladi();

            if (newBanExpiry.HasValue && DateTime.Now.AddMinutes(1) >= newBanExpiry.Value)
            {
                return _operation.SendError(
                    "لطفا زمان مسدود بودن کاربر را بیش از یک دقیقه وارد کنید",
                    OperationStatus.ValidationError);
            }

            if (newBanExpiry == userInDb.BanExpiry)
            {
                return _operation.SendError(
                    "برای تغییر مدت زمان مسدود بودن کاربر، باید مدت زمان جدید از قبلی متفاوت باشد.",
                    OperationStatus.ValidationError);
            }

            if (!userInDb.IsBan)
            {
                return _operation.SendError("این کاربر در حال حاضر مسدود نیست.", OperationStatus.Info);
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                UserBanHistory changeBanHistory = new()
                {
                    UserId = userInDb.Id,
                    BannedByAdminId = adminId,
                    Action = "Change",
                    Reason = changeBan.Reason,
                    BanExpiry = newBanExpiry,
                };

                await _db.UserBanHistories.AddAsync(changeBanHistory);

                string changeMessage =
                    $"از {userInDb.BanExpiry.ToShamsiWithTime() ?? "مسدودی دائم"} " +
                    $"به {changeBanHistory.BanExpiry.ToShamsiWithTime() ?? "مسدودی دائم"}";

                userInDb.IsBan = true;
                userInDb.BanExpiry = changeBanHistory.BanExpiry;
                userInDb.SecurityStamp = SecurityTool.GenerateSecurityStamp();

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // Dispatch UserBanExpiryChanged event
                var userDto = new UserListDto
                {
                    Id = userInDb.Id,
                    UserName = userInDb.UserName,
                    FirstName = userInDb.FirstName,
                    LastName = userInDb.LastName,
                    IsBanned = true,
                    IsAdmin = userInDb.Roles != null && userInDb.Roles.Any(),
                    RoleCount = userInDb.Roles?.Count ?? 0
                };

                var context = new AdminEventContext
                {
                    EventType = AdminEventType.UserBanExpiryChanged,
                    ActorAdminId = adminId,
                    ActorFullName = _adminSession.GetAdminFullName(),
                    ActorUserName = _adminSession.GetAdminUserName(),
                    TargetUserId = userInDb.Id,
                    TargetFullName = userInDb.FullName,
                    TargetUserName = userInDb.UserName,
                    DataSyncPayload = userDto
                };

                await _eventDispatcher.DispatchAsync(context);

                return _operation.SendSuccess($"وضعیت مسدودی کاربر {userInDb.FullName} {changeMessage} تغییر کرد.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return _operation.SendError("هنگام خارج کردن کاربر از مسدودی خطایی رخ داد، لطفا مجدد تلاش کنید.");
            }
        }

        #endregion

        #region System Auto‑Unban

        /// <inheritdoc />
        public async Task<bool> UnBanUserBySystemAsync(int userId)
        {
            User? userInDb = await _adminUserService.GetUserByIdAsync(userId);
            if (userInDb is null)
            {
                return false;
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                userInDb.IsBan = false;
                userInDb.BanExpiry = null;

                UserBanHistory newUnBanHistory = new()
                {
                    Action = "UnBan",
                    Reason = "مدت زمان مسدودی کاربر به پایان رسید و کاربر رفع مسدودی شد.",
                    UserId = userInDb.Id
                };

                await _db.UserBanHistories.AddAsync(newUnBanHistory);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        #endregion
    }
}