namespace MiCake.Core.Time
{
    /// <summary>
    /// This interface is used to get current datetime with zone.
    /// <para>
    ///     MiCake use it to get current time when the time needs to be set.
    /// </para>
    /// <para>
    ///     The default value is Utc now, you should implement this interface and replace default class if you want to use your custom zone.
    /// </para>
    /// </summary>
    public interface IAppClock
    {
        DateTime Now { get; }
    }

    internal class AppClock : IAppClock
    {
        public DateTime Now => DateTime.UtcNow;
    }
}
