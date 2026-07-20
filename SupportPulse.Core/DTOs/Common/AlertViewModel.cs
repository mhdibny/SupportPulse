namespace SupportPulse.Core.DTOs.Common
{
    /// <summary>
    /// Represents an alert message that can be displayed to the user.
    /// </summary>
    public class AlertViewModel
    {
        public string Message { get; set; } = "خطا";
        public string MessageType { get; set; } = "error";
        public string MessageTitle { get; set; } = "خطا";
    }

    /// <summary>
    /// Base view model that can optionally carry an <see cref="AlertViewModel"/>.
    /// </summary>
    public class WithAlertViewModel
    {
        public AlertViewModel? Alert { get; set; }
    }

    /// <summary>
    /// A view model that combines a list of items with an optional alert message.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    public class ListWithAlertViewModel<T> : WithAlertViewModel
    {
        public List<T> Items { get; set; } = new();
    }
}