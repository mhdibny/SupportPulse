namespace SupportPulse.Core.DTOs.Common
{
    /// <summary>
    /// A lightweight alert DTO sent to clients via SignalR for toast notifications.
    /// </summary>
    public class SystemAlertDto
    {
        public string Title { get; set; } = "خطا";
        public string Message { get; set; } = "خطا";
        public string Type { get; set; } = "error";
    }

    /// <summary>
    /// Contains the result of validating a model inside a SignalR hub method.
    /// </summary>
    public class HubValidationResult
    {
        public bool IsSuccess { get; set; }
        public SystemAlertDto? Alert { get; set; }
    }
}