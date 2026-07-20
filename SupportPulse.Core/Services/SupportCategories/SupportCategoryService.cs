#region Usings

using Microsoft.EntityFrameworkCore;
using SupportPulse.Core.DTOs.User;
using SupportPulse.Data.Context;

#endregion

namespace SupportPulse.Core.Services.SupportCategories
{
    /// <summary>
    /// Provides support category data for users (e.g., creating new chats).
    /// </summary>
    public class SupportCategoryService : ISupportCategoryService
    {
        #region Constructor & Dependencies

        private readonly ApplicationDbContext _db;

        public SupportCategoryService(ApplicationDbContext db)
        {
            _db = db;
        }

        #endregion

        #region GetCategories

        /// <inheritdoc />
        public async Task<List<SupportCategoryDto>> GetCategoriesAsync()
        {
            return await _db.SupportCategories
                .AsNoTracking()
                .Where(s => s.IsActive)
                .Select(s => new SupportCategoryDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Details = s.Details,
                    IconKey = s.IconKey
                })
                .ToListAsync();
        }

        #endregion
    }
}