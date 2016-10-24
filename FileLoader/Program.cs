using FileProcessing.Core.Setup;
using Logging;
using Topshelf;

namespace FileProcessing.Loader
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new Log4NetLogger(typeof(Program));

            DbSetup.Run("FileLoader");

            var host = HostFactory.New(x =>
            {
                x.BeforeInstall(() => { DbSetup.Run("FileLoader"); });

                x.Service<FileLoaderService>(nq =>
                {
                    nq.ConstructUsing(name => new FileLoaderService());
                    nq.WhenStarted((tc, hostControl) => tc.Start());
                    nq.WhenStopped((tc, hostControl) => tc.Stop());
                });

                x.RunAsNetworkService();
                x.SetDescription("File Loading Service monitors a table and add loaded files");
                x.SetDisplayName("File Loading Service");
                x.SetServiceName("FileLoadingService");
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
