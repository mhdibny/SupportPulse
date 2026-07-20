#region Usings

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SupportPulse.App.BackgroundServices;
using SupportPulse.App.Factories.Home;
using SupportPulse.App.HubFilter;
using SupportPulse.App.Hubs.Admin;
using SupportPulse.App.Hubs.Base;
using SupportPulse.App.Hubs.Chat;
using SupportPulse.App.Middleware;
using SupportPulse.App.Session;
using SupportPulse.Core.DTOs.Admin.AutoLock;
using SupportPulse.Core.Mapper;
using SupportPulse.Core.Mapper.Admin;
using SupportPulse.Core.Security.Password;
using SupportPulse.Core.Services.Admin.Assign;
using SupportPulse.Core.Services.Admin.Ban;
using SupportPulse.Core.Services.Admin.Chat;
using SupportPulse.Core.Services.Admin.EventDispatcher;
using SupportPulse.Core.Services.Admin.Message;
using SupportPulse.Core.Services.Admin.OnlineAdminTracker;
using SupportPulse.Core.Services.Admin.PermissionCache;
using SupportPulse.Core.Services.Admin.Roles;
using SupportPulse.Core.Services.Admin.Scoring;
using SupportPulse.Core.Services.Admin.Session;
using SupportPulse.Core.Services.Admin.SupportCategories;
using SupportPulse.Core.Services.Admin.Users;
using SupportPulse.Core.Services.Chats;
using SupportPulse.Core.Services.Common;
using SupportPulse.Core.Services.FileAccess;
using SupportPulse.Core.Services.Files;
using SupportPulse.Core.Services.Hubs.Base;
using SupportPulse.Core.Services.Hubs.Chats;
using SupportPulse.Core.Services.IconMapping;
using SupportPulse.Core.Services.Messages;
using SupportPulse.Core.Services.PresenceTracker;
using SupportPulse.Core.Services.SupportCategories;
using SupportPulse.Core.Services.TokenService;
using SupportPulse.Core.Services.Users;
using SupportPulse.Core.Settings;
using SupportPulse.Core.Utilities.ClaimsPrincipals;
using SupportPulse.Data.Context;
using SupportPulse.Data.Entities.User.Notification;
using System.Security.Claims;
using System.Text;
using System.Threading.Channels;

#endregion

var builder = WebApplication.CreateBuilder(args);

// =====================================================================
// Service Registration
// =====================================================================

#region Core Services

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOperationResultAction, OperationResultAction>();
builder.Services.AddScoped<ISupportCategoryService, SupportCategoryService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IFileAccessService, FileAccessService>();
builder.Services.AddSingleton<IIconMappingService, IconMappingService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<IConnectionPresenceTracker, ConnectionPresenceTracker>();

#endregion

#region Admin Services

builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IBanService, BanService>();
builder.Services.AddScoped<IAdminSupportCategoryService, AdminSupportCategoryService>();
builder.Services.AddScoped<IAdminChatService, AdminChatService>();
builder.Services.AddScoped<IAdminMessageService, AdminMessageService>();
builder.Services.AddSingleton<IOnlineAdminTracker, OnlineAdminTracker>();

#endregion

#region Event Dispatcher & Notification System

builder.Services.AddScoped<ICurrentAdminSession, CurrentAdminSession>();

// Permission cache (singleton – rebuilt on app start)
builder.Services.AddSingleton<IAdminPermissionCacheService, AdminPermissionCacheService>();

// Core dispatcher
builder.Services.AddScoped<IAdminEventDispatcher, AdminEventDispatcher>();

// Real‑time notifier (App layer implementation)
builder.Services.AddScoped<IAdminEventNotifier, AdminEventNotifier>();

// Channel for async notification persistence
builder.Services.AddSingleton(Channel.CreateBounded<AdminNotification>(
    new BoundedChannelOptions(500)
    {
        FullMode = BoundedChannelFullMode.Wait
    }));

// Background service that saves notifications to the database
builder.Services.AddHostedService<NotificationPersistenceService>();

#endregion

#region Auto Lock / Unlock & Assignment

