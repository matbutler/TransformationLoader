using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Logging;
using TransformationCore;
using TransformationCore.Interfaces;
using TransformationCore.Models;

namespace SQLBulkLoader
{
    [Export(typeof(IProcessStep))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("Name", "SQLBulkLoaderStep")]
    [ExportMetadata("Version", "1.0.0")]
    public class SQLBulkLoaderStep : IProcessStep
    {
        public void Initialise(XElement config, CancellationTokenSource cancellationTokenSource, ILogger logger, IRowLogger rowlogger)
        {
            
        }

        public Task<bool> Process(XElement processInfo, GlobalData globalData, bool previousStepSucceeded = true)
        {
            var a = globalData;

            return Task.FromResult(true);
        }
    }
}
