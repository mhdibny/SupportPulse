#region Usings

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportPulse.Core.Services.FileAccess;
using SupportPulse.Core.Utilities.ClaimsPrincipals;

#endregion

namespace SupportPulse.App.Controllers.Download
{
    /// <summary>
    /// Handles secure file downloads with path‑traversal protection and access control.
    /// </summary>
    [Authorize]
    [Route("download")]
    public class DownloadController : Controller
    {
        #region Constructor & Dependencies

        private readonly IWebHostEnvironment _env;
        private readonly IFileAccessService _fileAccessService;
        private readonly string _storageFolder;

        public DownloadController(
            IWebHostEnvironment env,
            IConfiguration configuration,
            IFileAccessService fileAccessService)
        {
            _env = env;
            _fileAccessService = fileAccessService;
            _storageFolder = configuration.GetValue<string>("FileUpload:StoragePath") ?? "UploadedFiles";
        }

        #endregion

        #region Download

        /// <summary>
        /// Serves a file from the secure uploads folder after validating the filename,
        /// preventing path traversal, and checking user access.
        /// </summary>
        /// <param name="fileName">The stored file name (GUID + extension).</param>
        /// <param name="originalName">Optional original file name to use in the download dialog.</param>
        [HttpGet("{fileName}")]
        public async Task<IActionResult> Download(string fileName, [FromQuery] string? originalName)
        {
            // 1. Basic validation
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("نام فایل نامعتبر است.");

            // 2. Prevent path traversal
            if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
                return BadRequest("نام فایل معتبر نیست.");

            // 3. Build the full file path
            var storageRoot = Path.Combine(_env.ContentRootPath, _storageFolder);
            var filePath = Path.Combine(storageRoot, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            // 4. Check user access
            var canAccess = await _fileAccessService.CanAccessFileAsync(fileName, User.GetUserIdAsInt());
            if (!canAccess)
                return NotFound();

            // 5. Determine the download name and MIME type
            var downloadName = string.IsNullOrWhiteSpace(originalName) ? fileName : originalName;
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var mimeType = GetMimeType(extension);

            // 6. Set security and caching headers
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Cache-Control"] = "private, max-age=3600";

            // 7. Return the file
            return PhysicalFile(filePath, mimeType, downloadName);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Maps a file extension to its corresponding MIME type.
        /// </summary>
        /// <param name="extension">The file extension (including the dot).</param>
        private static string GetMimeType(string extension) => extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            _ => "application/octet-stream"
        };

        #endregion
    }
}