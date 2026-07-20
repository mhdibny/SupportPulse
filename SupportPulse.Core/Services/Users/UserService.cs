#region Usings

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Core.DTOs.User;
using SupportPulse.Core.Security.Password;
using SupportPulse.Core.Services.Admin.Ban;
using SupportPulse.Core.Services.Common;
using SupportPulse.Core.Utilities.Converters;
using SupportPulse.Core.ViewModels.User;
using SupportPulse.Data.Context;
using SupportPulse.Data.Entities.User;

#endregion

namespace SupportPulse.Core.Services.Users
{
    /// <summary>
    /// Handles user authentication, registration, and security stamp validation.
    /// </summary>
    public class UserService : IUserService
    {
        #region Constructor & Dependencies

        private readonly ApplicationDbContext _db;
        private readonly PasswordHasher _passwordHasher;
        private readonly IOperationResultAction _operation;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _memoryCache;
        private readonly IBanService _banService;

        public UserService(
            ApplicationDbContext db,
            PasswordHasher passwordHasher,
            IOperationResultAction operation,
            IMapper mapper,
            IMemoryCache memoryCache,
            IBanService banService)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _operation = operation;
            _mapper = mapper;
            _memoryCache = memoryCache;
            _banService = banService;
        }

        #endregion

        #region Sign Up

        /// <inheritdoc />
        public async Task<OperationResult<UserForLoginDto>> SignUpUserAsync(SignUpUserVM user)
        {
            var validationResult = await ValidateUniqueUserName(user.UserName);
            if (validationResult is not null)
                return validationResult;

            User registerUser = _mapper.Map<User>(user);
            registerUser.Password = _passwordHasher.HashPassword(user.Password);
            registerUser.SecurityStamp = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

            User? signUpResult = await AddUser(registerUser);
            if (signUpResult?.Id is not 0)
            {
                UserForLoginDto successDto = new(
                    signUpResult.Id,
                    signUpResult.UserName,
                    signUpResult.FullName,
                    signUpResult.SecurityStamp,
                    signUpResult.IsBan,
                    signUpResult.BanExpiry);

                return _operation.SendSuccess(
                    "ثبت نام با موفقیت انجام شد.",
                    entity: successDto);
            }

            return _operation.SendError<UserForLoginDto>();
        }

        #endregion

        #region Login

        /// <inheritdoc />
        public async Task<OperationResult<UserForLoginDto>> LoginUserAsync(LoginUserVM user)
        {
            var userData = await _db.Users
                .Where(u => u.UserName.ToLower() == user.UserName.ToLower())
                .Select(u => new UserLoginValidationData(
                    u.Id,
                    u.UserName,
                    u.FullName,
                    u.SecurityStamp,
                    u.Password,
                    u.IsBan,
                    u.BanExpiry
                ))
                .SingleOrDefaultAsync();

            if (userData is null || !_passwordHasher.VerifyPassword(user.Password, userData.PasswordHash))
            {
                return _operation.SendError<UserForLoginDto>(
                    "کاربری با مشخصات وارد شده یافت نشد.");
            }

            if (userData.IsBan)
            {
                // Ban with expiry – lift it automatically if expired
                if (userData.BanExpiry.HasValue && userData.BanExpiry <= DateTime.Now)
                {
                    bool unBanUserResult = await _banService.UnBanUserBySystemAsync(userData.Id);
                    if (unBanUserResult is false)
                    {
                        return _operation.SendError<UserForLoginDto>(
                            "هنگام ورود خطایی رخ داد، لطفا مجدد تلاش کنید.");
                    }
                }
                else
                {
                    // Permanent ban or still‑active temporary ban
                    if (userData.BanExpiry.HasValue is false)
                    {
                        return _operation.SendError<UserForLoginDto>(
                            "کاربر گرامی، حساب کاربری شما به صورت دائمی، مسدود شده است.");
                    }
                    else
                    {
                        string? remaining = userData.BanExpiry.GetDifferenceFromNow();
                        if (remaining is null)
                        {
                            return _operation.SendError<UserForLoginDto>(
                                "کاربر گرامی حساب کاربری شما به صورت موقت مسدود است.");
                        }

                        return _operation.SendError<UserForLoginDto>(
                            $"کاربر گرامی حساب کاربری شما تا {remaining} دیگر مسدود است. بعد از پایان مدت مسدودی، میتوانید به سایت وارد شوید.");
                    }
                }
            }

            var userForLogin = new UserForLoginDto(
                userData.Id,
                userData.UserName,
                userData.FullName,
                userData.SecurityStamp,
                userData.IsBan,
                userData.BanExpiry,
                user.RememberMe);

            return _operation.SendSuccess(entity: userForLogin);
        }

