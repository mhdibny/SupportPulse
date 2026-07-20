namespace SupportPulse.Core.Services.Admin.PermissionCache
{
    /// <summary>
    /// Provides an ultra‑fast, in‑memory cache that maps permissions and support categories
    /// to sets of admin user IDs. It is updated incrementally when roles or user assignments change.
    /// </summary>
    public interface IAdminPermissionCacheService
    {
        /// <summary>
        /// Returns the set of admin IDs that hold the specified permission.
        /// </summary>
        /// <param name="permissionId">The permission identifier.</param>
        HashSet<int> GetAdminIdsByPermission(int permissionId);

        /// <summary>
        /// Returns the set of admin IDs that are members of the specified support category.
        /// </summary>
        /// <param name="supportCategoryId">The support category identifier.</param>
        HashSet<int> GetAdminIdsBySupportCategory(int supportCategoryId);

        /// <summary>
        /// Adds a single permission to a user in the cache.
        /// </summary>
        void AddPermissionToUser(int userId, int permissionId);

        /// <summary>
        /// Removes a single permission from a user in the cache.
        /// </summary>
        void RemovePermissionFromUser(int userId, int permissionId);

        /// <summary>
        /// Adds multiple permissions to a user in the cache.
        /// </summary>
        void AddPermissionsToUser(int userId, IEnumerable<int> permissionIds);

        /// <summary>
        /// Removes multiple permissions from a user in the cache.
        /// </summary>
        void RemovePermissionsFromUser(int userId, IEnumerable<int> permissionIds);

        /// <summary>
        /// Adds a support category to a user in the cache.
        /// </summary>
        void AddSupportCategoryToUser(int userId, int categoryId);

        /// <summary>
        /// Removes a support category from a user in the cache.
        /// </summary>
        void RemoveSupportCategoryFromUser(int userId, int categoryId);

        /// <summary>
        /// Fully rebuilds the cache from the database. Used as a fallback after an error.
        /// </summary>
        Task RebuildAsync();
    }
}