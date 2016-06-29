using System;

namespace Logging
{
    public interface ILogger
    {
        void Debug(string message);
        void Debug(string message, Exception ex);
        void DebugFormat(string message, params object[] args);
        void Error(string message);
        void Error(string message, Exception ex);
        void ErrorFormat(string message, params object[] args);
        void Fatal(string message);
        void Fatal(string message, Exception ex);
        void FatalFormat(string message, params object[] args);
        void Info(string message);
        void Info(string message, Exception ex);
        void InfoFormat(string message, params object[] args);
        void Warn(string message);
        void Warn(string message, Exception ex);
        void WarnFormat(string message, params object[] args);
    }
}
