#region Usings

using SupportPulse.Core.Services.Admin.Session;
using SupportPulse.Core.Utilities.ClaimsPrincipals;

#endregion

namespace SupportPulse.App.Session
{
    /// <summary>
    /// Provides the identity of the currently authenticated admin from the HTTP context.
    /// </summary>
    public class CurrentAdminSession : ICurrentAdminSession
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        #region Constructor & Dependencies

        public CurrentAdminSession(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        #endregion

        /// <inheritdoc />
        public int GetAdminId()
        {
            return _httpContextAccessor.HttpContext!.User.GetUserIdAsInt();
        }

        /// <inheritdoc />
        public string GetAdminFullName()
        {
            return _httpContextAccessor.HttpContext!.User.GetFullName() ?? "";
        }

        /// <inheritdoc />
        public string GetAdminUserName()
        {
            return _httpContextAccessor.HttpContext!.User.GetUserName() ?? "";
        }
    }
}