namespace SupportPulse.Core.Utilities.Modifier
{
    /// <summary>
    /// Provides simple string manipulation helpers.
    /// </summary>
    public static class StringModifier
    {
        /// <summary>
        /// Removes all space characters from the specified string.
        /// </summary>
        /// <param name="value">The input string.</param>
        /// <returns>A new string with all spaces removed.</returns>
        public static string ClearSpaces(this string value)
        {
            return value.Replace(" ", "");
        }

        /// <summary>
        /// Combines first and last names into a full name.
        /// </summary>
        /// <param name="firstName">The first name.</param>
        /// <param name="lastName">The last name.</param>
        /// <returns>The concatenated full name.</returns>
        public static string GetFullName(string firstName, string lastName)
        {
            return $"{firstName} {lastName}";
        }
    }
}