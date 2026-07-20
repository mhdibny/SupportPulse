#region Usings

using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#endregion

namespace SupportPulse.Core.Services.Admin.PermissionCache
{
    /// <summary>
    /// Implements the admin authorization cache with targeted, incremental updates.
    /// </summary>
    public class AdminPermissionCacheService : IAdminPermissionCacheService
    {
        #region Fields & Constructor

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AdminPermissionCacheService> _logger;

        // PermissionId → set of admin IDs
        private readonly ConcurrentDictionary<int, HashSet<int>> _permToAdmins = new();

        // SupportCategoryId → set of admin IDs
        private readonly ConcurrentDictionary<int, HashSet<int>> _catToAdmins = new();

        // Prevents concurrent rebuild operations
        private readonly SemaphoreSlim _rebuildLock = new(1, 1);

        public AdminPermissionCacheService(
            IServiceScopeFactory scopeFactory,
            ILogger<AdminPermissionCacheService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        #endregion

        #region Permission Cache

        /// <inheritdoc />
        public HashSet<int> GetAdminIdsByPermission(int permissionId)
        {
            return _permToAdmins.TryGetValue(permissionId, out var set)
                ? set
                : new HashSet<int>();
        }

        /// <inheritdoc />
        public void AddPermissionToUser(int userId, int permissionId)
        {
            _permToAdmins.AddOrUpdate(
                permissionId,
                _ => new HashSet<int> { userId },
                (_, set) => { lock (set) set.Add(userId); return set; });
        }

        /// <inheritdoc />
        public void RemovePermissionFromUser(int userId, int permissionId)
        {
            _permToAdmins.AddOrUpdate(
                permissionId,
                _ => new HashSet<int>(),
                (_, set) => { lock (set) set.Remove(userId); return set; });
        }

        /// <inheritdoc />
        public void AddPermissionsToUser(int userId, IEnumerable<int> permissionIds)
        {
            foreach (var permId in permissionIds)
                AddPermissionToUser(userId, permId);
        }

        /// <inheritdoc />
        public void RemovePermissionsFromUser(int userId, IEnumerable<int> permissionIds)
        {
            foreach (var permId in permissionIds)
                RemovePermissionFromUser(userId, permId);
        }

        #endregion

        #region Support Category Cache

        /// <inheritdoc />
        public HashSet<int> GetAdminIdsBySupportCategory(int supportCategoryId)
        {
            return _catToAdmins.TryGetValue(supportCategoryId, out var set)
                ? set
                : new HashSet<int>();
        }

        /// <inheritdoc />
        public void AddSupportCategoryToUser(int userId, int categoryId)
        {
            _catToAdmins.AddOrUpdate(
                categoryId,
                _ => new HashSet<int> { userId },
                (_, set) => { lock (set) set.Add(userId); return set; });
        }

        /// <inheritdoc />
        public void RemoveSupportCategoryFromUser(int userId, int categoryId)
        {
            _catToAdmins.AddOrUpdate(
                categoryId,
                _ => new HashSet<int>(),
                (_, set) => { lock (set) set.Remove(userId); return set; });
        }

        #endregion

        #region Rebuild

        /// <inheritdoc />
        public async Task RebuildAsync()
        {
            await _rebuildLock.WaitAsync();
            try
            {
                _permToAdmins.Clear();
                _catToAdmins.Clear();

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Data.Context.ApplicationDbContext>();

                // Rebuild permission → admins mapping
                var permData = await db.UserRoles
                    .Join(db.RolePermissions,
                        ur => ur.RoleId,
                        rp => rp.RoleId,
                        (ur, rp) => new { ur.UserId, rp.PermissionId })
                    .Distinct()
                    .ToListAsync();

                foreach (var item in permData)
                {
                    _permToAdmins.AddOrUpdate(
                        item.PermissionId,
                        _ => new HashSet<int> { item.UserId },
                        (_, set) => { lock (set) set.Add(item.UserId); return set; });
                }

                // Rebuild support category → admins mapping
                var catData = await db.UserSupportCategories
                    .Select(us => new { us.UserId, us.SupportCategoryId })
                    .ToListAsync();

                foreach (var item in catData)
                {
                    _catToAdmins.AddOrUpdate(
                        item.SupportCategoryId,
                        _ => new HashSet<int> { item.UserId },
                        (_, set) => { lock (set) set.Add(item.UserId); return set; });
                }

                _logger.LogInformation("AdminPermissionCacheService rebuilt successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rebuilding AdminPermissionCacheService.");
            }
            finally
            {
                _rebuildLock.Release();
            }
        }

        #endregion
    }
}