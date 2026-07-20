#region Usings

using System.ComponentModel.DataAnnotations;
using SupportPulse.Core.DTOs.Common;

#endregion

namespace SupportPulse.App.Security.HubModelValidator
{
    /// <summary>
    /// Validates models passed to SignalR Hub methods using Data Annotations.
    /// </summary>
    public static class HubModelValidator
    {
        /// <summary>
        /// Validates the given model and returns a <see cref="HubValidationResult"/> 
        /// containing the first validation error, if any.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to validate.</typeparam>
        /// <param name="model">The model instance to validate.</param>
        public static HubValidationResult Validate<TModel>(TModel model)
        {
            if (model == null)
            {
                return new HubValidationResult
                {
                    IsSuccess = false,
                    Alert = new SystemAlertDto
                    {
                        Title = "خطای اعتبار سنجی",
                        Message = "داده ای برای اعتبار سنجی وجود ندارد",
                        Type = "warning"
                    }
                };
            }

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model, serviceProvider: null, items: null);

            bool isValid = Validator.TryValidateObject(
                model, validationContext, validationResults, validateAllProperties: true);

            if (!isValid)
            {
                // Return the first validation error
                var firstError = validationResults.FirstOrDefault();
                return new HubValidationResult
                {
                    IsSuccess = false,
                    Alert = new SystemAlertDto
                    {
                        Title = "خطای اعتبار سنجی",
                        Message = firstError?.ErrorMessage ?? "خطای نامشخص در اعتبارسنجی.",
                        Type = "warning"
                    }
                };
            }

            return new HubValidationResult { IsSuccess = true };
        }
    }
}