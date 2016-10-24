using Logging;
using System.ComponentModel.Composition.Hosting;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TransformationCore.Models;

namespace TransformationCore.Interfaces
{
    public interface IProcessStep
    {
        void Initialise(XElement config, CancellationTokenSource cancellationTokenSource, ILogger logger, IRowLogger rowlogger);

        Task<bool> Process(XElement processInfo, GlobalData globalData, bool previousStepSucceeded = true);
    }
}
