using Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TransformationCore.Interfaces
{
    public interface IProcessStep
    {
        void Initialise(XElement config, CancellationTokenSource cancellationTokenSource, ILogger logger, IRowLogger rowlogger);

        Task<bool> Process(XElement processInfo, bool previousStepSucceeded = true);
    }
}