// Settings
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
builder.Services.Configure<ChatAutoLockSettings>(
    builder.Configuration.GetSection("ChatAutoLockSettings"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<ChatAutoLockSettings>>().Value);

// Scoring & assignment
builder.Services.AddSingleton<IScoringService, ScoringService>();
builder.Services.AddScoped<IAssignChatService, AssignChatService>();

// Single channel for assignment commands (used by auto‑assign and auto‑unlock)
builder.Services.AddSingleton(Channel.CreateBounded<AssignChatDto>(
    new BoundedChannelOptions(1000)
    {
        FullMode = BoundedChannelFullMode.Wait
    }));

builder.Services.AddHostedService<AutoAssignBackgroundService>();
builder.Services.AddHostedService<AutoUnlockBackgroundService>();

#endregion

#region Factories

builder.Services.AddScoped<HomeViewModelFactory>();

#endregion

#region Password Hashing

var passwordHasherOptions = new PasswordHasherOptions
{
    UseArgon2 = true,
    Pepper = builder.Configuration["Secrets:PasswordPepper"],
    Argon2MemoryKb = 64 * 1024,
    Argon2Iterations = 3,
    Argon2DegreeOfParallelism = 4,
    SaltSize = 16,
    HashSize = 32,
    Pbkdf2Iterations = 150_000
};

builder.Services.AddSingleton(passwordHasherOptions);
builder.Services.AddSingleton<PasswordHasher>();

#endregion

#region Database

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    options.ConfigureWarnings(warnings =>
        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
});

#endregion

#region SignalR

builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 5 * 1024 * 1024;
    options.AddFilter<TokenExpirationHubFilter>();
    options.AddFilter<NonceValidationHubFilter>();
    options.AddFilter<SecurityStampHubFilter>();
    options.AddFilter<PermissionCheckerHubFilter>();
});

// Generic system‑message sender
builder.Services.AddScoped(typeof(IHubSystemMessage<>), typeof(GenericHubSystemSender<>));

// Chat hub service for broadcasting messages
builder.Services.AddScoped<IChatHubService, ChatHubService>();

#endregion

#region ASP.NET Core Services

builder.Services.AddResponseCaching();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

// Request size limits (55 MB)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 55 * 1024 * 1024;
});
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 55 * 1024 * 1024;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 55 * 1024 * 1024;
});

#endregion

#region AutoMapper

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MapProfile>();
    cfg.AddProfile<AdminMappingProfile>();
});

#endregion

#region Memory Cache

builder.Services.AddMemoryCache();

#endregion

#region Authentication & Authorization

var jwtKey = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddCookie(options =>
    {
        options.LoginPath = "/SignUp";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Error/404";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
        options.MapInboundClaims = false;

        // Read token from query string for SignalR connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    (path.StartsWithSegments("/Chat") || path.StartsWithSegments("/hubs")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

#endregion

// =====================================================================
// Build & Configure Middleware Pipeline
// =====================================================================

var app = builder.Build();

// Apply EF Core migrations and rebuild the in‑memory permission cache on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();   // Auto‑migrate database to latest version

    var cache = scope.ServiceProvider.GetRequiredService<IAdminPermissionCacheService>();
    await cache.RebuildAsync();         // Warm up the permission cache
}

// Status code pages
app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
// Protect static files under /admin – return 404 for non‑admin users to hide admin assets
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/admin") &&
        Path.HasExtension(context.Request.Path.Value))
    {
        // Not authenticated → 404
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // Authenticated – safely get user ID
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            // If we can't determine the user ID, treat as non‑admin
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var adminUserService = context.RequestServices.GetRequiredService<IAdminUserService>();
        if (!await adminUserService.IsThisUserAdminAsync(userId))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
    }

    await next();
});

app.MapStaticAssets();

app.UseResponseCaching();


// Custom middleware: validate security stamp
app.UseMiddleware<SecurityStampValidatorMiddleware>();


// MVC routes
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// SignalR hubs
app.MapHub<ChatHub>("/Chat");
app.MapHub<AdminHub>("/Hubs/Admin");

app.Run();