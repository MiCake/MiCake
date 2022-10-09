using MiCake.Core.Util;
using System.Security.Claims;

namespace MiCake.Identity.Authentication.JwtToken
{
    /// <summary>
    /// Defined a refresh token
    /// </summary>
    public class RefreshToken
    {
        /// <summary>
        ///  Gets the subject identifier.
        /// </summary>
        public string SubjectId { get; }

        public string? Description { get; set; }

        public int Version { get; set; }

        /// <summary>
        /// The consumed date time for current refresh token.
        /// If the <see cref="ConsumedTime"/> has value, then current refresh token is no longer valid.
        /// Note: this time is Utc.
        /// </summary>
        public DateTime? ConsumedTime { get; set; }

        public int Lifetime { get; set; }

        /// <summary>
        /// The created date time for current refresh token.
        /// Note: this time is Utc.
        /// </summary>
        public DateTime CreationTime { get; set; }

        public ClaimsPrincipal Subject { get; }

        public RefreshToken(string subjectId, ClaimsPrincipal subject)
        {
            CheckValue.NotNullOrWhiteSpace(subjectId, nameof(subjectId));

            SubjectId = subjectId;
            Subject = subject;
        }

        public DateTime GetExpireDateTime()
        {
            return CreationTime.AddSeconds(Lifetime);
        }
    }
}
