namespace SupportPulse.Core.DTOs.IconMapping
{
    /// <summary>
    /// Represents an icon mapping entry with its key and Persian display name.
    /// </summary>
    public class IconMappingItemDto
    {
        /// <summary>
        /// The icon key (Font‑Awesome class without the "fas fa-" prefix).
        /// </summary>
        public required string IconKey { get; set; }

        /// <summary>
        /// The Persian name of the icon for display in the UI.
        /// </summary>
        public required string PersianName { get; set; }
    }
}