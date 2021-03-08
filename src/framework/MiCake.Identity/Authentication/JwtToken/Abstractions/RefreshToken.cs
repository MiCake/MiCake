using MiCake.Core.Util;
using System;
using System.Security.Claims;

namespace MiCake.Identity.Authentication.JwtToken.Abstractions
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

        public string Description { get; set; }

        public int Version { get; set; }

        public DateTimeOffset? ConsumedTime { get; set; }

        public int Lifetime { get; set; }

        public DateTimeOffset CreationTime { get; set; }

        public ClaimsPrincipal Subject { get; }

        public RefreshToken(string subjectId, ClaimsPrincipal subject)
        {
            CheckValue.NotNullOrWhiteSpace(subjectId, nameof(subjectId));

            SubjectId = subjectId;
            Subject = subject;
        }
    }
}
