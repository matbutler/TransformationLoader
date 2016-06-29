using log4net;
using log4net.Config;
using System;

namespace Logging
{
    public class Log4NetLogger : ILogger
    {
        private readonly ILog _log;

        public Log4NetLogger(Type type)
        {
            _log = LogManager.GetLogger(type);
            XmlConfigurator.Configure();
        }

        public void Debug(string message)
        {
            _log.Debug(message);
        }

        public void Debug(string message, Exception ex)
        {
            _log.Debug(message, ex);
        }

        public void Info(string message)
        {
            _log.Info(message);
        }

        public void Info(string message, Exception ex)
        {
            _log.Info(message, ex);
        }

        public void Warn(string message)
        {
            _log.Warn(message);
        }

        public void Warn(string message, Exception ex)
        {
            _log.Warn(message, ex);
        }

        public void Error(string message)
        {
            _log.Error(message);
        }

        public void Error(string message, Exception ex)
        {
            _log.Error(message, ex);
        }

        public void Fatal(string message)
        {
            _log.Fatal(message);
        }

        public void Fatal(string message, Exception ex)
        {
            _log.Fatal(message, ex);
        }

        public void DebugFormat(string message, params object[] args)
        {
            _log.DebugFormat(message, args);
        }

        public void InfoFormat(string message, params object[] args)
        {
            _log.InfoFormat(message, args);
        }

        public void WarnFormat(string message, params object[] args)
        {
            _log.WarnFormat(message, args);
        }

        public void ErrorFormat(string message, params object[] args)
        {
            _log.ErrorFormat(message, args);
        }

        public void FatalFormat(string message, params object[] args)
        {
            _log.FatalFormat(message, args);
        }
    }

}
