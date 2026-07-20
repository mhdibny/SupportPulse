#region Usings

using Microsoft.EntityFrameworkCore;
using SupportPulse.Data.Entities.Chat;
using SupportPulse.Data.Entities.Chat.Message;
using SupportPulse.Data.Entities.Chat.Message.MessageContent;
using SupportPulse.Data.Entities.User;
using SupportPulse.Data.Entities.User.Ban;
using SupportPulse.Data.Entities.User.Notification;
using SupportPulse.Data.Entities.User.RefreshToken;
using SupportPulse.Data.Entities.User.Role;
using SupportPulse.Data.Entities.User.Role.Permission;
using SupportPulse.Data.Entities.User.SupportCategory;

#endregion

namespace SupportPulse.Data.Context
{
    /// <summary>
    /// Main database context for the SupportUnit application.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        #region DbSets

        public DbSet<User> Users { get; set; }
        public DbSet<SupportCategory> SupportCategories { get; set; }
        public DbSet<UserSupportCategory> UserSupportCategories { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageContent> MessageContents { get; set; }
        public DbSet<MessageType> MessageTypes { get; set; }
        public DbSet<ChatStatus> ChatStatus { get; set; }
        public DbSet<AttachFile> AttachFiles { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserBanHistory> UserBanHistories { get; set; }
        public DbSet<AdminNotification> AdminNotifications { get; set; }

        #endregion

        #region Model Configuration

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureCompositeKeys(modelBuilder);
            ConfigureRelationships(modelBuilder);
            SeedData(modelBuilder);
            CreateIndexes(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        #endregion

        #region Composite Keys

        private static void ConfigureCompositeKeys(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserSupportCategory>()
                .HasKey(usc => new { usc.UserId, usc.SupportCategoryId });

            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });
        }

        #endregion

        #region Relationships

        private static void ConfigureRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Chat>(entity =>
            {
                entity.HasOne(c => c.Creator)
                    .WithMany(u => u.Chats)
                    .HasForeignKey(c => c.CreatorId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(c => c.LockedByAdmin)
                    .WithMany(u => u.LockedChats)
                    .HasForeignKey(c => c.LockedByAdminId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<UserBanHistory>(entity =>
            {
                entity.HasOne(h => h.User)
                    .WithMany(u => u.BanHistories)
                    .HasForeignKey(h => h.UserId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired();

                entity.HasOne(h => h.Admin)
                    .WithMany(u => u.BansIssued)
                    .HasForeignKey(h => h.BannedByAdminId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<AdminNotification>(entity =>
            {
                entity.HasOne(n => n.AdminUser)
                    .WithMany(u => u.AdminNotifications)
                    .HasForeignKey(n => n.AdminUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }

        #endregion

        #region Seed Data

        private static void SeedData(ModelBuilder modelBuilder)
        {
            // Message types
            modelBuilder.Entity<MessageType>().HasData(
                new MessageType { Id = 1, Name = "متن" },
                new MessageType { Id = 2, Name = "فایل" },
                new MessageType { Id = 3, Name = "متن و فایل" }
            );

            // Chat statuses
            modelBuilder.Entity<ChatStatus>().HasData(
                new ChatStatus { Id = 1, Name = "گفتگو تکمیل شد." },
                new ChatStatus { Id = 2, Name = "درحال پاسخگویی" }
            );

            // Default support categories
            modelBuilder.Entity<SupportCategory>().HasData(
                new SupportCategory { Id = 1, Name = "واحد فنی", Details = "توضیحاتی مختصر درمورد واحد فنی", IsActive = true, IconKey = "technical" },
                new SupportCategory { Id = 2, Name = "واحد فروش", Details = "توضیحاتی مختصر درمورد واحد فروش", IsActive = true, IconKey = "sales" },
                new SupportCategory { Id = 3, Name = "واحد مدیریت", Details = "توضیحاتی مختصر درمورد واحد مدیریت", IsActive = true, IconKey = "management" }
            );

            // Permissions (General + Notification)
            modelBuilder.Entity<Permission>().HasData(
                new Permission { Id = 1, Name = "داشبورد پنل ادمین", Category = "General" },
                new Permission { Id = 2, Name = "لیست نقش‌ها", Category = "General" },
                new Permission { Id = 3, Name = "افزودن نقش", Category = "General" },
                new Permission { Id = 4, Name = "ویرایش نقش", Category = "General" },
                new Permission { Id = 5, Name = "حذف نقش", Category = "General" },
                new Permission { Id = 6, Name = "لیست کاربران", Category = "General" },
                new Permission { Id = 7, Name = "مسدود کردن کاربر", Category = "General" },
                new Permission { Id = 8, Name = "رفع مسدودیت کاربر", Category = "General" },
                new Permission { Id = 9, Name = "تغییر مدت مسدودیت", Category = "General" },
                new Permission { Id = 10, Name = "مشاهده تاریخچه مسدودیت", Category = "General" },
                new Permission { Id = 11, Name = "لیست واحدهای پشتیبانی", Category = "General" },
                new Permission { Id = 12, Name = "افزودن واحد پشتیبانی", Category = "General" },
                new Permission { Id = 13, Name = "ویرایش واحد پشتیبانی", Category = "General" },
                new Permission { Id = 14, Name = "مشاهده لیست چت‌ها", Category = "General" },
                new Permission { Id = 15, Name = "مشاهده جزئیات چت", Category = "General" },
                new Permission { Id = 16, Name = "قفل کردن چت", Category = "General" },
                new Permission { Id = 17, Name = "آزادسازی چت", Category = "General" },
                new Permission { Id = 18, Name = "پایان دادن به چت", Category = "General" },
                new Permission { Id = 19, Name = "ارسال پیام در چت", Category = "General" },
                new Permission { Id = 20, Name = "تخصیص نقش به کاربر", Category = "General" },
                new Permission { Id = 21, Name = "تخصیص واحد پشتیبانی به کاربر", Category = "General" },

                new Permission { Id = 101, Name = "اعلان آزادسازی چت", Category = "Notification" },
                new Permission { Id = 102, Name = "اعلان قفل شدن چت", Category = "Notification" },
                new Permission { Id = 103, Name = "اعلان پایان چت", Category = "Notification" },
                new Permission { Id = 104, Name = "اعلان مسدود شدن کاربر", Category = "Notification" },
                new Permission { Id = 105, Name = "اعلان رفع مسدودی کاربر", Category = "Notification" },
                new Permission { Id = 106, Name = "اعلان تغییر مدت مسدودی", Category = "Notification" },
                new Permission { Id = 107, Name = "اعلان ایجاد نقش", Category = "Notification" },
                new Permission { Id = 108, Name = "اعلان ویرایش نقش", Category = "Notification" },
                new Permission { Id = 109, Name = "اعلان حذف نقش", Category = "Notification" },
                new Permission { Id = 110, Name = "اعلان ایجاد واحد پشتیبانی", Category = "Notification" },
                new Permission { Id = 111, Name = "اعلان ویرایش واحد پشتیبانی", Category = "Notification" },
                new Permission { Id = 112, Name = "اعلان آزادسازی خودکار چت", Category = "Notification" },
                new Permission { Id = 113, Name = "اعلان تغییر نقش‌های کاربر", Category = "Notification" },
                new Permission { Id = 114, Name = "اعلان تغییر واحدهای پشتیبانی کاربر", Category = "Notification" }
            );

            // Super admin role
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "مدیر کل" }
            );

            // Assign all permissions to the super admin role
            for (int i = 1; i <= 21; i++)
            {
                modelBuilder.Entity<RolePermission>().HasData(
                    new RolePermission { RoleId = 1, PermissionId = i }
                );
            }
            for (int i = 101; i <= 114; i++)
            {
                modelBuilder.Entity<RolePermission>().HasData(
                    new RolePermission { RoleId = 1, PermissionId = i }
                );
            }

            // Default admin user (password is pre‑hashed)
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    UserName = "manager",
                    FirstName = "ادمین",
                    LastName = "اصلی",
                    Password = "Argon2id$mem=65536;iter=3;par=4$ABX4d3wa6rdwM4SsxCjB0A==$dDUptPckroNuD0JtMyHqjV+ZMifgKTtHHxcvJN61cmc=", //manager
                    SecurityStamp = "68b6608745fc42f6b03bfa0ad68ac339d5ef90fbeee549a7833a1c985aae7d91",
                    IsBan = false
                }
            );

            // Assign super admin role to the default user
            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { UserId = 1, RoleId = 1 }
                );

            // Give the super admin permission to reply on behalf of all support units.
            modelBuilder.Entity<UserSupportCategory>().HasData(
                new UserSupportCategory { UserId = 1, SupportCategoryId = 1 },
                new UserSupportCategory { UserId = 1, SupportCategoryId = 2 },
                new UserSupportCategory { UserId = 1, SupportCategoryId = 3 }
                );
        }

        #endregion

        #region Indexes

        private static void CreateIndexes(ModelBuilder modelBuilder)
        {
            // Attach file lookup
            modelBuilder.Entity<AttachFile>(entity =>
            {
                entity.HasIndex(e => e.SavedPath)
                    .HasDatabaseName("IX_AttachFiles_SavedPath")
                    .IncludeProperties(e => e.MessageContentId);
            });

            // User‑support category membership
            modelBuilder.Entity<UserSupportCategory>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.SupportCategoryId })
                    .HasDatabaseName("IX_UserSupportCategories_UserId_SupportCategoryId");
            });

            // Chat lookup by unique ID and creator
            modelBuilder.Entity<Chat>(entity =>
            {
                entity.HasIndex(e => new { e.ChatUniqId, e.CreatorId })
                    .HasDatabaseName("IX_Chats_ChatUniqId_CreatorId");
            });

            // Messages of a chat ordered by time (for fetching whole conversation)
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasIndex(e => new { e.ChatId, e.Time })
                    .HasDatabaseName("IX_Messages_ChatId_Time")
                    .IncludeProperties(e => new
                    {
                        e.SenderId,
                        e.IsSeen,
                        e.MessageContentId
                    });
            });

            // User chat list: filter by CreatorId, order by CreatedTime DESC
            modelBuilder.Entity<Chat>(entity =>
            {
                entity.HasIndex(e => new { e.CreatorId, e.CreatedTime })
                    .IsDescending(false, true)  // CreatorId ASC, CreatedTime DESC
                    .HasDatabaseName("IX_Chats_CreatorId_CreatedTime")
                    .IncludeProperties(e => new
                    {
                        e.ChatUniqId,
                        e.Subject,
                        e.ChatStatusId,
                        e.SupportCategoryId
                    });
            });

            // Latest message per chat (used in subqueries)
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasIndex(e => new { e.ChatId, e.Time })
                    .IsDescending(false, true)  // ChatId ASC, Time DESC
                    .HasDatabaseName("IX_Messages_ChatId_Time_Desc")
                    .IncludeProperties(e => e.MessageContentId);
            });
        }

        #endregion
    }
}