        #endregion

        #region User Information

        /// <inheritdoc />
        public async Task<UserPanelInformationDto?> GetUserPanelInformationAsync(int userId)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(r => r.Id == userId)
                .Select(s => new UserPanelInformationDto()
                {
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    UserName = s.UserName
                }).SingleOrDefaultAsync();
        }


        #endregion

        #region Support Category Receivers

        /// <inheritdoc />
        public async Task<List<string>> GetSupportCategoryReceiverUserIdListAsync(int supportCategoryId)
        {
            string cacheKey = $"support_category_receivers_{supportCategoryId}";
            if (!_memoryCache.TryGetValue(cacheKey, out List<string> receivers))
            {
                receivers = await _db.UserSupportCategories
                    .AsNoTracking()
                    .Where(usc => usc.SupportCategoryId == supportCategoryId)
                    .Select(s => s.UserId.ToString())
                    .ToListAsync();

                _memoryCache.Set(cacheKey, receivers, TimeSpan.FromMinutes(60));
            }

            return receivers;
        }

        /// <inheritdoc />
        public async Task<List<int>> GetSupportCategoryReceiverUserIdListAsIntAsync(int supportCategoryId)
        {
            string cacheKey = $"support_category_receivers_int_{supportCategoryId}";
            if (!_memoryCache.TryGetValue(cacheKey, out List<int> receivers))
            {
                receivers = await _db.UserSupportCategories
                    .AsNoTracking()
                    .Where(usc => usc.SupportCategoryId == supportCategoryId)
                    .Select(s => s.UserId)
                    .ToListAsync();

                _memoryCache.Set(cacheKey, receivers, TimeSpan.FromMinutes(60));
            }

            return receivers;
        }

        #endregion

        #region Admin Check & User Retrieval

        /// <inheritdoc />
        public async Task<bool> IsAdminAsync(int userId)
        {
            return await _db.UserSupportCategories
                .AsNoTracking()
                .AnyAsync(a => a.UserId == userId);
        }

        /// <inheritdoc />
        public async Task<UserForLoginDto?> GetUserForLoginByIdAsync(int userId)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(s => new UserForLoginDto(
                    s.Id,
                    s.UserName,
                    s.FullName,
                    s.SecurityStamp,
                    s.IsBan,
                    s.BanExpiry,
                    false))
                .SingleOrDefaultAsync();
        }

        #endregion

        #region Security Stamp

        /// <inheritdoc />
        public async Task UpdateSecurityStampAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.SecurityStamp = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                _db.Users.Update(user);
                await _db.SaveChangesAsync();
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsSecurityStampValidAsync(int userId, string stamp)
        {
            string? userSecurityStampInDb = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(s => s.SecurityStamp)
                .SingleOrDefaultAsync();

            return !string.IsNullOrWhiteSpace(userSecurityStampInDb) && userSecurityStampInDb == stamp;
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Returns an error result if the username is already taken; otherwise <c>null</c>.
        /// </summary>
        private async Task<OperationResult<UserForLoginDto>?> ValidateUniqueUserName(string userName)
        {
            if (await IsExistingUsername(userName))
            {
                return _operation.SendError<UserForLoginDto>(
                    "لطفا از نام کاربری دیگری استفاده کنید.",
                    status: OperationStatus.ValidationError);
            }

            return null;
        }

        private async Task<bool> IsExistingUsername(string userName)
        {
            return await _db.Users.AnyAsync(a => a.UserName.ToLower() == userName.ToLower());
        }

        private async Task<User?> AddUser(User user)
        {
            try
            {
                await _db.Users.AddAsync(user);
                await _db.SaveChangesAsync();
                return user;
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}