namespace SupportPulse.Core.DTOs.Common
{
    /// <summary>
    /// Represents the outcome of a service operation, including success status,
    /// a user‑facing message, and a typed status code.
    /// </summary>
    public class OperationResult
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public OperationStatus Status { get; set; }

        /// <summary>
        /// A Persian title that corresponds to the <see cref="Status"/> value.
        /// </summary>
        public string MessageTitle => Status switch
        {
            OperationStatus.Success => "موفقیت",
            OperationStatus.Error => "خطا",
            OperationStatus.Warning => "هشدار",
            OperationStatus.ValidationError => "خطای اعتبار سنجی",
            OperationStatus.NotFound => "پیدا نشد",
            OperationStatus.Info => "اطلاعات",
            _ => "خطا"
        };

        /// <summary>
        /// A CSS‑friendly type string that corresponds to the <see cref="Status"/> value.
        /// </summary>
        public string MessageType => Status switch
        {
            OperationStatus.Success => "success",
            OperationStatus.Error => "error",
            OperationStatus.Warning => "warning",
            OperationStatus.NotFound => "info",
            OperationStatus.Info => "info",
            OperationStatus.ValidationError => "warning",
            _ => "error"
        };
    }

    /// <summary>
    /// A generic version of <see cref="OperationResult"/> that carries additional data.
    /// </summary>
    /// <typeparam name="T">The type of the result data.</typeparam>
    public class OperationResult<T> : OperationResult
    {
        public T? Data { get; set; }
    }

    /// <summary>
    /// Enumerates possible operation statuses with predefined Persian titles and CSS types.
    /// </summary>
    public enum OperationStatus
    {
        Success,
        Error,
        ValidationError,
        Warning,
        NotFound,
        Info
    }
}