namespace SupportPulse.Core.Services.FileAccess
{
    /// <summary>
    /// Defines functionality for checking whether a user is authorized to access a specific file.
    /// </summary>
    public interface IFileAccessService
    {
        /// <summary>
        /// Determines whether the specified user is allowed to download the file identified by its storage name.
        /// </summary>
        /// <param name="fileName">The unique storage name of the file (typically a GUID + extension).</param>
        /// <param name="userId">The identifier of the requesting user.</param>
        /// <returns><c>true</c> if the user has access; otherwise, <c>false</c>.</returns>
        Task<bool> CanAccessFileAsync(string fileName, int userId);
    }
}