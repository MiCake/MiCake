namespace MiCake.Identity.Authentication.JwtToken
{
    public enum RefreshTokenUsageMode
    {
        /// <summary>
        /// Re-use the refresh token handle when refresh token.
        /// </summary>
        Reuse = 0,

        /// <summary>
        /// Always create a new refresh-token when refresh token.
        /// </summary>
        Recreate = 1,

        /// <summary>
        /// Recreate a new refresh-token before old token overdue.
        /// Use this mode,need set RecreateRefreshTokenBeforeOverdueMinutes
        /// </summary>
        RecreateBeforeOverdue = 2
    }
}
