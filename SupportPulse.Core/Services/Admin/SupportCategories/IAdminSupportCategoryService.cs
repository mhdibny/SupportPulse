#region Usings

using SupportPulse.Core.DTOs.Admin.SupportCategory;
using SupportPulse.Core.DTOs.Common;
using SupportPulse.Data.Enums.Admin;
#endregion

namespace SupportPulse.Core.Services.Admin.SupportCategories
{
    /// <summary>
    /// Defines operations for managing support categories by admin users.
    /// </summary>
    public interface IAdminSupportCategoryService
    {
        #region Query

        /// <summary>
        /// Returns a flat list of all support categories for the admin panel.
        /// </summary>
        Task<List<SupportCategoryListDto>> GetSupportCategoryListAsync();

        /// <summary>
        /// Returns a list of support categories formatted for user assignment.
        /// </summary>
        Task<List<SupportCategoryForAssignToUserDto>> GetSupportCategoryListForAssignToUserAsync();

        /// <summary>
        /// Returns the support category IDs assigned to a specific user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        Task<List<int>> GetUserSupportCategoryIdsAsync(int userId);

        #endregion

        #region Single Category Operations

        /// <summary>
        /// Retrieves detailed information about a support category for editing.
        /// </summary>
        /// <param name="supportCategoryId">The support category identifier.</param>
        Task<OperationResult<EditSupportCategoryDto>> GetSupportCategoryForEditAsync(int supportCategoryId);

        #endregion

        #region Add / Edit

        /// <summary>
        /// Creates a new support category and dispatches a <see cref="AdminEventType.SupportCategoryCreated"/> event.
        /// </summary>
        /// <param name="supportCategory">The support category data.</param>
        Task<OperationResult> AddSupportCategoryAsync(AddSupportCategoryDto supportCategory);

        /// <summary>
        /// Updates an existing support category and dispatches a <see cref="AdminEventType.SupportCategoryEdited"/> event.
        /// </summary>
        /// <param name="supportCategory">The updated support category data.</param>
        Task<OperationResult> EditSupportCategoryAsync(EditSupportCategoryDto supportCategory);

        #endregion
    }
}