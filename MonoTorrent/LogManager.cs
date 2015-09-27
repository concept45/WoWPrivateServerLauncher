// -------------------------------------------------------------
// imported from SharpDc project 
// written by Vladislav Pozdnyakov (hackward@gmail.com) 2012-2013
// licensed under the LGPL
// -------------------------------------------------------------

using System.Diagnostics;

namespace MonoTorrent
{
    /// <summary>
    /// LogManager provides logger object for a specific class
    /// This allows to filter information by class if supported
    /// </summary>
    public interface ILogManager
    {
        ILogger GetLogger(string className);
    }

    /// <summary>
    /// Performs actual logging implementation
    /// </summary>
    public interface ILogger
    {
        void Info(string message, params object[] args);
        void Warn(string message, params object[] args);
        void Error(string message, params object[] args);
        void Fatal(string message, params object[] args);
    }

    /// <summary>
    /// Provides object that is used when no logging is required
    /// </summary>
    public class NullLogger : ILogger
    {
        public void Info(string message, params object[] args)
        {
        }

        public void Warn(string message, params object[] args)
        {
        }

        public void Error(string message, params object[] args)
        {
        }

        public void Fatal(string message, params object[] args)
        {
        }
    }

    /// <summary>
    /// Redirects all messages to the TraceListener
    /// </summary>
    public class TraceLogger : ILogger
    {
        public void Info(string message, params object[] args)
        {
            Trace.TraceInformation(string.Format(message, args));
        }

        public void Warn(string message, params object[] args)
        {
            Trace.TraceWarning(string.Format(message, args));
        }

        public void Error(string message, params object[] args)
        {
            Trace.TraceError(string.Format(message, args));
        }

        public void Fatal(string message, params object[] args)
        {
            Trace.TraceError(string.Format(message, args));
        }
    }

    /// <summary>
    /// Provides single trace logger for every class
    /// </summary>
    public class TraceLogManager : ILogManager
    {
        private static readonly TraceLogger Logger = new TraceLogger();

        public ILogger GetLogger(string className)
        {
            return Logger;
        }
    }

    /// <summary>
    /// Provides single null logger if no logging is required
    /// </summary>
    public class NullLogManager : ILogManager
    {
        private static readonly NullLogger Logger = new NullLogger();

        public ILogger GetLogger(string className)
        {
            return Logger;
        }
    }

    /// <summary>
    /// Helper to obtain class logger
    /// </summary>
    public static class LogManager
    {
        public static ILogManager LogManagerInstance = new NullLogManager();

        public static ILogger GetLogger()
        {
            if (LogManagerInstance is NullLogManager)
            {
                return LogManagerInstance.GetLogger(string.Empty);
            }

            if (LogManagerInstance is TraceLogManager)
            {
                return LogManagerInstance.GetLogger(string.Empty);
            }

            var stackFrame = new StackFrame(1, false);
            return LogManagerInstance.GetLogger(stackFrame.GetMethod().DeclaringType.FullName);
        }
    }
}