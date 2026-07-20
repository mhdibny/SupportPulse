namespace SupportPulse.Core.Services.Admin.Scoring
{
    /// <summary>
    /// Calculates a score for each admin based on current load and activity,
    /// used to determine the best candidate for automatic chat assignment.
    /// </summary>
    public interface IScoringService
    {
        /// <summary>
        /// Computes a score for an admin given their current active chats,
        /// number of chats ended today, and idle minutes since their last user message.
        /// Higher scores indicate better assignment candidates.
        /// </summary>
        /// <param name="activeChats">Number of chats currently locked by the admin.</param>
        /// <param name="endedToday">Number of chats the admin has ended today.</param>
        /// <param name="idleMinutes">Minutes since the last user message in the admin's locked chats.</param>
        double CalculateScore(int activeChats, int endedToday, double idleMinutes);
    }
}