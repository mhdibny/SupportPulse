namespace SupportPulse.Core.Settings
{
    /// <summary>
    /// Configuration settings for chat auto‑lock and auto‑assign behavior.
    /// </summary>
    public class ChatAutoLockSettings
    {
        /// <summary>
        /// Maximum number of chats that can be automatically assigned to a single admin.
        /// </summary>
        public int MaxActiveChatsPerAdmin { get; set; } = 5;

        /// <summary>
        /// Maximum total active chats (auto + manual) allowed per admin.
        /// </summary>
        public int MaxTotalActiveChatsPerAdmin { get; set; } = 10;

        /// <summary>
        /// Number of minutes after which a locked chat is eligible for automatic unlock.
        /// </summary>
        public double AutoUnlockTimeoutMinutes { get; set; } = 5;

        /// <summary>
        /// Interval (in minutes) at which the unlock check cycle runs.
        /// </summary>
        public double UnlockCheckIntervalMinutes { get; set; } = 1;

        /// <summary>
        /// Weight assigned to free capacity in the admin scoring algorithm.
        /// </summary>
        public int ScoreWeightCapacity { get; set; } = 1000;

        /// <summary>
        /// Weight assigned to the number of chats ended today (penalty).
        /// </summary>
        public int ScoreWeightEfficiency { get; set; } = 10;

        /// <summary>
        /// Weight assigned to idle minutes (bonus).
        /// </summary>
        public int ScoreWeightIdleMinutes { get; set; } = 5;
    }
}