#region Usings

using SupportPulse.Core.DTOs.IconMapping;

#endregion

namespace SupportPulse.Core.Services.IconMapping
{
    /// <summary>
    /// Provides a static dictionary of Font Awesome icon keys to their CSS classes,
    /// and a list of Persian‑named icon mappings for the admin UI.
    /// </summary>
    public class IconMappingService : IIconMappingService
    {
        #region Icon Class Dictionary

        private readonly Dictionary<string, string> _iconClasses = new()
        {
            {"home", "fas fa-home"},
            {"dashboard", "fas fa-tachometer-alt"},
            {"user", "fas fa-user"},
            {"users", "fas fa-users"},
            {"search", "fas fa-search"},
            {"settings", "fas fa-cog"},
            {"notification", "fas fa-bell"},
            {"message", "fas fa-envelope"},
            {"chat", "fas fa-comments"},
            {"calendar", "fas fa-calendar-alt"},
            {"file", "fas fa-file"},
            {"folder", "fas fa-folder"},
            {"download", "fas fa-download"},
            {"upload", "fas fa-upload"},
            {"edit", "fas fa-edit"},
            {"delete", "fas fa-trash-alt"},
            {"add", "fas fa-plus-circle"},
            {"check", "fas fa-check-circle"},
            {"close", "fas fa-times-circle"},
            {"warning", "fas fa-exclamation-triangle"},
            {"support", "fas fa-headset"},
            {"technical", "fas fa-tools"},
            {"financial", "fas fa-coins"},
            {"sales", "fas fa-chart-line"},
            {"management", "fas fa-briefcase"},
            {"hr", "fas fa-user-tie"},
            {"marketing", "fas fa-bullhorn"},
            {"design", "fas fa-paint-brush"},
            {"development", "fas fa-code"},
            {"legal", "fas fa-gavel"},
            {"security", "fas fa-shield-alt"},
            {"education", "fas fa-graduation-cap"},
            {"health", "fas fa-heartbeat"},
            {"logistics", "fas fa-warehouse"},
            {"research", "fas fa-flask"},
            {"ceo", "fas fa-crown"},
            {"secretary", "fas fa-phone-alt"},
            {"consultant", "fas fa-handshake"},
            {"media", "fas fa-photo-video"},
            {"network", "fas fa-network-wired"},
            {"print", "fas fa-print"},
            {"save", "fas fa-save"},
            {"share", "fas fa-share-alt"},
            {"link", "fas fa-link"},
            {"unlink", "fas fa-unlink"},
            {"lock", "fas fa-lock"},
            {"unlock", "fas fa-unlock"},
            {"refresh", "fas fa-sync-alt"},
            {"backup", "fas fa-cloud-upload-alt"},
            {"restore", "fas fa-history"},
            {"archive", "fas fa-archive"},
            {"filter", "fas fa-filter"},
            {"sort", "fas fa-sort-amount-down"},
            {"zoom-in", "fas fa-search-plus"},
            {"zoom-out", "fas fa-search-minus"},
            {"fullscreen", "fas fa-expand"},
            {"crop", "fas fa-crop-alt"},
            {"copy", "fas fa-copy"},
            {"paste", "fas fa-paste"},
            {"cut", "fas fa-cut"},
            {"report", "fas fa-chart-bar"},
            {"statistics", "fas fa-chart-pie"},
            {"analytics", "fas fa-analytics"},
            {"invoice", "fas fa-file-invoice"},
            {"receipt", "fas fa-receipt"},
            {"task", "fas fa-tasks"},
            {"project", "fas fa-project-diagram"},
            {"note", "fas fa-sticky-note"},
            {"book", "fas fa-book"},
            {"newspaper", "fas fa-newspaper"},
            {"article", "fas fa-file-alt"},
            {"video", "fas fa-video"},
            {"music", "fas fa-music"},
            {"camera", "fas fa-camera"},
            {"image", "fas fa-image"},
            {"map", "fas fa-map-marker-alt"},
            {"location", "fas fa-location-arrow"},
            {"globe", "fas fa-globe"},
            {"clock", "fas fa-clock"},
            {"stopwatch", "fas fa-stopwatch"},
            {"email", "fas fa-at"},
            {"phone", "fas fa-phone"},
            {"mobile", "fas fa-mobile-alt"},
            {"sms", "fas fa-sms"},
            {"fax", "fas fa-fax"},
            {"wifi", "fas fa-wifi"},
            {"bluetooth", "fas fa-bluetooth"},
            {"battery", "fas fa-battery-full"},
            {"power", "fas fa-power-off"},
            {"star", "fas fa-star"},
            {"heart", "fas fa-heart"},
            {"thumbs-up", "fas fa-thumbs-up"},
            {"thumbs-down", "fas fa-thumbs-down"},
            {"flag", "fas fa-flag"},
            {"tag", "fas fa-tag"},
            {"ticket", "fas fa-ticket-alt"},
            {"cart", "fas fa-shopping-cart"},
            {"credit-card", "fas fa-credit-card"},
            {"wallet", "fas fa-wallet"},
            {"gift", "fas fa-gift"}
        };

