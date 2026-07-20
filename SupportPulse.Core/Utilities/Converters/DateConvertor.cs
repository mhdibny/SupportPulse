#region Usings

using System.Globalization;
using System.Text.RegularExpressions;

#endregion

namespace SupportPulse.Core.Utilities.Converters
{
    /// <summary>
    /// Utility methods for converting dates between Gregorian and Persian (Shamsi) calendars,
    /// and for calculating time differences in a human‑readable Persian format.
    /// </summary>
    public static class DateConverter
    {
        #region Shamsi → Gregorian

        /// <summary>
        /// Converts a Persian (Shamsi) date string to a <see cref="DateTime"/>?.
        /// Supports optional time part (HH:MM:SS).
        /// </summary>
        /// <param name="dateTime">Shamsi date string in format "yyyy/MM/dd" or "yyyy/MM/dd HH:mm:ss".</param>
        /// <returns>A <see cref="DateTime"/> representing the Gregorian equivalent, or <c>null</c> if conversion fails.</returns>
        public static DateTime? ShamsiToMiladi(this string? dateTime)
        {
            if (string.IsNullOrWhiteSpace(dateTime))
                return null;

            PersianCalendar pc = new();
            string pattern = @"^(?<y>\d{4})/(?<m>\d{1,2})/(?<d>\d{1,2})(?:\s+(?<h>\d{1,2}):(?<min>\d{1,2}):(?<s>\d{1,2}))?$";
            var match = Regex.Match(dateTime.Trim(), pattern);
            if (!match.Success) return null;

            if (!int.TryParse(match.Groups["y"].Value, out int year) ||
                !int.TryParse(match.Groups["m"].Value, out int month) ||
                !int.TryParse(match.Groups["d"].Value, out int day))
                return null;

            int hour = 0, min = 0, sec = 0;
            if (match.Groups["h"].Success)
            {
                hour = int.Parse(match.Groups["h"].Value);
                min = int.Parse(match.Groups["min"].Value);
                sec = int.Parse(match.Groups["s"].Value);
            }

            try
            {
                return pc.ToDateTime(year, month, day, hour, min, sec, 0);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Gregorian → Shamsi

        /// <summary>
        /// Converts a <see cref="DateTime"/> to a Persian (Shamsi) date string (yyyy/MM/dd).
        /// </summary>
        public static string ToShamsi(this DateTime value)
        {
            PersianCalendar pc = new();
            return $"{pc.GetYear(value)}/{pc.GetMonth(value):00}/{pc.GetDayOfMonth(value):00}";
        }

        /// <summary>
        /// Converts a nullable <see cref="DateTime"/> to a Persian (Shamsi) date string, or <c>null</c>.
        /// </summary>
        public static string? ToShamsi(this DateTime? value)
        {
            return value.HasValue ? value.Value.ToShamsi() : null;
        }

        /// <summary>
        /// Converts a <see cref="DateTime"/> to a Persian (Shamsi) date and time string (yyyy/MM/dd - HH:mm).
        /// </summary>
        public static string ToShamsiWithTime(this DateTime value)
        {
            PersianCalendar pc = new();
            return $"{pc.GetYear(value)}/{pc.GetMonth(value):00}/{pc.GetDayOfMonth(value):00} - {pc.GetHour(value):00}:{pc.GetMinute(value):00}";
        }

        /// <summary>
        /// Converts a nullable <see cref="DateTime"/> to a Persian (Shamsi) date and time string, or <c>null</c>.
        /// </summary>
        public static string? ToShamsiWithTime(this DateTime? value)
        {
            if (value is null) return null;

            var dateTime = value.Value;
            PersianCalendar pc = new();
            return $"{pc.GetYear(dateTime)}/{pc.GetMonth(dateTime):00}/{pc.GetDayOfMonth(dateTime):00} - {pc.GetHour(dateTime):00}:{pc.GetMinute(dateTime):00}";
        }

        /// <summary>
        /// Attempts to parse a string as a <see cref="DateTime"/> and return its Persian (Shamsi) representation.
        /// Returns <c>null</c> if parsing fails.
        /// </summary>
        public static string? ToShamsi(this string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (!DateTime.TryParse(value, out var dateTime)) return null;

            PersianCalendar pc = new();
            return $"{pc.GetYear(dateTime)}/{pc.GetMonth(dateTime):00}/{pc.GetDayOfMonth(dateTime):00}";
        }

        #endregion

        #region Difference from now

        /// <summary>
        /// Calculates the time difference between a future <see cref="DateTime"/> and now,
        /// and returns a human‑readable Persian string (e.g., "2 روز و 3 ساعت").
        /// </summary>
        /// <param name="value">The future date (if <c>null</c> or in the past, returns <c>null</c>).</param>
        /// <returns>A Persian description of the remaining time, or <c>null</c>.</returns>
        public static string? GetDifferenceFromNow(this DateTime? value)
        {
            DateTime now = DateTime.Now;

            if (value is null || value.Value <= now)
                return null;

            DateTime future = value.Value;

            // Calendar‑based date difference
            int years = future.Year - now.Year;
            int months = future.Month - now.Month;
            int days = future.Day - now.Day;

            // Time difference
            TimeSpan timeDiff = future.TimeOfDay - now.TimeOfDay;
            int hours = timeDiff.Hours;
            int minutes = timeDiff.Minutes;

            // Adjust negative components by borrowing from higher units
            if (minutes < 0) { minutes += 60; hours--; }
            if (hours < 0) { hours += 24; days--; }

            if (days < 0)
            {
                var prevMonth = future.AddMonths(-1);
                days += DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
                months--;
            }
            if (months < 0) { months += 12; years--; }

            // Build the Persian output string (only non‑zero components)
            var parts = new List<string>();

            if (years > 0) parts.Add($"{years} سال");
            if (months > 0) parts.Add($"{months} ماه");
            if (days > 0) parts.Add($"{days} روز");
            if (hours > 0) parts.Add($"{hours} ساعت");
            if (minutes > 0) parts.Add($"{minutes} دقیقه");

            return parts.Count > 0 ? string.Join(" و ", parts) : "کمتر از یک دقیقه";
        }

        #endregion
    }
}