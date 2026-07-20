namespace SupportPulse.Core.Utilities.Converters
{
    /// <summary>
    /// Provides helper methods for time conversion and formatting.
    /// </summary>
    public static class TimeConverter
    {
        /// <summary>
        /// Converts a <see cref="TimeSpan"/> to a <see cref="TimeOnly"/> value.
        /// </summary>
        /// <param name="value">The time span to convert.</param>
        /// <returns>A <see cref="TimeOnly"/> representing the same time of day.</returns>
        public static TimeOnly ToTimeOnly(this TimeSpan value)
        {
            return new TimeOnly(value.Hours, value.Minutes, value.Seconds, value.Milliseconds);
        }

        /// <summary>
        /// Formats a <see cref="DateTime"/> as a short time string (HH:mm).
        /// </summary>
        /// <param name="time">The date and time to format.</param>
        /// <returns>A string in "HH:mm" format.</returns>
        public static string ToDayTime(this DateTime time)
        {
            return $"{time.Hour:00}:{time.Minute:00}";
        }
    }
}