        #endregion

        #region Persian Icons List

        private readonly List<IconMappingItemDto> _persianIcons = new()
        {
            new() { IconKey = "home", PersianName = "خانه" },
            new() { IconKey = "dashboard", PersianName = "داشبورد" },
            new() { IconKey = "user", PersianName = "کاربر" },
            new() { IconKey = "users", PersianName = "کاربران" },
            new() { IconKey = "search", PersianName = "جستجو" },
            new() { IconKey = "settings", PersianName = "تنظیمات" },
            new() { IconKey = "notification", PersianName = "اعلان" },
            new() { IconKey = "message", PersianName = "پیام" },
            new() { IconKey = "chat", PersianName = "گفتگو" },
            new() { IconKey = "calendar", PersianName = "تقویم" },
            new() { IconKey = "file", PersianName = "فایل" },
            new() { IconKey = "folder", PersianName = "پوشه" },
            new() { IconKey = "download", PersianName = "دانلود" },
            new() { IconKey = "upload", PersianName = "آپلود" },
            new() { IconKey = "edit", PersianName = "ویرایش" },
            new() { IconKey = "delete", PersianName = "حذف" },
            new() { IconKey = "add", PersianName = "افزودن" },
            new() { IconKey = "check", PersianName = "تأیید" },
            new() { IconKey = "close", PersianName = "بستن" },
            new() { IconKey = "warning", PersianName = "هشدار" },
            new() { IconKey = "support", PersianName = "پشتیبانی" },
            new() { IconKey = "technical", PersianName = "واحد فنی" },
            new() { IconKey = "financial", PersianName = "واحد مالی" },
            new() { IconKey = "sales", PersianName = "واحد فروش" },
            new() { IconKey = "management", PersianName = "مدیریت" },
            new() { IconKey = "hr", PersianName = "منابع انسانی" },
            new() { IconKey = "marketing", PersianName = "بازاریابی" },
            new() { IconKey = "design", PersianName = "طراحی" },
            new() { IconKey = "development", PersianName = "توسعه" },
            new() { IconKey = "legal", PersianName = "حقوقی" },
            new() { IconKey = "security", PersianName = "امنیت" },
            new() { IconKey = "education", PersianName = "آموزش" },
            new() { IconKey = "health", PersianName = "بهداشت" },
            new() { IconKey = "logistics", PersianName = "انبار/لجستیک" },
            new() { IconKey = "research", PersianName = "تحقیقات" },
            new() { IconKey = "ceo", PersianName = "مدیرعامل" },
            new() { IconKey = "secretary", PersianName = "دفتر/منشی" },
            new() { IconKey = "consultant", PersianName = "مشاور" },
            new() { IconKey = "media", PersianName = "رسانه" },
            new() { IconKey = "network", PersianName = "شبکه" },
            new() { IconKey = "print", PersianName = "چاپ" },
            new() { IconKey = "save", PersianName = "ذخیره" },
            new() { IconKey = "share", PersianName = "اشتراک‌گذاری" },
            new() { IconKey = "link", PersianName = "لینک" },
            new() { IconKey = "unlink", PersianName = "قطع لینک" },
            new() { IconKey = "lock", PersianName = "قفل" },
            new() { IconKey = "unlock", PersianName = "باز کردن قفل" },
            new() { IconKey = "refresh", PersianName = "بازنشانی" },
            new() { IconKey = "backup", PersianName = "پشتیبان‌گیری" },
            new() { IconKey = "restore", PersianName = "بازیابی" },
            new() { IconKey = "archive", PersianName = "آرشیو" },
            new() { IconKey = "filter", PersianName = "فیلتر" },
            new() { IconKey = "sort", PersianName = "مرتب‌سازی" },
            new() { IconKey = "zoom-in", PersianName = "بزرگنمایی" },
            new() { IconKey = "zoom-out", PersianName = "کوچک‌نمایی" },
            new() { IconKey = "fullscreen", PersianName = "تمام‌صفحه" },
            new() { IconKey = "crop", PersianName = "برش" },
            new() { IconKey = "copy", PersianName = "کپی" },
            new() { IconKey = "paste", PersianName = "چسباندن" },
            new() { IconKey = "cut", PersianName = "بریدن" },
            new() { IconKey = "report", PersianName = "گزارش" },
            new() { IconKey = "statistics", PersianName = "آمار" },
            new() { IconKey = "analytics", PersianName = "آنالیز" },
            new() { IconKey = "invoice", PersianName = "فاکتور" },
            new() { IconKey = "receipt", PersianName = "رسید" },
            new() { IconKey = "task", PersianName = "وظیفه" },
            new() { IconKey = "project", PersianName = "پروژه" },
            new() { IconKey = "note", PersianName = "یادداشت" },
            new() { IconKey = "book", PersianName = "کتاب" },
            new() { IconKey = "newspaper", PersianName = "روزنامه" },
            new() { IconKey = "article", PersianName = "مقاله" },
            new() { IconKey = "video", PersianName = "ویدیو" },
            new() { IconKey = "music", PersianName = "موسیقی" },
            new() { IconKey = "camera", PersianName = "دوربین" },
            new() { IconKey = "image", PersianName = "تصویر" },
            new() { IconKey = "map", PersianName = "نقشه" },
            new() { IconKey = "location", PersianName = "موقعیت" },
            new() { IconKey = "globe", PersianName = "جهان/وب" },
            new() { IconKey = "clock", PersianName = "ساعت" },
            new() { IconKey = "stopwatch", PersianName = "کرنومتر" },
            new() { IconKey = "email", PersianName = "ایمیل" },
            new() { IconKey = "phone", PersianName = "تلفن" },
            new() { IconKey = "mobile", PersianName = "موبایل" },
            new() { IconKey = "sms", PersianName = "پیامک" },
            new() { IconKey = "fax", PersianName = "فکس" },
            new() { IconKey = "wifi", PersianName = "وای‌فای" },
            new() { IconKey = "bluetooth", PersianName = "بلوتوث" },
            new() { IconKey = "battery", PersianName = "باتری" },
            new() { IconKey = "power", PersianName = "خاموش/روشن" },
            new() { IconKey = "star", PersianName = "ستاره" },
            new() { IconKey = "heart", PersianName = "علاقه" },
            new() { IconKey = "thumbs-up", PersianName = "پسندیدن" },
            new() { IconKey = "thumbs-down", PersianName = "نپسندیدن" },
            new() { IconKey = "flag", PersianName = "پرچم" },
            new() { IconKey = "tag", PersianName = "برچسب" },
            new() { IconKey = "ticket", PersianName = "تیکت" },
            new() { IconKey = "cart", PersianName = "سبد خرید" },
            new() { IconKey = "credit-card", PersianName = "کارت بانکی" },
            new() { IconKey = "wallet", PersianName = "کیف پول" },
            new() { IconKey = "gift", PersianName = "هدیه" }
        };

        #endregion

        #region Methods

        /// <inheritdoc />
        public string GetIconClassByIconKey(string iconKey)
        {
            if (string.IsNullOrWhiteSpace(iconKey))
                return "fas fa-tools";

            return _iconClasses.TryGetValue(iconKey, out string? iconClass)
                ? iconClass
                : "fas fa-tools";
        }

        /// <inheritdoc />
        public List<IconMappingItemDto> GetAllIconMappings()
        {
            return _persianIcons;
        }

        #endregion
    }
}