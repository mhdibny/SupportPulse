#region Usings

using Microsoft.Extensions.Options;
using SupportPulse.Core.Settings;

#endregion

namespace SupportPulse.Core.Services.Admin.Scoring
{
    /// <summary>
    /// Implements the scoring algorithm using weights defined in <see cref="ChatAutoLockSettings"/>.
    /// </summary>
    public class ScoringService : IScoringService
    {
        #region Constructor & Dependencies

        private readonly ChatAutoLockSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScoringService"/> class.
        /// </summary>
        /// <param name="settings">The auto‑lock settings containing scoring weights.</param>
        public ScoringService(IOptions<ChatAutoLockSettings> settings)
        {
            _settings = settings.Value;
        }

        #endregion

        #region Scoring

        /// <inheritdoc />
        public double CalculateScore(int activeChats, int endedToday, double idleMinutes)
        {
            // Score = (free capacity * weight) - (ended today penalty) + (idle time bonus)
            return (_settings.MaxActiveChatsPerAdmin - activeChats) * _settings.ScoreWeightCapacity
                   - endedToday * _settings.ScoreWeightEfficiency
                   + idleMinutes * _settings.ScoreWeightIdleMinutes;
        }

        #endregion
    }
}