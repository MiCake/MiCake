using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MiCake.Core.Util
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
                    var assembly = Assembly.GetEntryAssembly();
                    if (assembly == null)
                    {
                        assembly = new StackTrace().GetFrames().Last().GetMethod().Module.Assembly;
                    }

                    var debuggableAttribute = assembly.GetCustomAttribute<DebuggableAttribute>();
                    _isDebugMode = debuggableAttribute.DebuggingFlags
                        .HasFlag(DebuggableAttribute.DebuggingModes.EnableEditAndContinue);
                }

                return _isDebugMode.Value;
            }
        }
        private static bool? _isDebugMode;
    }
}
