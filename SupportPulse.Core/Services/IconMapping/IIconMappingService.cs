#region Usings

using SupportPulse.Core.DTOs.IconMapping;

#endregion

namespace SupportPulse.Core.Services.IconMapping
{
    /// <summary>
    /// Provides Font Awesome icon class resolution and a list of available icon mappings.
    /// </summary>
    public interface IIconMappingService
    {
        /// <summary>
        /// Returns the Font Awesome CSS class for the specified icon key.
        /// Falls back to <c>fas fa-tools</c> if the key is unknown.
        /// </summary>
        /// <param name="iconKey">The icon key (e.g., "home", "settings").</param>
        string GetIconClassByIconKey(string iconKey);

        /// <summary>
        /// Returns all predefined icon mappings (key + Persian display name).
        /// </summary>
        List<IconMappingItemDto> GetAllIconMappings();
    }
}