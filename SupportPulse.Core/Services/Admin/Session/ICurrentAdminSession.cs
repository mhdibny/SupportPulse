namespace SupportPulse.Core.Services.Admin.Session
{
    /// <summary>
    /// Provides access to the currently authenticated admin's identity information.
    /// </summary>
    public interface ICurrentAdminSession
    {
        /// <summary>
        /// Returns the unique identifier of the current admin.
        /// </summary>
        int GetAdminId();

        /// <summary>
        /// Returns the full name of the current admin.
        /// </summary>
        string GetAdminFullName();

        /// <summary>
        /// Returns the username of the current admin.
        /// </summary>
        string GetAdminUserName();
    }
}