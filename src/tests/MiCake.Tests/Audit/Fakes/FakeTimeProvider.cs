using System;

namespace MiCake.Audit.Tests.Fakes
{
    /// <summary>
    /// Fake TimeProvider for testing audit functionality with fixed time.
    /// </summary>
    internal class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _fixedTime;

        public FakeTimeProvider(DateTimeOffset fixedTime)
        {
            _fixedTime = fixedTime;
        }

        public override DateTimeOffset GetUtcNow() => _fixedTime;
    }
}
