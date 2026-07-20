#region Usings

using Microsoft.EntityFrameworkCore;
using SupportPulse.Data.Context;

#endregion

namespace SupportPulse.Core.Services.FileAccess
{
    /// <summary>
    /// Implements file access control by checking multiple relationship paths
    /// between the user and the file's originating chat.
    /// </summary>
    public class FileAccessService : IFileAccessService
    {
        #region Constructor & Dependencies

        private readonly ApplicationDbContext _db;

        public FileAccessService(ApplicationDbContext db)
        {
            _db = db;
        }

        #endregion

        #region Access Check

        /// <inheritdoc />
        public async Task<bool> CanAccessFileAsync(string fileName, int userId)
        {
            return await _db.AttachFiles
                .AsNoTracking()
                .AnyAsync(a =>
                    a.SavedPath == fileName &&
                    (
                        // 1. The user is the original sender of the file (regular user or admin).
                        a.MessageContent!.Message!.SenderId == userId
                        ||
                        // 2. The user is the creator of the chat (can view files sent by an admin).
                        a.MessageContent.Message.Chat!.CreatorId == userId
                        ||
                        // 3. The user is the admin who currently locked the chat.
                        a.MessageContent.Message.Chat.LockedByAdminId == userId
                        ||
                        // 4. The chat is unlocked and the user is an admin assigned to the same support category.
                        (a.MessageContent.Message.Chat.LockedByAdminId == null &&
                         _db.UserSupportCategories.Any(usc =>
                             usc.SupportCategoryId == a.MessageContent.Message.Chat.SupportCategoryId
                             && usc.UserId == userId))
                    ));
        }

        #endregion
    }
}