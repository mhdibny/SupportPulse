#region Usings

using SupportPulse.Core.DTOs.User;

#endregion

namespace SupportPulse.Core.Services.SupportCategories
{
    /// <summary>
    /// Defines operations for retrieving active support categories.
    /// </summary>
    public interface ISupportCategoryService
    {
        /// <summary>
        /// Returns all active support categories.
        /// </summary>
        Task<List<SupportCategoryDto>> GetCategoriesAsync();
    }
}