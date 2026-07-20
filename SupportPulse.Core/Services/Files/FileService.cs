#region Usings

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Collections.Frozen;
using SupportPulse.Core.DTOs.Message;

#endregion

namespace SupportPulse.Core.Services.Files
{
    /// <summary>
    /// Implements file upload functionality with configurable limits on size, count, and allowed extensions.
    /// </summary>
    public class FileService : IFileService
    {
        #region Static Allow Lists

        private static readonly FrozenSet<string> AllowedMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/bmp",
            "application/pdf", "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "text/plain", "application/zip", "application/x-rar-compressed",
            "text/csv"
        }.ToFrozenSet();

        #endregion

        #region Constructor & Dependencies

        private readonly IHostEnvironment _env;
        private readonly FrozenSet<string> _allowedExtensions;
        private readonly long _maxFileSize;
        private readonly int _maxFileCount;
        private readonly string _storageFolder;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileService"/> class.
        /// </summary>
        /// <param name="env">Host environment for determining the content root path.</param>
        /// <param name="configuration">Application configuration containing the "FileUpload" section.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="env"/> is null.</exception>
        public FileService(IHostEnvironment env, IConfiguration configuration)
        {
            _env = env ?? throw new ArgumentNullException(nameof(env));

            var fileUploadSection = configuration.GetSection("FileUpload");

            _maxFileSize = long.TryParse(fileUploadSection["MaxSizeBytes"], out var maxSize) ? maxSize : 10 * 1024 * 1024;
            _maxFileCount = int.TryParse(fileUploadSection["MaxFileCount"], out var maxCount) ? maxCount : 5;
            _storageFolder = fileUploadSection["StoragePath"] ?? "UploadedFiles";

            var extensionsString = fileUploadSection["AllowedExtensions"]
                                   ?? ".jpg,.jpeg,.png,.gif,.bmp,.pdf,.doc,.docx,.xls,.xlsx,.txt,.csv,.zip,.rar";

            _allowedExtensions = extensionsString
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLowerInvariant())
                .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region File Saving

        /// <inheritdoc />
        public async Task<List<AttachFileDto>> SaveFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default)
        {
            var fileList = files?.ToList() ?? new List<IFormFile>();

            if (fileList.Count == 0)
                throw new ArgumentException("At least one file must be provided.");

            if (fileList.Count > _maxFileCount)
                throw new InvalidOperationException($"Maximum number of files allowed is {_maxFileCount}.");

            var attachFileData = new List<AttachFileDto>();

            foreach (var file in fileList)
            {
                // Validate individual file
                if (file == null || file.Length == 0)
                    throw new InvalidOperationException("One of the provided files is invalid.");

                // Validate extension
                var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(extension) || !_allowedExtensions.Contains(extension))
                    throw new InvalidOperationException($"The format of '{file.FileName}' is not allowed.");

                // Validate MIME type
                if (!AllowedMimeTypes.Contains(file.ContentType))
                    throw new InvalidOperationException($"The content type of '{file.FileName}' is not acceptable.");

                // Validate file size
                if (file.Length > _maxFileSize)
                    throw new InvalidOperationException($"The size of '{file.FileName}' exceeds the maximum limit of {_maxFileSize / 1_048_576} MB.");

                // Ensure the storage directory exists
                var storageRoot = Path.Combine(_env.ContentRootPath, _storageFolder);
                if (!Directory.Exists(storageRoot))
                    Directory.CreateDirectory(storageRoot);

                // Generate a unique file name and persist the file
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(storageRoot, uniqueFileName);

                await using var fileStream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(fileStream, cancellationToken);

                attachFileData.Add(new AttachFileDto
                {
                    SavePath = uniqueFileName,
                    OriginalName = file.FileName
                });
            }

            return attachFileData;
        }

        #endregion
    }
}