using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SupportPulse.Data.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    IsBan = table.Column<bool>(type: "bit", nullable: false),
                    BanExpiry = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageTypeId = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageContents_MessageTypes_MessageTypeId",
                        column: x => x.MessageTypeId,
                        principalTable: "MessageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminUserId = table.Column<int>(type: "int", nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSeen = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminNotifications_Users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Chats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatUniqId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: false),
                    SupportCategoryId = table.Column<int>(type: "int", nullable: false),
                    IsEnded = table.Column<bool>(type: "bit", nullable: false),
                    ChatStatusId = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LockedByAdminId = table.Column<int>(type: "int", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chats_ChatStatus_ChatStatusId",
                        column: x => x.ChatStatusId,
                        principalTable: "ChatStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Chats_SupportCategories_SupportCategoryId",
                        column: x => x.SupportCategoryId,
                        principalTable: "SupportCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Chats_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Chats_Users_LockedByAdminId",
                        column: x => x.LockedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Expires = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBanHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    BannedByAdminId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BanExpiry = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBanHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBanHistories_Users_BannedByAdminId",
                        column: x => x.BannedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserBanHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSupportCategories",
                columns: table => new
                {
                    SupportCategoryId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSupportCategories", x => new { x.UserId, x.SupportCategoryId });
                    table.ForeignKey(
                        name: "FK_UserSupportCategories_SupportCategories_SupportCategoryId",
                        column: x => x.SupportCategoryId,
                        principalTable: "SupportCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSupportCategories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttachFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalFileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SavedPath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    MessageContentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttachFiles_MessageContents_MessageContentId",
                        column: x => x.MessageContentId,
                        principalTable: "MessageContents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatId = table.Column<int>(type: "int", nullable: false),
                    MessageContentId = table.Column<int>(type: "int", nullable: false),
                    Time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSeen = table.Column<bool>(type: "bit", nullable: false),
                    SeenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SenderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_MessageContents_MessageContentId",
                        column: x => x.MessageContentId,
                        principalTable: "MessageContents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "ChatStatus",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "گفتگو تکمیل شد." },
                    { 2, "درحال پاسخگویی" }
                });

            migrationBuilder.InsertData(
                table: "MessageTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "متن" },
                    { 2, "فایل" },
                    { 3, "متن و فایل" }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Name" },
                values: new object[,]
                {
                    { 1, "General", "داشبورد پنل ادمین" },
                    { 2, "General", "لیست نقش‌ها" },
                    { 3, "General", "افزودن نقش" },
                    { 4, "General", "ویرایش نقش" },
                    { 5, "General", "حذف نقش" },
                    { 6, "General", "لیست کاربران" },
                    { 7, "General", "مسدود کردن کاربر" },
                    { 8, "General", "رفع مسدودیت کاربر" },
                    { 9, "General", "تغییر مدت مسدودیت" },
                    { 10, "General", "مشاهده تاریخچه مسدودیت" },
                    { 11, "General", "لیست واحدهای پشتیبانی" },
                    { 12, "General", "افزودن واحد پشتیبانی" },
                    { 13, "General", "ویرایش واحد پشتیبانی" },
                    { 14, "General", "مشاهده لیست چت‌ها" },
                    { 15, "General", "مشاهده جزئیات چت" },
                    { 16, "General", "قفل کردن چت" },
                    { 17, "General", "آزادسازی چت" },
                    { 18, "General", "پایان دادن به چت" },
                    { 19, "General", "ارسال پیام در چت" },
                    { 20, "General", "تخصیص نقش به کاربر" },
                    { 21, "General", "تخصیص واحد پشتیبانی به کاربر" },
                    { 101, "Notification", "اعلان آزادسازی چت" },
                    { 102, "Notification", "اعلان قفل شدن چت" },
                    { 103, "Notification", "اعلان پایان چت" },
                    { 104, "Notification", "اعلان مسدود شدن کاربر" },
                    { 105, "Notification", "اعلان رفع مسدودی کاربر" },
                    { 106, "Notification", "اعلان تغییر مدت مسدودی" },
                    { 107, "Notification", "اعلان ایجاد نقش" },
                    { 108, "Notification", "اعلان ویرایش نقش" },
                    { 109, "Notification", "اعلان حذف نقش" },
                    { 110, "Notification", "اعلان ایجاد واحد پشتیبانی" },
                    { 111, "Notification", "اعلان ویرایش واحد پشتیبانی" },
                    { 112, "Notification", "اعلان آزادسازی خودکار چت" },
                    { 113, "Notification", "اعلان تغییر نقش‌های کاربر" },
                    { 114, "Notification", "اعلان تغییر واحدهای پشتیبانی کاربر" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[] { 1, "مدیر کل" });

            migrationBuilder.InsertData(
                table: "SupportCategories",
                columns: new[] { "Id", "Details", "IconKey", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "توضیحاتی مختصر درمورد واحد فنی", "technical", true, "واحد فنی" },
                    { 2, "توضیحاتی مختصر درمورد واحد فروش", "sales", true, "واحد فروش" },
                    { 3, "توضیحاتی مختصر درمورد واحد مدیریت", "management", true, "واحد مدیریت" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "BanExpiry", "FirstName", "IsBan", "LastName", "Password", "SecurityStamp", "UserName" },
                values: new object[] { 1, null, "ادمین", false, "اصلی", "Argon2id$mem=65536;iter=3;par=4$ABX4d3wa6rdwM4SsxCjB0A==$dDUptPckroNuD0JtMyHqjV+ZMifgKTtHHxcvJN61cmc=", "68b6608745fc42f6b03bfa0ad68ac339d5ef90fbeee549a7833a1c985aae7d91", "manager" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 },
                    { 3, 1 },
                    { 4, 1 },
                    { 5, 1 },
                    { 6, 1 },
                    { 7, 1 },
                    { 8, 1 },
                    { 9, 1 },
                    { 10, 1 },
                    { 11, 1 },
                    { 12, 1 },
                    { 13, 1 },
                    { 14, 1 },
                    { 15, 1 },
                    { 16, 1 },
                    { 17, 1 },
                    { 18, 1 },
                    { 19, 1 },
                    { 20, 1 },
                    { 21, 1 },
                    { 101, 1 },
                    { 102, 1 },
                    { 103, 1 },
                    { 104, 1 },
                    { 105, 1 },
                    { 106, 1 },
                    { 107, 1 },
                    { 108, 1 },
                    { 109, 1 },
                    { 110, 1 },
                    { 111, 1 },
                    { 112, 1 },
                    { 113, 1 },
                    { 114, 1 }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { 1, 1 });

            migrationBuilder.InsertData(
                table: "UserSupportCategories",
                columns: new[] { "SupportCategoryId", "UserId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 },
                    { 3, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotifications_AdminUserId",
                table: "AdminNotifications",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttachFiles_MessageContentId",
                table: "AttachFiles",
                column: "MessageContentId");

            migrationBuilder.CreateIndex(
                name: "IX_AttachFiles_SavedPath",
                table: "AttachFiles",
                column: "SavedPath")
                .Annotation("SqlServer:Include", new[] { "MessageContentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_ChatStatusId",
                table: "Chats",
                column: "ChatStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_ChatUniqId_CreatorId",
                table: "Chats",
                columns: new[] { "ChatUniqId", "CreatorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_CreatorId_CreatedTime",
                table: "Chats",
                columns: new[] { "CreatorId", "CreatedTime" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "ChatUniqId", "Subject", "ChatStatusId", "SupportCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_LockedByAdminId",
                table: "Chats",
                column: "LockedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_SupportCategoryId",
                table: "Chats",
                column: "SupportCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageContents_MessageTypeId",
                table: "MessageContents",
                column: "MessageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChatId_Time_Desc",
                table: "Messages",
                columns: new[] { "ChatId", "Time" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "MessageContentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_MessageContentId",
                table: "Messages",
                column: "MessageContentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                table: "Messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBanHistories_BannedByAdminId",
                table: "UserBanHistories",
                column: "BannedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBanHistories_UserId",
                table: "UserBanHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSupportCategories_SupportCategoryId",
                table: "UserSupportCategories",
                column: "SupportCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSupportCategories_UserId_SupportCategoryId",
                table: "UserSupportCategories",
                columns: new[] { "UserId", "SupportCategoryId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminNotifications");

            migrationBuilder.DropTable(
                name: "AttachFiles");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UserBanHistories");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserSupportCategories");

            migrationBuilder.DropTable(
                name: "Chats");

            migrationBuilder.DropTable(
                name: "MessageContents");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "ChatStatus");

            migrationBuilder.DropTable(
                name: "SupportCategories");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "MessageTypes");
        }
    }
}
