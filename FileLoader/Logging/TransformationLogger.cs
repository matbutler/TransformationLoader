using TransformationCore.Enums;
using TransformationCore.Interfaces;

namespace FileProcessing.Loader.Helper
{
    class TransformationLogger : ILogger
    {
        private readonly Logging.ILogger _logger;

        public TransformationLogger(Logging.ILogger logger)
        {
            _logger = logger;
        }

        public void Log(string message, MessageLevel msgLevel)
        {
            switch (msgLevel)
            {
                case MessageLevel.Info:
                    _logger.Info(message);
                    break;
                case MessageLevel.Action:
                    _logger.Info(message);
                    break;
                case MessageLevel.Warn:
                    _logger.Warn(message);
                    break;
                case MessageLevel.Critical:
                    _logger.Error(message);
                    break;
                default:
                    _logger.Error(message);
            }
        }
    }
}
