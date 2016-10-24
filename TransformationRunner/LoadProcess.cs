using Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TransformationCore;
using TransformationCore.Exceptions;
using TransformationCore.Interfaces;
using TransformationCore.Models;

namespace Transformation.Loader
{
    public class LoadProcess
    {
        private readonly ILogger _logger;
        private readonly CompositionContainer _container;
        private readonly XElement _config;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IRowLogger _rowlogger;
        private readonly GlobalData _globalData;

        public LoadProcess(XElement config, CancellationTokenSource cancellationTokenSource, ILogger logger, IRowLogger rowlogger = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException("Missing Config");
            }

            _logger = logger;
            _config = config;
            _cancellationTokenSource = cancellationTokenSource;
            _rowlogger = rowlogger;

            var catalog = new AggregateCatalog();

            catalog.Catalogs.Add(new AssemblyCatalog(typeof(LoadProcess).Assembly));
            catalog.Catalogs.Add(new DirectoryCatalog("Engine"));

            _container = new CompositionContainer(catalog);

            var globalDictionaryBuilder = new GlobalDictionaryBuilder();
            _globalData = new GlobalData(globalDictionaryBuilder.Build(_config));
        }

        public async Task Run(XElement processInfo)
        {
            var processStepElements = GetProcessSteps(_config);

            _logger.Info(string.Format("Started: {0:dd-MM-yy HH:mm:ss}", DateTime.Now));

            int count = 0;
            bool success = true;
            foreach (var processStepConfig in processStepElements)
            {
                count++;

                var continueOnFail = processStepConfig.Attribute("continueonfail") != null ? (bool)processStepConfig.Attribute("continueonfail") : false;

                _logger.Info(string.Format("Process {1}: {0:dd-MM-yy HH:mm:ss}", DateTime.Now, count));

                var processStep = GetProcessStep(count, processStepConfig);

                processStep.Initialise(processStepConfig, _cancellationTokenSource, _logger, _rowlogger);

                try
                {
                    success = await processStep.Process(processInfo, _globalData, success);
                }
                catch (Exception ex)
                {
                    _logger.Error(string.Format("Error running process {0}", count), ex);
                    success = false;
                }

                if(!success && !continueOnFail)
                {
                    break;
                }
            }

            _logger.Info(string.Format("Completed: {0:dd-MM-yy HH:mm:ss}", DateTime.Now));
        }

        private IProcessStep GetProcessStep(int count, XElement processStepConfig)
        {
            var processStepName = processStepConfig.Attribute("name")?.Value;

            if (string.IsNullOrWhiteSpace(processStepName))
            {
                throw new LoadProcessException(string.Format("Incorrect Process Step Config : Missing Name : {0}", processStepName));
            }

            string processStepVersion = processStepConfig.Attribute("version")?.Value ?? "";

            try
            {
                var processStepFactory = new MefFactory<IProcessStep>();
                _container.ComposeParts(processStepFactory);

                return processStepFactory.CreateComponent(processStepName, processStepVersion);
            }
            catch (Exception ex)
            {
                _logger.Fatal(string.Format("Unable to Create Process Step {0} : {1} ({2})", count, processStepName, ex.Message));
                throw new LoadProcessException(string.Format("Unable to Create Process Step {0} : {1}", processStepName, ex.Message));
            }
        }

        private static IEnumerable<XElement> GetProcessSteps(XElement config)
        {
            if (config.Elements("step").Count() == 0)
            {
                throw new LoadProcessException("Config has no Process Steps");
            }

            return config.Elements("step");
        }
    }
}
