using System.Diagnostics;
using System.Reflection;

namespace MiCake.Util.Diagnostics.Environment
{
    /// <summary>
    /// Determine whether the current environment is debug
    /// </summary>
    public static class DebugEnvironment
    {
        /// <summary>
        /// Main assembly is debug
        /// </summary>
        public static bool IsDebug
        {
            get
            {
                if (_isDebugMode == null)
                {
                    var frames = new StackTrace().GetFrames();
                    var assembly = Assembly.GetEntryAssembly() ?? (frames != null && frames.Length > 0 ? frames[^1]?.GetMethod()?.Module.Assembly : null);
                    var debuggableAttribute = assembly?.GetCustomAttribute<DebuggableAttribute>();
                    _isDebugMode = debuggableAttribute?.DebuggingFlags
                        .HasFlag(DebuggableAttribute.DebuggingModes.EnableEditAndContinue);
                }

                return _isDebugMode ?? false;
            }
        }
        private static bool? _isDebugMode;
    }
}
