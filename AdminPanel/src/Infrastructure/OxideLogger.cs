using Oxide.Core;

namespace Oxide.Ext.AdminPanel
{
    public class OxideLogger : ILogger
    {
        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogInfo(string message)
        {
            Interface.Oxide.LogInfo(FormatMessage(message));
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogError(string message)
        {
            Interface.Oxide.LogError(FormatMessage(message)); 
        }

        /// <summary>
        /// Formats the log message with a prefix.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <returns>The formatted message.</returns>
        private string FormatMessage(string message)
        {
            return $"[WebServer] {message}";
        }
    }
}
