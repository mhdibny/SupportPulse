namespace SupportPulse.Core.DTOs.User
{
    /// <summary>
    /// Represents a support category as presented to end users (e.g., for creating a new chat).
    /// </summary>
    public class SupportCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Details { get; set; }
        public string IconKey { get; set; }

        /// <summary>
        /// The Font‑Awesome CSS class computed from <see cref="IconKey"/>.
        /// </summary>
        public string IconClass { get; set; }
    }
}