#region Usings

using System.ComponentModel.DataAnnotations;
using SupportPulse.Core.DTOs.Common;

#endregion

namespace SupportPulse.Core.ViewModels.User
{
    /// <summary>
    /// View model for the sign‑up / registration page.
    /// </summary>
    public class SignUpUserVM : WithAlertViewModel
    {
        [Display(Name = "نام کاربری")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [MaxLength(50, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        [MinLength(5, ErrorMessage = "{0} نمیتواند کمتر از {1} کاراکتر باشد.")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "{0} فقط باید شامل حروف انگلیسی، اعداد و کاراکترهای '_' و '-' و '.' باشد.")]
        public required string UserName { get; set; }

        [Display(Name = "نام")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [MaxLength(70, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        [MinLength(3, ErrorMessage = "{0} نمیتواند کمتر از {1} کاراکتر باشد.")]
        public required string FirstName { get; set; }

        [Display(Name = "نام خانوادگی")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [MaxLength(70, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        [MinLength(3, ErrorMessage = "{0} نمیتواند کمتر از {1} کاراکتر باشد.")]
        public required string LastName { get; set; }

        [Display(Name = "رمز عبور")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [MaxLength(200, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        [MinLength(6, ErrorMessage = "{0} نمیتواند کمتر از {1} کاراکتر باشد.")]
        public required string Password { get; set; }

        [Display(Name = "تکرار رمز عبور")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [MaxLength(200, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        [MinLength(6, ErrorMessage = "{0} نمیتواند کمتر از {1} کاراکتر باشد.")]
        [Compare("Password", ErrorMessage = "{0} با {1} مطابقت ندارد.")]
        public required string RePassword { get; set; }
    }

    /// <summary>
    /// View model for the login page.
    /// </summary>
    public class LoginUserVM : WithAlertViewModel
    {
        [Display(Name = "نام کاربری")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [MaxLength(50, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        [MinLength(5, ErrorMessage = "{0} نمیتواند کمتر از {1} کاراکتر باشد.")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "{0} فقط باید شامل حروف انگلیسی، اعداد و کاراکترهای '_' و '-' و '.' باشد.")]
        public required string UserName { get; set; }

        [Display(Name = "رمز عبور")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [MaxLength(200, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        [MinLength(6, ErrorMessage = "{0} نمیتواند کمتر از {1} کاراکتر باشد.")]
        public required string Password { get; set; }

        [Display(Name = "مرا به خاطر بسپار")]
        public bool RememberMe { get; set; }
    }
}