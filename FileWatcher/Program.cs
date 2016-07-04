using FileProcessing.Core.Setup;
using Logging;
using Topshelf;

namespace FileProcessing.Watcher
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new Log4NetLogger(typeof(Program));

            DbSetup.Run("FileWatcher");

            var host = HostFactory.New(x =>
            {
                x.BeforeInstall(() => { DbSetup.Run("FileWatcher"); });

                x.Service<FileWatcherService>(nq =>
                {
                    nq.ConstructUsing(name => new FileWatcherService());
                    nq.WhenStarted((tc, hostControl) => tc.Start());
                    nq.WhenStopped((tc, hostControl) => tc.Stop());
                });

                x.RunAsNetworkService();
                x.SetDescription("File Watching Service monitors a directory and add files that require processing to the queue");
                x.SetDisplayName("File Watching Service");
                x.SetServiceName("FileWatchingService");
                x.StartAutomatically();
                x.EnableServiceRecovery(action => action.RestartService(1));
            });

            var exitcode = host.Run();

            if (exitcode != TopshelfExitCode.Ok)
            {
                logger.Error($"Exited with error code {exitcode}");
            }
        }
    }
}
