#region Usings

using Microsoft.AspNetCore.Http;
using SupportPulse.Core.DTOs.Message;

#endregion

namespace SupportPulse.Core.Services.Files
{
    /// <summary>
    /// Defines operations for validating and persisting uploaded files to disk.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Validates a collection of uploaded files and saves them to the configured storage folder.
        /// </summary>
        /// <param name="files">The collection of uploaded files.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A list of <see cref="AttachFileDto"/> containing metadata for the saved files.</returns>
        Task<List<AttachFileDto>> SaveFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default);
    }
}